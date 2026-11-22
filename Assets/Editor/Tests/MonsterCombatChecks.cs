#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class MonsterCombatChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target,string field,object value) => target.GetType().GetField(field,Private).SetValue(target,value);
    static T Get<T>(object target,string field) => (T)target.GetType().GetField(field,Private).GetValue(target);
    static void Call(object target,string method,params object[] args) => target.GetType().GetMethod(method,Private).Invoke(target,args);
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    static void Near(float actual,float expected,string message) => Check(Mathf.Abs(actual-expected)<.01f,message+": "+actual);

    sealed class Fixture : IDisposable
    {
        public readonly GameObject Root=new GameObject("Monster combat fixture");
        public readonly Vector3 Origin=new Vector3(12000,0,12000);
        public readonly NavMeshSurface Surface;
        public readonly MonsterController Monster;
        public readonly MonsterAnimationController Animation;
        public readonly Animator Animator;
        public readonly GameManager Manager;
        public readonly GameObject Player;
        public Fixture()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(Root.transform);
            floor.transform.position=Origin+Vector3.down*.1f; floor.transform.localScale=new Vector3(60,.2f,60);
            Surface=Root.AddComponent<NavMeshSurface>(); Surface.collectObjects=CollectObjects.Children;
            Surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders; Physics.SyncTransforms(); Surface.BuildNavMesh();
            var manager=new GameObject("Manager"); manager.transform.SetParent(Root.transform);
            Manager=manager.AddComponent<GameManager>(); Manager.enabled=false;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/Monster.prefab");
            var go=UnityEngine.Object.Instantiate(prefab,Origin,Quaternion.identity,Root.transform);
            Monster=go.GetComponent<MonsterController>(); Monster.enabled=false; Monster.GameManager=Manager;
            Monster.Definition=AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/SOInstances/MonsterDefinitions/StalkerMonsterDefinition.asset");
            Monster.GetComponent<NavMeshAgent>().Warp(Origin); Call(Monster,"Start");
            Animation=go.GetComponent<MonsterAnimationController>(); Animation.enabled=false; Animator=go.GetComponentInChildren<Animator>();
            Player=new GameObject("Player"); Player.transform.SetParent(Root.transform);
            var body=Player.AddComponent<CharacterController>(); body.height=5; body.radius=1; body.center=Vector3.up*2.5f;
            Monster.RoomTracker=Player.AddComponent<RoomTracker>(); Player.GetComponent<RoomTracker>().enabled=false;
            Move(new Vector3(0,0,4));
        }
        public void Move(Vector3 offset) { Player.transform.position=Origin+offset; Physics.SyncTransforms(); }
        public void Begin()
        {
            Manager.currentState=GameState.Playing;
            Call(Monster,"ChangeState",MonsterState.Chasing); Move(new Vector3(0,0,4));
            Check(Monster.CanAttackPlayer(),"Attack begins at authored reach boundary");
            Call(Monster,"ChangeState",MonsterState.Attacking);
        }
        public void Impact() { Set(Monster,"_attackStarted",Time.time-.36f); Call(Monster,"Update"); }
        public void Dispose() { Surface.RemoveData(); UnityEngine.Object.DestroyImmediate(Root); }
    }

    public static string AnimationPace()
    {
        using(var f=new Fixture())
        {
            Call(f.Monster,"ChangeState",MonsterState.Chasing);
            foreach(float speed in new[]{.2f,3.4f,6.5f,8.5f,30f})
            {
                Set(f.Animation,"_hasLastPosition",true);
                Set(f.Animation,"_lastPosition",f.Monster.transform.position-Vector3.forward*speed*Time.deltaTime);
                Set(f.Animation,"_smoothedSpeed",speed); Set(f.Animation,"_speedSmoothVelocity",0f);
                Call(f.Animation,"Update");
                Near(f.Animator.speed,Mathf.Clamp(speed*.15f,.5f,1.1f),"Locomotion rate remains bounded at speed "+speed);
                Near(f.Animator.GetFloat("MoveSpeed"),speed,"Movement parameter remains actual world speed");
            }
            Set(f.Animation,"_lastPosition",f.Monster.transform.position); Set(f.Animation,"_smoothedSpeed",0f);
            Set(f.Animation,"_speedSmoothVelocity",0f); Call(f.Animation,"Update");
            Near(f.Animator.speed,1f,"Idle is unaffected by locomotion slowdown");
            Call(f.Monster,"ChangeState",MonsterState.Attacking);
            Set(f.Animation,"_lastPosition",f.Monster.transform.position-Vector3.forward*8.5f*Time.deltaTime);
            Set(f.Animation,"_smoothedSpeed",8.5f); Call(f.Animation,"Update");
            Near(f.Animator.speed,1f,"Attack playback remains normal despite smoothed movement");
            Check(f.Animator.GetBool("Attack"),"Attack parameter remains active");
        }
        return "Animation pace passed";
    }

    public static string AttackReachAndEscape()
    {
        using(var f=new Fixture())
        {
            Check(f.Monster.CanAttackPlayer(),"Attack reaches collider at distance four");
            f.Move(new Vector3(0,0,4.1f)); Check(!f.Monster.CanAttackPlayer(),"Follow-through cannot initiate remote attacks");
            Call(f.Monster,"ChangeState",MonsterState.Chasing); Call(f.Monster,"Update");
            Check(f.Monster.CurrentState==MonsterState.Chasing,"Player outside initiation range is still pursued");
            f.Begin(); f.Move(new Vector3(0,0,5.75f)); f.Impact();
            Check(f.Manager.currentState==GameState.GameOver,"Walking back during wind-up remains within swing reach");
            f.Begin(); f.Move(new Vector3(1.75f,0,4f)); f.Impact();
            Check(f.Manager.currentState==GameState.GameOver,"Small sidestep does not nullify attack");
            f.Begin(); f.Move(new Vector3(0,0,7.5f)); f.Impact();
            Check(f.Manager.currentState==GameState.Playing,"Sprinting far enough avoids impact");
            Check(Get<bool>(f.Monster,"_impactApplied"),"Missed impact is consumed once");
            f.Move(new Vector3(0,0,4f)); Call(f.Monster,"Update");
            Check(f.Manager.currentState==GameState.Playing,"Returning after a miss cannot trigger another hit");
            f.Begin(); f.Move(new Vector3(0,0,6.1f)); f.Impact();
            Check(f.Manager.currentState==GameState.Playing,"Outside follow-through boundary avoids hit");
        }
        return "Attack reach and escape passed";
    }

    public static string ObstructionAndPause()
    {
        using(var f=new Fixture())
        {
            f.Begin(); f.Move(new Vector3(0,0,5.75f));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(f.Root.transform);
            wall.transform.position=f.Origin+new Vector3(0,3,2); wall.transform.localScale=new Vector3(20,8,.5f); Physics.SyncTransforms();
            f.Impact(); Check(f.Manager.currentState==GameState.Playing,"Longer swing cannot hit through cover or closed door geometry");
            UnityEngine.Object.DestroyImmediate(wall);
            f.Begin(); f.Move(new Vector3(0,Room.RoomSize.y,4)); f.Impact();
            Check(f.Manager.currentState==GameState.Playing,"Longer swing cannot hit another floor");
            f.Begin(); f.Manager.currentState=GameState.Paused;
            Call(f.Monster,"Update"); Check(!Get<bool>(f.Monster,"_impactApplied"),"Pause freezes attack impact");
            Check(f.Monster.GetComponent<NavMeshAgent>().isStopped,"Pause stops attacking agent");
            f.Manager.currentState=GameState.Playing; f.Impact();
            Check(f.Manager.currentState==GameState.GameOver,"Attack resumes with normal damage behavior");
        }
        return "Obstruction and pause passed";
    }
}
#endif
