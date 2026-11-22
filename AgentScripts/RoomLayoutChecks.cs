using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEngine.AI;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Invoke RoomLayoutChecks.Run() in Edit Mode through Unity Pipeline script run.
public static class RoomLayoutChecks
{
    static void ValidateNavigation(GameObject root,List<string> notes) {
      var originalPosition=root.transform.position;root.transform.position=new Vector3(10000,0,10000);
      NavMeshSurface surface=null;NavMeshDataInstance instance=default;
      try {
        foreach(var col in root.GetComponentsInChildren<Collider>())if(!col.isTrigger&&col.name.StartsWith("SM_Prop_")&&!col.name.Contains("Rug")) {var modifier=col.GetComponent<NavMeshModifier>();if(modifier==null)modifier=col.gameObject.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;}
        surface=root.AddComponent<NavMeshSurface>();surface.agentTypeID=0;surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.1f;Physics.SyncTransforms();surface.BuildNavMesh();instance=NavMesh.AddNavMeshData(surface.navMeshData,root.transform.position,root.transform.rotation);
        var targets=new List<Vector3>();
        foreach(var marker in root.GetComponentsInChildren<PointNode>(true)){
          if(marker.GetComponent<ItemSpawnPoint>()!=null)continue;
          var p=marker.transform.position;if(marker.name.Contains("Door")){var local=p-root.transform.position;p-=new Vector3(Mathf.Abs(local.x)>18?Mathf.Sign(local.x)*1.8f:0,0,Mathf.Abs(local.z)>18?Mathf.Sign(local.z)*1.8f:0);}
          if(!NavMesh.SamplePosition(p,out var hit,1.25f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-p.y)>1)throw new Exception(root.name+": unreachable marker "+marker.name+" "+(p-root.transform.position));targets.Add(hit.position);
        }
        int paths=0;for(int i=0;i<targets.Count;i++)for(int j=i+1;j<targets.Count;j++){var path=new NavMeshPath();if(!NavMesh.CalculatePath(targets[i],targets[j],NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception(root.name+": disconnected markers "+i+"/"+j);paths++;}
        int interactions=0;
        foreach(var interactable in root.GetComponentsInChildren<MonoBehaviour>(true).Where(m=>m is IInteractable || m is IHoldInteractable)) {
          var p=interactable.transform.position;p.y=0;bool reachable=false;
          for(float radius=1;radius<=4&&!reachable;radius+=.75f)for(int direction=0;direction<16&&!reachable;direction++){
            float a=direction*Mathf.PI/8;var spot=p+new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);
            if(!NavMesh.SamplePosition(spot,out var hit,.5f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y)>.7f)continue;
            var path=new NavMeshPath();if(NavMesh.CalculatePath(targets[0],hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)reachable=true;
          }
          if(!reachable)throw new Exception(root.name+": no interaction approach "+interactable.name);interactions++;
        }
        notes.Add(root.name+": navmesh complete, "+targets.Count+" markers, "+paths+" paths, "+interactions+" interaction approaches");
      }finally{if(instance.valid)instance.Remove();if(surface!=null){surface.RemoveData();var data=surface.navMeshData;UnityEngine.Object.DestroyImmediate(surface);if(data!=null)UnityEngine.Object.DestroyImmediate(data);}root.transform.position=originalPosition;}
    }

    public static string Run() {
      if(Application.isPlaying)throw new Exception("Run prefab checks in Edit Mode");
      var report=new List<string>();
      foreach(string baseline in Directory.GetFiles("AgentScripts/RoomLayoutBaseline","*.txt")) {
        string name=Path.GetFileNameWithoutExtension(baseline),path="Assets/Prefabs/Building/"+name+".prefab";
        var expected=File.ReadAllLines(baseline);var root=PrefabUtility.LoadPrefabContents(path);
        try {
          string guid=expected.First(l=>l.StartsWith("GUID=")).Substring(5);
          if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new Exception(name+": prefab identity changed");
          if(root.transform.position!=Vector3.zero||Quaternion.Angle(root.transform.rotation,Quaternion.identity)>.001f||root.transform.localScale!=Vector3.one)throw new Exception(name+": root transform changed");
          if("RoomType="+root.GetComponent<Room>().RoomType!=expected.First(l=>l.StartsWith("RoomType=")))throw new Exception(name+": topology changed");
          var doors=root.GetComponentsInChildren<PointNode>(true).Where(p=>p.name.Contains("Door")).ToArray();
          var originalDoors=expected.Where(l=>l.Contains(" rotation=")).ToArray();
          if(doors.Length!=originalDoors.Length)throw new Exception(name+": door count changed");
          foreach(var door in doors)if(!originalDoors.Contains(door.name+"="+door.transform.position+" rotation="+door.transform.rotation))throw new Exception(name+": door transform changed "+door.name);
          int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
          if(missing!=0)throw new Exception(name+": missing scripts");
          foreach(var t in root.GetComponentsInChildren<Transform>(true))if(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetPrefabInstanceStatus(t.gameObject)==PrefabInstanceStatus.MissingAsset)throw new Exception(name+": missing nested prefab");
          foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)){
            if(renderer.GetComponent<MeshFilter>()?.sharedMesh==null)throw new Exception(name+": missing mesh "+renderer.name);
            if(renderer.sharedMaterials.Any(m=>m==null))throw new Exception(name+": missing material "+renderer.name);
            // Doorway exterior faces are the existing shared seam, not usable room volume.
            if(renderer.transform.GetComponentsInParent<Transform>(true).Any(t=>t.name.Contains("Wall_Door")))continue;
            // Preserve the reference kitchen's existing shared exterior wall seam (up to 0.43 units).
            bool sharedSeam=name=="CN_Kitchen"&&renderer.transform.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Wall");
            var b=renderer.bounds;float limit=sharedSeam?19.2f:18.76f;
            if(b.min.x < -limit||b.max.x>limit||b.min.z < -limit||b.max.z>limit||b.max.y>13.76f)throw new Exception(name+": geometry outside cell "+renderer.name+" "+b);
          }
          foreach(var trigger in root.GetComponents<BoxCollider>())if(trigger.isTrigger){var b=trigger.bounds;if(b.min.x < -18.76f||b.max.x>18.76f||b.min.z < -18.76f||b.max.z>18.76f)throw new Exception(name+": tracking volume outside cell");}
          ValidateNavigation(root,report);
        } finally{PrefabUtility.UnloadPrefabContents(root);}
      }
      return string.Join("\n",report);
    }
}

