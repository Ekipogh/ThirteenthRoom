using UnityEngine;using UnityEditor;using UnityEngine.AI;using Unity.AI.Navigation;using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;
// Run RoomInteractionChecks.Run() in Play Mode. Temporary fixtures are always destroyed.
public static class RoomInteractionChecks {
 public static string Run() {
 if(!Application.isPlaying)throw new Exception("Requires Play Mode");
 var flags=BindingFlags.NonPublic|BindingFlags.Static;
 int rays=0,nestedTargets=0;var failures=new List<string>();var rooms=new List<Room>();
 var fixture=new GameObject("Room interaction validation");NavMeshSurface surface=null;
 try {
 int index=0;foreach(var baseline in Directory.GetFiles("AgentScripts/RoomLayoutBaseline","*.txt")){
 var name=Path.GetFileNameWithoutExtension(baseline);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Building/"+name+".prefab");
 var copy=UnityEngine.Object.Instantiate(prefab,new Vector3(10000,0,10000+index*80),Quaternion.identity,fixture.transform);rooms.Add(copy.GetComponent<Room>());index++;
 }
 surface=fixture.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.1f;
 foreach(var col in fixture.GetComponentsInChildren<Collider>())if(!col.isTrigger&&col.name.StartsWith("SM_Prop_")&&!col.name.Contains("Rug")){var modifier=col.GetComponent<NavMeshModifier>();if(modifier==null)modifier=col.gameObject.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;}
 Physics.SyncTransforms();surface.BuildNavMesh();
var owner=typeof(PlayerInteractor).GetMethod("GetInteractionOwner",flags);var blocked=typeof(PlayerInteractor).GetMethod("IsInteractionObstructed",flags);
var player=UnityEngine.Object.FindFirstObjectByType<PlayerInteractor>();var mask=(LayerMask)typeof(PlayerInteractor).GetField("interactableLayerMask",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);
foreach(var room in rooms){foreach(var m in room.GetComponentsInChildren<MonoBehaviour>(true).Where(m=>m.enabled&&m.gameObject.activeInHierarchy&&(m is IInteractable||m is IHoldInteractable))){
 if(!m.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled&&c.gameObject.activeInHierarchy&&owner.Invoke(null,new object[]{c}) as Component==m)){nestedTargets++;continue;}
 bool found=false;
 foreach(var col in m.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy)){
  if(found)break;if(owner.Invoke(null,new object[]{col}) as Component!=m)continue;
  var target=col.bounds.center;
  for(float radius=.9f;radius<=3&&!found;radius+=.4f)for(int dir=0;dir<24&&!found;dir++){
   var floor=target;floor.y=room.transform.position.y;float a=dir*Mathf.PI/12;floor+=new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);
   if(!NavMesh.SamplePosition(floor,out var sampled,.4f,NavMesh.AllAreas)||Mathf.Abs(sampled.position.y-room.transform.position.y)>.5f)continue;
   var eye=sampled.position+Vector3.up*4.8f;var ray=new Ray(eye,(target-eye).normalized);
   if(Physics.Raycast(ray,out var hit,3f,mask,QueryTriggerInteraction.Collide)&&owner.Invoke(null,new object[]{hit.collider}) as Component==m&&!(bool)blocked.Invoke(null,new object[]{ray,hit}))found=true;
  }
 }
 if(found)rays++;else failures.Add("interaction ray "+room.name+"/"+m.name);
}}

 if(failures.Count>0)throw new Exception(string.Join("; ",failures));
 return rooms.Count+" prefab interaction fixtures passed: "+rays+" visible interaction targets at the player's 3-unit range; "+nestedTargets+" targets handled by an ancestor";
 }finally{if(surface!=null){surface.RemoveData();if(surface.navMeshData!=null)UnityEngine.Object.DestroyImmediate(surface.navMeshData);}UnityEngine.Object.DestroyImmediate(fixture);}
 }
}

