using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class RoomLayoutRepair {
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
    struct Vertex {
      public Vector3 p,n;public Vector2 uv;public Vector4 tangent;public Color color;
      public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.LerpUnclamped(a.p,b.p,t),n=Vector3.LerpUnclamped(a.n,b.n,t).normalized,uv=Vector2.LerpUnclamped(a.uv,b.uv,t),tangent=Vector4.LerpUnclamped(a.tangent,b.tangent,t),color=Color.LerpUnclamped(a.color,b.color,t)};
    }
    sealed class MeshBuilder {
      public List<Vector3> positions=new List<Vector3>(),normals=new List<Vector3>();public List<Vector2> uv=new List<Vector2>();public List<Vector4> tangents=new List<Vector4>();public List<Color> colors=new List<Color>();
      public List<Material> materials=new List<Material>();public List<List<int>> triangles=new List<List<int>>();
      static List<Vertex> Cut(List<Vertex> input,int axis,float value,bool greater){
        var output=new List<Vertex>();if(input.Count==0)return output;var a=input[input.Count-1];float da=(greater?1:-1)*(a.p[axis]-value);bool ina=da>=-.000001f;
        foreach(var b in input){float db=(greater?1:-1)*(b.p[axis]-value);bool inb=db>=-.000001f;if(ina!=inb)output.Add(Vertex.Lerp(a,b,da/(da-db)));if(inb)output.Add(b);a=b;da=db;ina=inb;}return output;
      }
      static List<Vertex> CutPlane(List<Vertex> input,Vector4 plane){
        var output=new List<Vertex>();if(input.Count==0)return output;
        float Distance(Vertex v)=>plane.x*v.p.x+plane.y*v.p.y+plane.z*v.p.z+plane.w;
        var a=input[input.Count-1];float da=Distance(a);bool ina=da>=-.000001f;
        foreach(var b in input){float db=Distance(b);bool inb=db>=-.000001f;if(ina!=inb)output.Add(Vertex.Lerp(a,b,da/(da-db)));if(inb)output.Add(b);a=b;da=db;ina=inb;}return output;
      }
      public void Add(Mesh source,Matrix4x4 matrix,Material[] mats,Rect[] pieces,Rect clip,Vector4[] miters=null){
        var ps=source.vertices;var ns=source.normals;var us=source.uv;var ts=source.tangents;var cs=source.colors;
        Vertex Get(int i)=>new Vertex{p=matrix.MultiplyPoint3x4(ps[i]),n=ns.Length==ps.Length?matrix.MultiplyVector(ns[i]).normalized:Vector3.up,uv=us.Length==ps.Length?us[i]:Vector2.zero,tangent=ts.Length==ps.Length?new Vector4(matrix.MultiplyVector((Vector3)ts[i]).normalized.x,matrix.MultiplyVector((Vector3)ts[i]).normalized.y,matrix.MultiplyVector((Vector3)ts[i]).normalized.z,ts[i].w):new Vector4(1,0,0,1),color=cs.Length==ps.Length?cs[i]:Color.white};
        for(int sub=0;sub<source.subMeshCount;sub++){
          var mat=mats[Math.Min(sub,mats.Length-1)];int slot=materials.IndexOf(mat);if(slot<0){slot=materials.Count;materials.Add(mat);triangles.Add(new List<int>());}var indices=source.GetTriangles(sub);
          for(int i=0;i<indices.Length;i+=3){var initial=new List<Vertex>{Get(indices[i]),Get(indices[i+1]),Get(indices[i+2])};
            foreach(var piece in pieces){float x0=Mathf.Max(piece.xMin,clip.xMin),x1=Mathf.Min(piece.xMax,clip.xMax),z0=Mathf.Max(piece.yMin,clip.yMin),z1=Mathf.Min(piece.yMax,clip.yMax);if(x1-x0<.00001f||z1-z0<.00001f)continue;
              var poly=Cut(initial,0,x0,true);poly=Cut(poly,0,x1,false);poly=Cut(poly,2,z0,true);poly=Cut(poly,2,z1,false);poly=Cut(poly,1,13.5f,false);if(miters!=null)foreach(var plane in miters)poly=CutPlane(poly,plane);if(poly.Count<3)continue;
              for(int k=1;k<poly.Count-1;k++){var a=poly[0];var b=poly[k];var c=poly[k+1];if(Vector3.Cross(b.p-a.p,c.p-a.p).sqrMagnitude<1e-12f)continue;foreach(var v in new[]{a,b,c}){triangles[slot].Add(positions.Count);positions.Add(v.p);normals.Add(v.n);uv.Add(v.uv);tangents.Add(v.tangent);colors.Add(v.color);}}
            }
          }
        }
      }
      public Mesh Finish(string name){var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTangents(tangents);mesh.SetColors(colors);mesh.subMeshCount=materials.Count;for(int i=0;i<materials.Count;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateBounds();return mesh;}
    }
    struct Edge {public bool horizontal;public float line,a,b;public Vector2 inward;}
    static bool Contains(Rect[] rects,float x,float z)=>rects.Any(r=>x>=r.xMin-.00001f&&x<=r.xMax+.00001f&&z>=r.yMin-.00001f&&z<=r.yMax+.00001f);
    static Bounds BoundsOf(Transform t){var renderers=t.GetComponentsInChildren<MeshRenderer>(true);if(renderers.Length==0)return new Bounds(t.position,Vector3.zero);var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
    static List<Rect> Partition(Rect[] rects,out List<Edge> edges){
      var xs=rects.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(x=>x).ToArray();var zs=rects.SelectMany(r=>new[]{r.yMin,r.yMax}).Distinct().OrderBy(x=>x).ToArray();var cells=new bool[xs.Length-1,zs.Length-1];edges=new List<Edge>();var pieces=new List<Rect>();
      for(int x=0;x<xs.Length-1;x++)for(int z=0;z<zs.Length-1;z++)cells[x,z]=Contains(rects,(xs[x]+xs[x+1])/2,(zs[z]+zs[z+1])/2);
      for(int x=0;x<xs.Length-1;x++)for(int z=0;z<zs.Length-1;z++)if(cells[x,z]){
        pieces.Add(R(xs[x],zs[z],xs[x+1],zs[z+1]));
        if(z==0||!cells[x,z-1])edges.Add(new Edge{horizontal=true,line=zs[z],a=xs[x],b=xs[x+1],inward=Vector2.up});
        if(z==zs.Length-2||!cells[x,z+1])edges.Add(new Edge{horizontal=true,line=zs[z+1],a=xs[x],b=xs[x+1],inward=Vector2.down});
        if(x==0||!cells[x-1,z])edges.Add(new Edge{line=xs[x],a=zs[z],b=zs[z+1],inward=Vector2.right});
        if(x==xs.Length-2||!cells[x+1,z])edges.Add(new Edge{line=xs[x+1],a=zs[z],b=zs[z+1],inward=Vector2.left});
      }
      var merged=new List<Edge>();foreach(var group in edges.GroupBy(e=>(e.horizontal,e.line,e.inward.x,e.inward.y))){Edge current=default;bool first=true;foreach(var edge in group.OrderBy(e=>e.a)){if(first){current=edge;first=false;}else if(Mathf.Abs(current.b-edge.a)<.001f)current.b=edge.b;else{merged.Add(current);current=edge;}}if(!first)merged.Add(current);}edges=merged;return pieces;
    }
    static Mesh StoreMesh(string path,Mesh mesh){
      var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m=>m.name==mesh.name);
      if(existing!=null){
        // CopySerialized alone left stale GPU index/vertex buffers in the live Scene View.
        existing.Clear(false);existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.tangents=mesh.tangents;existing.colors=mesh.colors;existing.subMeshCount=mesh.subMeshCount;
        for(int i=0;i<mesh.subMeshCount;i++)existing.SetTriangles(mesh.GetTriangles(i),i);existing.RecalculateBounds();existing.UploadMeshData(false);
        UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
      }
      mesh.UploadMeshData(false);if(AssetDatabase.LoadMainAssetAtPath(path)==null)AssetDatabase.CreateAsset(mesh,path);else AssetDatabase.AddObjectToAsset(mesh,path);return mesh;
    }
    static Vector2 Endpoint(Edge e,bool start)=>e.horizontal?new Vector2(start?e.a:e.b,e.line):new Vector2(e.line,start?e.a:e.b);
    static Edge Adjacent(Edge e,bool start,List<Edge> edges)=>edges.First(other=>other.horizontal!=e.horizontal&&(Vector2.Distance(Endpoint(e,start),Endpoint(other,true))<.001f||Vector2.Distance(Endpoint(e,start),Endpoint(other,false))<.001f));
    static Vector4 Miter(Edge e,bool start,List<Edge> edges){
      var point=Endpoint(e,start);var other=Adjacent(e,start,edges);var along=e.horizontal?Vector2.right:Vector2.up;if(!start)along=-along;
      var normal=along-Mathf.Sign(Vector2.Dot(along,other.inward))*e.inward;
      return new Vector4(normal.x,0,normal.y,-Vector2.Dot(normal,point));
    }
    static void BuildShell(GameObject root){
      var old=root.transform.Find("Authored Architecture");if(old==null)return;
      var filters=old.GetComponentsInChildren<MeshFilter>(true);
      MeshFilter Find(string prefix){var found=filters.FirstOrDefault(f=>f.sharedMesh.name.StartsWith(prefix));if(found!=null)return found;
        string path=prefix=="SM_Bld_Base_Wall_Half_01"||prefix=="SM_Bld_Architrave_01"?"Assets/Prefabs/Building/UpperWall.prefab":"Assets/Thirdparty/PolygonHorrorMansion/Prefabs/Buildings/"+prefix+".prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh.name.StartsWith(prefix));
      }
      var floor=Find("SM_Bld_Base_Floor_01");var ceiling=Find("SM_Bld_Base_Ceiling_01");var lower=Find("SM_Bld_Base_Wall_01");var half=Find("SM_Bld_Base_Wall_Half_01");var trim=Find("SM_Bld_Architrave_01");
      var fMat=old.Find("Floor strip").GetComponentInChildren<MeshRenderer>().sharedMaterials;var cMat=old.Find("Ceiling strip").GetComponentInChildren<MeshRenderer>().sharedMaterials;
      var wMat=old.Find("Wall").GetComponentInChildren<MeshRenderer>().sharedMaterials;var uMat=old.Find("Upper wall").GetComponentInChildren<MeshRenderer>().sharedMaterials;
      if(uMat.Length>2)uMat=uMat.Take(2).ToArray();
      var floorBuilder=new MeshBuilder();var ceilingBuilder=new MeshBuilder();var wallBuilder=new MeshBuilder();var upperBuilder=new MeshBuilder();
      var rects=Shapes[root.name];var pieces=Partition(rects,out var edges).ToArray();
      for(float x=-18.75f;x<18.74f;x+=7.5f)for(float z=-18.75f;z<18.74f;z+=7.5f){var cell=R(x,z,x+7.5f,z+7.5f);
        floorBuilder.Add(floor.sharedMesh,Matrix4x4.TRS(new Vector3(x+7.5f,0,z),Quaternion.identity,Vector3.one*3),fMat,pieces,cell);
        ceilingBuilder.Add(ceiling.sharedMesh,Matrix4x4.TRS(new Vector3(x+7.5f,13.5f,z),Quaternion.identity,Vector3.one*3),cMat,pieces,cell);
      }
      var architecture=new GameObject("Repaired Architecture");architecture.transform.SetParent(root.transform,false);
      var doorBounds=root.transform.Cast<Transform>().Where(t=>t.name.Contains("Wall_Door")).Select(BoundsOf).ToArray();
      foreach(var edge in edges){
        var normal=new Vector3(edge.inward.x,0,edge.inward.y);var rotation=Quaternion.LookRotation(normal,Vector3.up);var miters=new[]{Miter(edge,true,edges),Miter(edge,false,edges)};
        var cuts=new List<float>{edge.a,edge.b};foreach(var door in doorBounds){float line=edge.horizontal?door.center.z:door.center.x;if(Mathf.Abs(line-edge.line)>.02f)continue;float a=edge.horizontal?door.min.x:door.min.z,b=edge.horizontal?door.max.x:door.max.z;if(a>edge.a&&a<edge.b)cuts.Add(a);if(b>edge.a&&b<edge.b)cuts.Add(b);}cuts=cuts.Distinct().OrderBy(v=>v).ToList();
        for(int ci=0;ci<cuts.Count-1;ci++){float a=cuts[ci],b=cuts[ci+1];bool doorway=doorBounds.Any(d=>Mathf.Abs((edge.horizontal?d.center.z:d.center.x)-edge.line)<.02f&&a>=(edge.horizontal?d.min.x:d.min.z)-.01f&&b<=(edge.horizontal?d.max.x:d.max.z)+.01f);
          if(!doorway){var go=new GameObject("Wall collision");go.transform.SetParent(architecture.transform,false);var box=go.AddComponent<BoxCollider>();box.center=edge.horizontal?new Vector3((a+b)/2,6.75f,edge.line)+normal*.17f:new Vector3(edge.line,6.75f,(a+b)/2)+normal*.17f;box.size=edge.horizontal?new Vector3(b-a,13.5f,.34f):new Vector3(.34f,13.5f,b-a);go.AddComponent<InteractionBlocker>();}
          for(float start=-18.75f;start<18.74f;start+=7.5f){float end=start+7.5f,lo=Mathf.Max(a-(ci==0?1.1f:0),start),hi=Mathf.Min(b+(ci==cuts.Count-2?1.1f:0),end);if(hi-lo<.0001f)continue;
            var center=edge.horizontal?new Vector3((start+end)/2,0,edge.line):new Vector3(edge.line,0,(start+end)/2);
            var matrix=Matrix4x4.TRS(center,rotation,Vector3.one)*Matrix4x4.Translate(new Vector3(3.75f,0,0))*Matrix4x4.Scale(Vector3.one*3);
            var clip=edge.horizontal?R(lo,edge.line-.001f,hi,edge.line+1.1f):R(edge.line-.001f,lo,edge.line+1.1f,hi);
            if(edge.horizontal&&edge.inward.y<0)clip=R(lo,edge.line-1.1f,hi,edge.line+.001f);
            if(!edge.horizontal&&edge.inward.x<0)clip=R(edge.line-1.1f,lo,edge.line+.001f,hi);
            if(!doorway)wallBuilder.Add(lower.sharedMesh,matrix,wMat,pieces,clip,miters);
            upperBuilder.Add(half.sharedMesh,Matrix4x4.Translate(Vector3.up*9)*matrix,uMat,pieces,clip,miters);
            upperBuilder.Add(trim.sharedMesh,Matrix4x4.Translate(Vector3.up*(9-4.36f))*matrix,trim.GetComponent<MeshRenderer>().sharedMaterials,pieces,clip,miters);
          }
        }
      }
      var filledCorners=new HashSet<Vector2>();foreach(var edge in edges)foreach(bool start in new[]{true,false}){
        var other=Adjacent(edge,start,edges);var along=(edge.horizontal?Vector2.right:Vector2.up)*(start?1:-1);var point=Endpoint(edge,start);
        if(Vector2.Dot(along,other.inward)>=0||!filledCorners.Add(point))continue;
        var go=new GameObject("Corner collision");go.transform.SetParent(architecture.transform,false);var col=go.AddComponent<BoxCollider>();var inward=edge.inward+other.inward;
        col.center=new Vector3(point.x+inward.x*.16875f,6.75f,point.y+inward.y*.16875f);col.size=new Vector3(.3375f,13.5f,.3375f);go.AddComponent<InteractionBlocker>();
      }
      Directory.CreateDirectory("Assets/Generated/RoomLayouts");AssetDatabase.Refresh();string asset="Assets/Generated/RoomLayouts/"+root.name+".asset";
      foreach(var pair in new[]{("Floor strip",floorBuilder),("Ceiling strip",ceilingBuilder),("Wall",wallBuilder),("Upper wall",upperBuilder)}){var go=new GameObject(pair.Item1);go.transform.SetParent(architecture.transform,false);go.AddComponent<MeshFilter>().sharedMesh=StoreMesh(asset,pair.Item2.Finish(pair.Item1));var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=pair.Item2.materials.ToArray();renderer.shadowCastingMode=ShadowCastingMode.TwoSided;}
      foreach(var piece in pieces){var col=architecture.AddComponent<BoxCollider>();col.center=new Vector3(piece.center.x,-.075f,piece.center.y);col.size=new Vector3(piece.width,.15f,piece.height);
        var ceilingCollision=architecture.transform.Find("Ceiling strip").gameObject.AddComponent<BoxCollider>();ceilingCollision.center=new Vector3(piece.center.x,13.575f,piece.center.y);ceilingCollision.size=new Vector3(piece.width,.15f,piece.height);
      }
      UnityEngine.Object.DestroyImmediate(old.gameObject);architecture.name="Authored Architecture";
    }
    static void Place(Transform t,Vector3 center,float yaw,bool floor=false){if(t==null)return;t.rotation=Quaternion.Euler(0,yaw,0);var b=BoundsOf(t);if(floor)center.y=b.extents.y;t.position+=center-b.center;}
    static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
    static IEnumerable<Transform> PropRoots(GameObject root){
      foreach(var name in new[]{"Objects/Furniture","Objects/Props","Objects/Interactables","Room Details","Furnishings"}){var group=root.transform.Find(name);if(group!=null)foreach(Transform t in group)yield return t;}
    }
    static void MoveWithContents(GameObject root,Transform support,Vector3 center,float yaw){
      if(support==null)return;var old=BoundsOf(support);var oldPos=support.position;var oldRotation=support.rotation;
      var contents=PropRoots(root).Where(t=>!t.IsChildOf(support)&&t!=support&&t.GetComponent<LightSwitchInteractable>()==null).Where(t=>{var b=BoundsOf(t);return b.min.y>=old.max.y-.22f&&b.min.y<old.max.y+1.2f&&b.center.x>old.min.x-.1f&&b.center.x<old.max.x+.1f&&b.center.z>old.min.z-.1f&&b.center.z<old.max.z+.1f;}).ToArray();
      Place(support,center,yaw);var rotation=support.rotation*Quaternion.Inverse(oldRotation);foreach(var t in contents){t.position=support.position+rotation*(t.position-oldPos);t.rotation=rotation*t.rotation;}
    }
    static void Library(GameObject root){
      var furniture=root.transform.Find("Objects/Furniture");var shelves=furniture.Cast<Transform>().Where(t=>t.name.StartsWith("Bookshelf")).ToArray();
      var remove=shelves.Where(t=>Mathf.Abs(BoundsOf(t).center.x)<11).ToArray();foreach(var t in remove)UnityEngine.Object.DestroyImmediate(t.gameObject);
      var left=furniture.Cast<Transform>().Where(t=>t.name.StartsWith("Bookshelf")&&BoundsOf(t).center.x<0).OrderBy(t=>BoundsOf(t).center.z).ToArray();var right=furniture.Cast<Transform>().First(t=>t.name.StartsWith("Bookshelf")&&BoundsOf(t).center.x>0);
      Place(left[0],new Vector3(-12.3f,4.51f,-6),90,true);Place(left[1],new Vector3(-12.3f,4.51f,2.5f),90,true);Place(right,new Vector3(12.3f,4.51f,0),270,true);
      string[] desks={"SM_Prop_Desk_01","SM_Prop_Desk_01 (1)","SM_Prop_Desk_01 (2)"};var centers=new[]{new Vector3(-7,1.25f,1),new Vector3(7,1.25f,1),new Vector3(0,1.25f,-10.5f)};float[] angles={270,90,0};
      for(int i=0;i<3;i++){var desk=Find(root,desks[i]);MoveWithContents(root,desk,centers[i],angles[i]);var chair=Find(root,"SM_Prop_Chair_02"+(i==0?"":" ("+i+")"));Place(chair,i==0?new Vector3(-4.7f,0,1):i==1?new Vector3(4.7f,0,1):new Vector3(0,0,-7.8f),i==0?270:i==1?90:180,true);}
      Place(Find(root,"SM_Prop_Couch_01"),new Vector3(8.5f,0,-11.4f),0,true);Place(Find(root,"SM_Prop_Cabinet_02"),new Vector3(-9.5f,0,-12.4f),0,true);
      MoveWithContents(root,Find(root,"Reading Corner Side Table"),new Vector3(4.1f,.96f,-9),0);
      Place(Find(root,"SM_Prop_Ladder_02"),new Vector3(10,0,-5.8f),270,true);
      foreach(var lampName in new[]{"Desklamp","Desklamp (1)"}){var lamp=Find(root,lampName);int i=lampName=="Desklamp"?0:1;Place(lamp,new Vector3(i==0?-7:7,3.8f,2.5f),i==0?90:270);}
      Place(Find(root,"SM_Prop_Book_Pile_03"),new Vector3(-5.5f,0,-12.2f),90,true);
      Place(Find(root,"SM_Prop_Book_Pile_02"),new Vector3(-5.5f,0,-10),90,true);
      Place(Find(root,"SM_Prop_Book_Pile_05"),new Vector3(10,0,5.8f),90,true);
      Place(Find(root,"SM_Prop_Book_Pile_05 (1)"),new Vector3(10,0,-7.6f),90,true);
      // Keep the existing spawn node and restore useful table-supported spawn locations.
      var parent=root.transform.Find("ItemSpawnPoints");var template=parent.GetComponentInChildren<ItemSpawnPoint>(true);var points=new[]{new Vector3(-8.05f,2.53f,-.2f),new Vector3(8.05f,2.53f,-.2f),new Vector3(1.8f,2.53f,-10.5f)};
      var original=template.gameObject;var existing=parent.GetComponentsInChildren<ItemSpawnPoint>(true);for(int i=0;i<points.Length;i++){var node=i<existing.Length?existing[i].gameObject:UnityEngine.Object.Instantiate(original,parent);node.name="ItemSpawnPoint"+(i==0?"":" "+(i+1));node.transform.position=points[i];}
      var serialized=new SerializedObject(root.GetComponent<Room>());serialized.FindProperty("itemSpawnPointsParent").objectReferenceValue=parent.gameObject;serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Ritual(GameObject root){
      Place(Find(root,"Bookshelf_1"),new Vector3(-8.5f,0,-12.4f),0,true);Place(Find(root,"Bookshelf_2"),new Vector3(0,0,-12.4f),0,true);
      MoveWithContents(root,Find(root,"SM_Prop_Dresser_03"),new Vector3(11.3f,1.55f,-1.5f),270);
      Place(Find(root,"SM_Prop_Mirror_02"),new Vector3(12.58f,5.5f,-1.5f),270);
    }
    static void Office(GameObject root){
      var body=Find(root,"SM_Bld_Bookshelf_03");var insert=Find(root,"SM_Bld_Bookshelf_03_Insert_03");
      if(body!=null&&insert!=null){
        var group=Find(root,"Corner Bookcase")?.gameObject;if(group==null){group=new GameObject("Corner Bookcase");group.transform.SetParent(root.transform.Find("Objects/Furniture"),false);}
        var template=Find(root,"Bookshelf_2");var north=UnityEngine.Object.Instantiate(template.gameObject,group.transform).transform;north.name="North Bookcase";
        var east=UnityEngine.Object.Instantiate(template.gameObject,group.transform).transform;east.name="East Bookcase";
        Place(north,new Vector3(7.55f,0,-14.4f),0,true);Place(east,new Vector3(10.6f,0,-9.45f),270,true);
        UnityEngine.Object.DestroyImmediate(body.gameObject);UnityEngine.Object.DestroyImmediate(insert.gameObject);
      }
      MoveWithContents(root,Find(root,"SM_Prop_Desk_01"),new Vector3(.5f,1.25f,-6),0);
      Place(Find(root,"SM_Prop_ArmChair_01"),new Vector3(.5f,0,-9.5f),0,true);Place(Find(root,"SM_Prop_Rug_01"),new Vector3(.5f,.14f,-6),0);
      var wine=Find(root,"WineRack1");if(wine!=null)UnityEngine.Object.DestroyImmediate(wine.gameObject);
      Place(Find(root,"Safe"),new Vector3(-13.5f,2.81f,-4.4f),90,true);
      Place(Find(root,"SM_Prop_ArmChair_03"),new Vector3(-7,0,-11.5f),270,true);Place(Find(root,"SM_Prop_ArmChair_03 (1)"),new Vector3(-10.8f,0,-7.5f),180,true);
      Place(Find(root,"SM_Prop_Curtain_01_Closed_01"),new Vector3(-10,5,9.3f),180);
      // Shelves and their inserts now share one transform, so future edits cannot separate them.
    }
    static void Corridor(GameObject root){
      MoveWithContents(root,Find(root,"SM_Prop_Dresser_03"),new Vector3(-3.1f,1.55f,-.5f),90);
      Place(Find(root,"SM_Prop_Mirror_01"),new Vector3(-4.0f,6.23f,-.5f),90);
      var plate=Find(root,"SM_Prop_Display_Plate_01");Place(plate,new Vector3(-3.1f,3.78f,-1.5f),90);var pb=BoundsOf(plate);plate.position+=Vector3.up*(3.12f-pb.min.y);
      MoveWithContents(root,Find(root,"SM_Prop_Shelf_01"),new Vector3(-3.65f,4.3f,15.5f),90);
      foreach(var name in new[]{"Painting2","Painting_1"}){var t=Find(root,name);if(t!=null){float yaw=name=="Painting2"?0:90;t.rotation=Quaternion.Euler(0,yaw,0);var b=BoundsOf(t);Place(t,new Vector3(-4.162f+b.extents.x+.02f,b.center.y,name=="Painting2"?-13.75f:10.25f),yaw);}}
    }

    public static string RebuildGarden(){
      const string path="Assets/Prefabs/Building/J_GardenRoom.prefab";
      Directory.CreateDirectory("Temp/GardenLayoutBackup");if(!File.Exists("Temp/GardenLayoutBackup/J_GardenRoom.prefab"))File.Copy(path,"Temp/GardenLayoutBackup/J_GardenRoom.prefab");
      var root=PrefabUtility.LoadPrefabContents(path);try{Garden(root);BuildShell(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();AssetDatabase.ImportAsset("Assets/Generated/RoomLayouts/J_GardenRoom.asset",ImportAssetOptions.ForceUpdate);return "Rebuilt garden chamber and grouped furnishings";
    }
    static void Garden(GameObject root){
      var parent=root.transform.Find("Furnishings");var all=parent.Cast<Transform>().ToArray();
      Place(all.First(t=>t.name=="Indoor Fountain"),new Vector3(0,0,0),0,true);
      for(int i=1;i<=4;i++){
        var bed=all.First(t=>t.name=="Raised Garden Bed "+i);var related=all.Where(t=>t.name=="Garden Bed Foliage "+i||t.name.StartsWith("Raised Garden Bed "+i+" Fern")).ToArray();
        var previous=bed.position;float x=i<=2?-11:11,z=(i==1||i==3)?-8:8;
        Place(bed,new Vector3(x,0,z),bed.eulerAngles.y,true);var delta=bed.position-previous;foreach(var t in related)t.position+=delta;
      }
      var benches=all.Where(t=>t.name.StartsWith("Garden Viewing Bench")).ToArray();
      Place(benches[0],new Vector3(0,0,-15.5f),0,true);Place(benches[1],new Vector3(-5,0,10.5f),180,true);Place(benches[2],new Vector3(5,0,10.5f),180,true);
      var plants=all.Where(t=>t.name.StartsWith("Plant")).ToArray();var locations=new[]{new Vector3(-6,0,-10),new Vector3(6,0,-10),new Vector3(-7,0,5),new Vector3(7,0,5)};
      for(int i=0;i<plants.Length;i++)Place(plants[i],locations[i],0,true);
      Find(root,"MonsterPoint").localPosition=new UnityEngine.Vector3(0,0,-7.5f);
      Find(root,"PlayerSpawnPoint").localPosition=new UnityEngine.Vector3(0,0,15.5f);
      // Fit tracking volumes to the same union as the accessible architecture.
      foreach(var box in root.GetComponents<BoxCollider>().Where(c=>c.isTrigger).ToArray())UnityEngine.Object.DestroyImmediate(box);
      foreach(var rect in Shapes["J_GardenRoom"]){var box=root.AddComponent<BoxCollider>();box.isTrigger=true;box.center=new Vector3(rect.center.x,6.5f,rect.center.y);box.size=new Vector3(rect.width,13,rect.height);}
    }

    public static string RebuildShells(){
      if(Application.isPlaying)throw new Exception("Requires Edit Mode");var notes=new List<string>();Directory.CreateDirectory("Temp/CornerRepairBackup");
      foreach(string name in Shapes.Keys){string path="Assets/Prefabs/Building/"+name+".prefab";if(!File.Exists("Temp/CornerRepairBackup/"+name+".prefab"))File.Copy(path,"Temp/CornerRepairBackup/"+name+".prefab");
        var root=PrefabUtility.LoadPrefabContents(path);try{BuildShell(root);PrefabUtility.SaveAsPrefabAsset(root,path);notes.Add(name+": mitered shell and GPU buffers rebuilt");}finally{PrefabUtility.UnloadPrefabContents(root);}
      }
      AssetDatabase.SaveAssets();foreach(string name in Shapes.Keys)AssetDatabase.ImportAsset("Assets/Generated/RoomLayouts/"+name+".asset",ImportAssetOptions.ForceUpdate);SceneView.RepaintAll();return string.Join("\n",notes);
    }
    public static string Run(){
      if(Application.isPlaying)throw new Exception("Requires Edit Mode");
      Directory.CreateDirectory("Temp/RoomLayoutRepairBackup");var reports=new List<string>();
      foreach(var name in Shapes.Keys){string path="Assets/Prefabs/Building/"+name+".prefab";if(!File.Exists("Temp/RoomLayoutRepairBackup/"+name+".prefab"))File.Copy(path,"Temp/RoomLayoutRepairBackup/"+name+".prefab");
        var root=PrefabUtility.LoadPrefabContents(path);try{
          var doors=root.GetComponentsInChildren<PointNode>(true).Where(n=>n.name.Contains("Door")).ToDictionary(n=>n.name,n=>n.transform.position);
          if(name=="J_GardenRoom")Garden(root);BuildShell(root);if(name=="B_Library")Library(root);if(name=="B_RitualRoom")Ritual(root);if(name=="CN_Office")Office(root);if(name=="CR_Corridor")Corridor(root);
          foreach(var p in root.GetComponentsInChildren<PointNode>(true).Where(n=>n.name.Contains("Door")))if(Vector3.Distance(p.transform.position,doors[p.name])>.00001f)throw new Exception("Door moved");
          PrefabUtility.SaveAsPrefabAsset(root,path);reports.Add(name+": UV-preserving shell saved");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
      }
      AssetDatabase.SaveAssets();return string.Join("\n",reports);
    }
}




