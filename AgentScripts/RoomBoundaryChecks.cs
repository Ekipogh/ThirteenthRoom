using UnityEngine;using UnityEditor;using System;using System.Linq;using System.Collections.Generic;
public static class RoomBoundaryChecks {
    static Rect R(float x0,float z0,float x1,float z1)=>Rect.MinMaxRect(x0,z0,x1,z1);
    public static readonly Dictionary<string,Rect[]> Shapes=new Dictionary<string,Rect[]> {
      {"B_Lavatory",new[]{R(-5.5f,7,5.5f,18.75f)}},
      {"B_Library",new[]{R(-14,-14,14,8),R(-5,8,5,18.75f)}},
      {"B_RitualRoom",new[]{R(-13,-14,13,7),R(-5,7,5,18.75f)}},
      {"CN_Office",new[]{R(-16,-16,12,10),R(-18.75f,-5,12,5),R(-5,10,5,18.75f)}},
      {"CN_Storage",new[]{R(-18.75f,-18.75f,18.75f,18.75f)}},
      {"CN_UtilityRoom",new[]{R(-18.75f,-18.75f,18.75f,18.75f)}},
      {"CR_Corridor",new[]{R(-4.5f,-18.75f,4.5f,18.75f)}},
      {"CR_Crypt",new[]{R(-4.5f,-18.75f,4.5f,18.75f),R(-14,-13,14,-5),R(-14,5,14,13),R(-14,-5,-4.5f,5)}},
      {"CR_Gallery",new[]{R(-5,-18.75f,5,18.75f),R(-12,-14,-5,-3),R(5,3,12,14)}},
      {"CR_Laboratory",new[]{R(-5,-18.75f,5,18.75f),R(-16,-12,-5,10),R(5,-4,11,9)}},
      {"I_DinningHall",new[]{R(-15.5f,-15.5f,15.5f,15.5f),R(-5,-18.75f,5,18.75f),R(-18.75f,-5,18.75f,5)}},
      {"I_GardenRoom",new[]{R(-6,-18.75f,6,18.75f),R(-18.75f,-6,18.75f,6),R(-15,-14,14,13)}},
      {"J_GardenRoom",new[]{R(-14,-18.75f,14,14),R(-18.75f,-5,18.75f,5),R(-5,14,5,18.75f)}},
      {"J_Bedroom",new[]{R(-18.75f,-5,18.75f,5),R(-5,5,5,18.75f),R(-15,-16,3,-5),R(3,-13,13,-5)}},
      {"J_Cellar",new[]{R(-18.75f,-18.75f,18.75f,18.75f)}},
      {"J_DungeonRoom",new[]{R(-18.75f,-5,18.75f,5),R(-5,5,5,18.75f),R(-16,-15,16,-5),R(-12,5,12,12)}},
      {"J_MusicRoom",new[]{R(-18.75f,-5,18.75f,5),R(-5,5,5,18.75f),R(-16,-16,8,-5),R(8,-10,15,10)}}
    };
    struct Triangle {public Vector3 a,b,c;}
    sealed class Surface {public Bounds bounds;public List<Triangle> triangles=new List<Triangle>();}
    static Surface SurfaceOf(MeshFilter filter){var result=new Surface{bounds=filter.GetComponent<MeshRenderer>().bounds};var mesh=filter.sharedMesh;var vertices=mesh.vertices;var mats=filter.GetComponent<MeshRenderer>().sharedMaterials;
      for(int s=0;s<mesh.subMeshCount;s++){if(mats[Math.Min(s,mats.Length-1)].name=="Transparent")continue;var indices=mesh.GetTriangles(s);for(int i=0;i<indices.Length;i+=3)result.triangles.Add(new Triangle{a=filter.transform.TransformPoint(vertices[indices[i]]),b=filter.transform.TransformPoint(vertices[indices[i+1]]),c=filter.transform.TransformPoint(vertices[indices[i+2]])});}return result;
    }
    static bool Hit(Ray ray,IEnumerable<Surface> surfaces,float maximum,out float nearest){nearest=maximum;bool found=false;foreach(var surface in surfaces){if(!surface.bounds.IntersectRay(ray,out float entry)||entry>nearest)continue;foreach(var t in surface.triangles){var e1=t.b-t.a;var e2=t.c-t.a;var h=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,h);if(det<1e-7f)continue;float inv=1/det;var s=ray.origin-t.a;float u=Vector3.Dot(s,h)*inv;if(u<-.00002f||u>1.00002f)continue;var q=Vector3.Cross(s,e1);float v=Vector3.Dot(ray.direction,q)*inv;if(v<-.00002f||u+v>1.00002f)continue;float distance=Vector3.Dot(e2,q)*inv;if(distance>=0&&distance<nearest){nearest=distance;found=true;}}}return found;}
    static bool Inside(Rect[] rects,Vector2 p)=>rects.Any(r=>p.x>r.xMin&&p.x<r.xMax&&p.y>r.yMin&&p.y<r.yMax);
    public static string Run(){if(Application.isPlaying)throw new Exception("Run in Edit Mode");var failures=new List<string>();int corners=0,diagonals=0,ceilingRays=0,doorProbes=0;
      Shapes["CN_Kitchen"]=new[]{R(-18.75f,-7.29f,8.29f,18.75f)};Shapes["Entrance"]=new[]{R(-18.75f,-18.75f,18.75f,18.75f)};Shapes["Landing"]=Shapes["Entrance"];
      foreach(var pair in Shapes){var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Building/"+pair.Key+".prefab");try{
        var architecture=root.transform.Find("Authored Architecture");var walls=(architecture==null?root.transform.Find("Wall").GetComponentsInChildren<MeshFilter>().AsEnumerable():architecture.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Wall"||f.name=="Upper wall")).Select(SurfaceOf).ToList();
        var ceiling=architecture==null?null:new[]{SurfaceOf(architecture.Find("Ceiling strip").GetComponent<MeshFilter>())};
        var xs=pair.Value.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct();var zs=pair.Value.SelectMany(r=>new[]{r.yMin,r.yMax}).Distinct();
        foreach(float x in xs)foreach(float z in zs){var p=new Vector2(x,z);var quadrants=new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(1,1)};var inside=quadrants.Where(q=>Inside(pair.Value,p+q*.02f)).ToArray();if(inside.Length!=1&&inside.Length!=3)continue;
          var inward=(inside.Length==1?inside[0]:-quadrants.First(q=>!Inside(pair.Value,p+q*.02f))).normalized;var tangent=new Vector2(-inward.y,inward.x);corners++;
          foreach(float shift in new[]{-.12f,0,.12f})foreach(float height in new[]{.3f,4.5f,8.8f,9.2f,12.5f,13.4f}){var from=p+inward*1.4f+tangent*shift;var ray=new Ray(new Vector3(from.x,height,from.y),new Vector3(-inward.x,0,-inward.y));if(!Hit(ray,walls,3,out _))failures.Add(pair.Key+": diagonal corner gap at "+p+" height="+height+" shift="+shift);diagonals++;}
        }
        // Ceiling tests use front-facing triangles: a two-sided CPU hit must not hide inverted faces.
        if(architecture!=null)for(float x=-18.5f;x<18.6f;x+=1.25f)for(float z=-18.5f;z<18.6f;z+=1.25f)if(Inside(pair.Value,new Vector2(x,z))){if(!Hit(new Ray(new Vector3(x,1,z),Vector3.up),ceiling,13,out float distance)||Mathf.Abs(distance-12.5f)>.01f)failures.Add(pair.Key+": ceiling hole or inverted face at "+new Vector2(x,z));ceilingRays++;}
        foreach(var door in root.transform.Cast<Transform>().Where(t=>t.name.Contains("Wall_Door"))){var renderers=door.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);var all=walls.Concat(door.GetComponentsInChildren<MeshFilter>().Select(SurfaceOf)).ToArray();bool horizontal=Mathf.Abs(bounds.center.z)>Mathf.Abs(bounds.center.x);var inward=horizontal?new Vector3(0,0,-Mathf.Sign(bounds.center.z)):new Vector3(-Mathf.Sign(bounds.center.x),0,0);var along=horizontal?Vector3.right:Vector3.forward;
          foreach(float side in new[]{-1f,1f})foreach(float height in new[]{.3f,4.5f,8.8f,9.2f}){
            var from=bounds.center;from.y=height;from+=inward*2;float[] distances=new float[2];bool both=true;
            for(int i=0;i<2;i++){var origin=from+along*(side*(3.75f+(i==0?-.02f:.02f)));if(!Hit(new Ray(origin,-inward),all,3,out distances[i])){failures.Add(pair.Key+": doorway seam hole "+door.name+" height="+height);both=false;}doorProbes++;}
            if(both&&Mathf.Abs(distances[0]-distances[1])>.025f)failures.Add(pair.Key+": doorway wall face offset "+Mathf.Abs(distances[0]-distances[1]));
          }
        }
      }finally{PrefabUtility.UnloadPrefabContents(root);}}
      if(failures.Count>0)throw new Exception(failures.Count+" failures:\n"+string.Join("\n",failures.Take(16)));
      return corners+" corners / "+diagonals+" front-facing diagonal probes, "+ceilingRays+" ceiling probes, "+doorProbes+" doorway seam probes passed";
    }
}

