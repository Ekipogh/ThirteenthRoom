using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class RoomVisualChecks {
  struct Triangle {public Vector3 a,b,c;}
  sealed class Surface {public Bounds bounds;public List<Triangle> triangles=new List<Triangle>();}
  static Surface SurfaceOf(MeshFilter filter){var surface=new Surface{bounds=filter.GetComponent<MeshRenderer>().bounds};var mesh=filter.sharedMesh;var vertices=mesh.vertices;var mats=filter.GetComponent<MeshRenderer>().sharedMaterials;
    for(int sub=0;sub<mesh.subMeshCount;sub++){if(mats[Math.Min(sub,mats.Length-1)].name=="Transparent")continue;var indices=mesh.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3)surface.triangles.Add(new Triangle{a=filter.transform.TransformPoint(vertices[indices[i]]),b=filter.transform.TransformPoint(vertices[indices[i+1]]),c=filter.transform.TransformPoint(vertices[indices[i+2]])});}return surface;
  }
  static bool Hit(Ray ray,Surface surface,float length){if(!surface.bounds.IntersectRay(ray,out float entry)||entry>length)return false;foreach(var t in surface.triangles){var e1=t.b-t.a;var e2=t.c-t.a;var h=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-7f)continue;float inv=1/det;var s=ray.origin-t.a;float u=Vector3.Dot(s,h)*inv;if(u<-.0001f||u>1.0001f)continue;var q=Vector3.Cross(s,e1);float v=Vector3.Dot(ray.direction,q)*inv;if(v<-.0001f||u+v>1.0001f)continue;float distance=Vector3.Dot(e2,q)*inv;if(distance>=0&&distance<=length)return true;}return false;}
  public static string Run(){if(Application.isPlaying)throw new Exception("Requires Edit Mode");var native=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Thirdparty/PolygonHorrorMansion/Prefabs/Buildings/SM_Bld_Base_Floor_01.prefab").GetComponentInChildren<MeshFilter>().sharedMesh;
    var nv=native.vertices;var nu=native.uv;var ni=native.triangles;var du=nu[ni[1]]-nu[ni[0]];var dv=nu[ni[2]]-nu[ni[0]];float nativeDensity=9*Vector3.Cross(nv[ni[1]]-nv[ni[0]],nv[ni[2]]-nv[ni[0]]).magnitude/Mathf.Abs(du.x*dv.y-du.y*dv.x);
    var wallNative=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Thirdparty/PolygonHorrorMansion/Prefabs/Buildings/SM_Bld_Base_Wall_01.prefab").GetComponentInChildren<MeshFilter>().sharedMesh;
    var wv=wallNative.vertices;var wu=wallNative.uv;var wi=wallNative.triangles;var wallDensities=new List<float>();for(int i=0;i<wi.Length;i+=3){var a=wu[wi[i+1]]-wu[wi[i]];var b=wu[wi[i+2]]-wu[wi[i]];float area=Mathf.Abs(a.x*b.y-a.y*b.x);if(area>.000001f)wallDensities.Add(9*Vector3.Cross(wv[wi[i+1]]-wv[wi[i]],wv[wi[i+2]]-wv[wi[i]]).magnitude/area);}
    int density=0,wallDensity=0,seams=0,spawnPoints=0;var failures=new List<string>();
    foreach(var baseline in Directory.GetFiles("AgentScripts/RoomLayoutBaseline","*.txt")){
      string name=Path.GetFileNameWithoutExtension(baseline);var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Building/"+name+".prefab");try{
        var architecture=root.transform.Find("Authored Architecture");if(architecture==null)continue;
        var floor=architecture.Find("Floor strip").GetComponent<MeshFilter>();var floorSurface=SurfaceOf(floor);var mesh=floor.sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;var indices=mesh.triangles;
        for(int i=0;i<indices.Length;i+=3){int a=indices[i],b=indices[i+1],c=indices[i+2];float worldArea=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).magnitude;var u=uv[b]-uv[a];var v=uv[c]-uv[a];float uvArea=Mathf.Abs(u.x*v.y-u.y*v.x);if(worldArea<.005f||uvArea<.00001f)continue;float ratio=worldArea/uvArea;if(Mathf.Abs(ratio-nativeDensity)>.05f)failures.Add(name+": compressed floor texture "+ratio);density++;}
        var wallMesh=architecture.Find("Wall").GetComponent<MeshFilter>().sharedMesh;var wps=wallMesh.vertices;var wuv=wallMesh.uv;var wind=wallMesh.triangles;
        for(int i=0;i<wind.Length;i+=3){int a=wind[i],b=wind[i+1],c=wind[i+2];float worldArea=Vector3.Cross(wps[b]-wps[a],wps[c]-wps[a]).magnitude;var u=wuv[b]-wuv[a];var v=wuv[c]-wuv[a];float uvArea=Mathf.Abs(u.x*v.y-u.y*v.x);if(worldArea<.005f||uvArea<.00001f)continue;float ratio=worldArea/uvArea;if(!wallDensities.Any(n=>Mathf.Abs(ratio-n)<.1f))failures.Add(name+": compressed wall texture "+ratio);wallDensity++;}
        var surfaces=architecture.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.name=="Wall"||f.gameObject.name=="Upper wall").Select(SurfaceOf).ToArray();
        foreach(var col in architecture.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Wall collision")){
          var bounds=col.bounds;bool horizontal=bounds.size.x>bounds.size.z;var axis=horizontal?Vector3.forward:Vector3.right;var center=bounds.center;center.y=0;
          bool plus=Hit(new Ray(center+axis*.8f+Vector3.up,Vector3.down),floorSurface,1.1f),minus=Hit(new Ray(center-axis*.8f+Vector3.up,Vector3.down),floorSurface,1.1f);if(plus==minus)continue;var inward=plus?axis:-axis;
          float min=horizontal?bounds.min.x:bounds.min.z,max=horizontal?bounds.max.x:bounds.max.z;
          // Endpoint rays 0.02 units from a corner start inside the adjacent wall solid.
          // Diagonal free-space corner coverage is tested by RoomBoundaryChecks instead.
          if(max-min<2.2f)continue;
          foreach(float along in new[]{min+1.1f,(min+max)/2,max-1.1f})foreach(float height in new[]{.3f,4.5f,8.8f,9.2f,13.1f}){
            var origin=center+inward*.9f;if(horizontal)origin.x=along;else origin.z=along;origin.y=height;
            var floorProbe=origin;floorProbe.y=1;if(!Hit(new Ray(floorProbe,Vector3.down),floorSurface,1.1f))continue;
            if(!surfaces.Any(s=>Hit(new Ray(origin,-inward),s,1.5f)))failures.Add(name+": visible shell gap near "+origin);seams++;
          }
        }
        foreach(var point in root.GetComponentsInChildren<ItemSpawnPoint>(true)){var p=point.transform.position;if(name=="B_Library"){
          if(!Hit(new Ray(new Vector3(p.x,1,p.z),Vector3.down),floorSurface,1.1f))failures.Add(name+": item spawn outside floor");
          var desks=root.transform.Find("Objects/Furniture").Cast<Transform>().Where(t=>t.name.StartsWith("SM_Prop_Desk_01"));if(!desks.Any(d=>{var b=d.GetComponent<MeshRenderer>().bounds;return p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z&&Mathf.Abs(p.y-b.max.y)<.12f;}))failures.Add(name+": item spawn has no desk support");var clearance=new Bounds(p+Vector3.up*.2f,new Vector3(.5f,.3f,.5f));
          if(root.GetComponentsInChildren<MeshRenderer>(true).Any(m=>m.bounds.min.y>2&&m.bounds.Intersects(clearance)))failures.Add(name+": item spawn intersects desktop decoration");spawnPoints++;
        }}
        if(name=="B_Library"&&(root.GetComponent<Room>().GetItemSpawnPoints().Length!=3||root.transform.Find("Objects/Furniture").Cast<Transform>().Count(t=>t.name.StartsWith("Bookshelf"))!=3))failures.Add(name+": expected three supported item spawn points");
        if(name=="CR_Corridor"){
          var painting=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Painting2");var bounds=painting.GetComponentsInChildren<MeshRenderer>(true).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
          if(bounds.size.x>.3f||bounds.max.x> -3.8f||bounds.min.x< -4.18f||bounds.min.z< -15.8f||bounds.max.z> -11.7f)failures.Add(name+": painting is not mounted flush to the side wall");
        }
        if(name=="CN_Office"){
          var transforms=root.GetComponentsInChildren<Transform>(true);var corner=transforms.FirstOrDefault(t=>t.name=="Corner Bookcase");
          if(corner==null||corner.childCount!=2||transforms.Any(t=>t.name=="SM_Bld_Bookshelf_03_Insert_03"))failures.Add(name+": corner bookcase assembly is incomplete");
          if(corner!=null)foreach(Transform shelf in corner){var renderers=shelf.GetComponentsInChildren<MeshRenderer>(true);var body=renderers.First(r=>!r.name.Contains("Insert"));var bounds=body.bounds;bounds.Expand(.3f);foreach(var insert in renderers.Where(r=>r.name.Contains("Insert")))if(!bounds.Contains(insert.bounds.min)||!bounds.Contains(insert.bounds.max))failures.Add(name+": book insert outside its supporting frame");}
          if(transforms.Any(t=>t.name=="WineRack1"))failures.Add(name+": obsolete desk obstruction");
        }
      }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    if(failures.Count>0)throw new Exception("FAILURES ("+failures.Count+"):\n"+string.Join("\n",failures.Take(50)));
    return "Passed "+density+" floor and "+wallDensity+" wall UV-density triangles, "+seams+" visible wall/upper-wall seam probes, and "+spawnPoints+" supported library item spawn points";
  }
}




