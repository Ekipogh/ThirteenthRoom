#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class MonsterAgitationChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, Private).GetValue(obj);
    static void Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Near(float a, float b, string message) => Check(Mathf.Abs(a-b)<0.001f,message+": "+a);

    sealed class Fixture : IDisposable
    {
        public GameObject Root;
        public NavMeshSurface Surface;
        public MonsterController Monster;
        public GameManager Manager;
        public AgitationProfile Profile;
        public Vector3 Origin = new Vector3(4000f,0f,4000f);
        public Fixture()
        {
            Check(Application.isPlaying,"Requires Play Mode");
            Root=new GameObject("Agitation fixture");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(Root.transform);
            floor.transform.position=Origin+Vector3.down*0.1f; floor.transform.localScale=new Vector3(100f,0.2f,100f);
            Surface=Root.AddComponent<NavMeshSurface>(); Surface.collectObjects=CollectObjects.Children;
            Surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders; Physics.SyncTransforms(); Surface.BuildNavMesh();
            var go=new GameObject("Manager"); go.transform.SetParent(Root.transform); Manager=go.AddComponent<GameManager>(); Manager.enabled=false;
            go=new GameObject("Monster"); go.transform.SetParent(Root.transform); go.transform.position=Origin;
            Monster=go.AddComponent<MonsterController>(); Monster.GameManager=Manager;
            Monster.GetComponent<NavMeshAgent>().Warp(Origin); Call(Monster,"Start");
            // Isolate base emitter/hearing contracts from progression multipliers.
            var response = Get<MonsterActivityProfile>(Monster, "_fallbackActivity");
            response.HearingRadiusMultiplier = response.NoiseStrengthMultiplier = response.InvestigationDurationMultiplier = Vector2.one;
            Profile=ScriptableObject.CreateInstance<AgitationProfile>(); Profile.HearingRadius=30f; Profile.InvestigationSeconds=12f; Profile.Strength=2f;
        }
        public AgitationEmitter Emitter(GameObject go)
        {
            var e=go.AddComponent<AgitationEmitter>(); e.BindOwner(Manager); Set(e,"profile",Profile); return e;
        }
        public GameObject Object(string name)
        {
            var go=new GameObject(name); go.transform.SetParent(Root.transform); go.transform.position=Origin; return go;
        }
        public void Dispose()
        {
            Surface.RemoveData(); UnityEngine.Object.DestroyImmediate(Root); UnityEngine.Object.DestroyImmediate(Profile);
        }
    }

    public static string HearingAndTimers()
    {
        using(var f=new Fixture())
        {
            var m=f.Monster; var p=f.Profile;
            float aggression=m.CurrentAggression;
            f.Manager.ReportAgitation(f.Origin+Vector3.up*30.01f,p);
            Check(m.CurrentState==MonsterState.Roaming,"Outside 3D radius ignored");
            f.Manager.ReportAgitation(f.Origin+Vector3.up*30f,p);
            Check(m.CurrentState==MonsterState.Investigating,"Radius boundary heard across floors");
            Call(m,"Update"); Near(m.GetComponent<NavMeshAgent>().speed,Mathf.Lerp(4f,6f,m.CurrentAgitation/100f),"Investigation uses agitation speed");
            Vector3 original=m.InvestigationOrigin; p.InvestigationSeconds=99f;
            Call(m,"AdvanceInvestigation",2f); Near(m.InvestigationSecondsRemaining,10f- Time.deltaTime,"Duration snapshotted");
            Check(m.InvestigationOrigin==original,"Unreachable source remains a position snapshot");
            p.Strength=1f; f.Manager.ReportAgitation(f.Origin,p);
            Check(m.InvestigationOrigin==original,"Weaker source ignored");
            p.Strength=2f; p.InvestigationSeconds=5f; f.Manager.ReportAgitation(f.Origin+Vector3.right*5f,p);
            Near(m.InvestigationSecondsRemaining,5f,"Equal refresh replaces rather than adds");
            p.Strength=3f; f.Manager.ReportAgitation(f.Origin,p);
            Check(m.InvestigationOrigin==f.Origin,"Stronger redirects"); Call(m,"Investigate");
            Call(m,"AdvanceInvestigation",5f); Check(m.CurrentState==MonsterState.Roaming,"Expiry after arrival");
            f.Manager.ReportAgitation(f.Origin+Vector3.up*30f,p); Call(m,"Investigate");
            Call(m,"AdvanceInvestigation",5f); Check(m.CurrentState==MonsterState.Roaming,"Unreachable travel expires");
            Near(m.CurrentAggression,aggression,"Noise leaves aggression unchanged");
            p.Strength=float.NaN; f.Manager.ReportAgitation(f.Origin,p);
            Check(m.CurrentState==MonsterState.Roaming,"Invalid profile ignored");
            p.Strength=2; f.Manager.ReportAgitation(new Vector3(float.NaN,0,0),p);
            Check(m.CurrentState==MonsterState.Roaming,"Invalid position ignored");
        }
        return "Hearing and timer checks passed";
    }

    public static string PriorityAndLifecycle()
    {
        using(var f=new Fixture())
        {
            var m=f.Monster; var manager=f.Manager;
            var playerAudio=f.Object("Player audio").AddComponent<PlayerAudioManager>(); m.PlayerAudioManager=playerAudio;
            var monsterAudio=f.Object("Monster audio").AddComponent<MonsterAudioManager>(); Set(m,"MonsterAudioManager",monsterAudio);
            manager.ReportAgitation(f.Origin,f.Profile);
            Check(!Get<bool>(playerAudio,"_isMonsterEncounterActive"),"Investigation does not activate player encounter breathing");
            Near(Get<float>(m,"_alertCooldownRemaining"),0f,"Investigation does not play an alert");
            Near(Get<float>(monsterAudio,"_chaseTarget"),0f,"Investigation has no chase vocal target");
            foreach(var state in new[]{GameState.Paused,GameState.GameOver})
            {
                manager.currentState=state; Call(m,"AdvanceInvestigation",100f); Call(m,"Update");
                manager.ReportAgitation(f.Origin+Vector3.right,f.Profile);
                Near(m.InvestigationSecondsRemaining,12f,"Inactive gameplay freezes timer and ignores reports");
                Check(m.InvestigationOrigin==f.Origin,"Inactive report cannot redirect");
            }
            manager.currentState=GameState.Playing; Call(m,"AdvanceInvestigation",1f); Near(m.InvestigationSecondsRemaining,11f,"Resume advances");
            manager.ResetAgitation(); Check(m.CurrentState==MonsterState.Roaming,"Restart reset clears investigation");
            manager.ReportAgitation(f.Origin,f.Profile); m.enabled=false;
            Check(m.CurrentState==MonsterState.Roaming && m.InvestigationSecondsRemaining==0,"Disable clears investigation");
            manager.ReportAgitation(f.Origin,f.Profile); Check(m.CurrentState==MonsterState.Roaming,"Disabled listener removed");
            m.enabled=true; manager.ReportAgitation(f.Origin,f.Profile); Check(m.CurrentState==MonsterState.Investigating,"Reenabled listener registered");
            var other=f.Object("Other manager").AddComponent<GameManager>(); other.enabled=false; m.GameManager=other;
            manager.ReportAgitation(f.Origin,f.Profile); Check(m.CurrentState==MonsterState.Roaming,"Old owner cannot deliver");
            m.GameManager=manager; Call(m,"ChangeState",MonsterState.Searching); manager.ReportAgitation(f.Origin,f.Profile);
            Check(m.CurrentState==MonsterState.Investigating,"Sound interrupts remembered search");
            var player=f.Object("Player"); player.transform.position=f.Origin+Vector3.forward*10f;
            m.RoomTracker=player.AddComponent<RoomTracker>(); Physics.SyncTransforms(); Call(m,"UpdatePursuit",0f);
            Check(m.CurrentState==MonsterState.Chasing && m.InvestigationSecondsRemaining==0,"Sight overrides investigation");
            Check(Get<bool>(playerAudio,"_isMonsterEncounterActive"),"Visual acquisition activates encounter audio");
            Near(Get<float>(m,"_alertCooldownRemaining"),15f,"Visual acquisition uses alert cooldown");
            manager.ReportAgitation(f.Origin,f.Profile); Check(m.CurrentState==MonsterState.Chasing,"Chase ignores sound");
            Call(m,"ChangeState",MonsterState.Attacking); manager.ReportAgitation(f.Origin,f.Profile);
            Check(m.CurrentState==MonsterState.Attacking,"Attack ignores sound");
            Call(m,"ChangeState",MonsterState.Roaming); m.RoomTracker=null;
            manager.SetMaximumHunt("test",true); manager.ReportAgitation(f.Origin,f.Profile);
            Check(m.CurrentState==MonsterState.Roaming,"Maximum hunt rejects noise before pursuit tick");
            manager.SetMaximumHunt("test",false); manager.ReportAgitation(f.Origin,f.Profile);
            m.RoomTracker=player.GetComponent<RoomTracker>(); manager.SetMaximumHunt("test",true); Call(m,"UpdatePursuit",0f);
            Check(m.CurrentState==MonsterState.Chasing,"Maximum hunt interrupts investigation");
        }
        return "Priority and lifecycle checks passed";
    }

    public static string ActionIntegration()
    {
        using(var f=new Fixture())
        {
            var m=f.Monster; var source=f.Object("Source"); var e=f.Emitter(source);
            e.TickContinuous(true,0f); Check(m.CurrentState==MonsterState.Investigating,"Immediate continuous pulse");
            Call(m,"AdvanceInvestigation",2f); e.TickContinuous(true,0.5f); Near(m.InvestigationSecondsRemaining,10f,"No early pulse");
            e.TickContinuous(true,0.5f); Near(m.InvestigationSecondsRemaining,12f,"One second refresh");
            e.TickContinuous(false,0f); Call(m,"AdvanceInvestigation",2f); Near(m.InvestigationSecondsRemaining,10f,"Stopping retains heard event");
            f.Manager.ResetAgitation();
            e.TickContinuous(true,0f); Call(m,"AdvanceInvestigation",2f);
            f.Manager.currentState=GameState.Paused; e.TickContinuous(true,10f);
            Near(m.InvestigationSecondsRemaining,10f,"Paused emitter cannot refresh");
            f.Manager.currentState=GameState.Playing; e.TickContinuous(true,0.5f);
            Near(m.InvestigationSecondsRemaining,10f,"Paused emitter freezes cadence");
            e.TickContinuous(true,0.5f); Near(m.InvestigationSecondsRemaining,12f,"Emitter resumes cadence");
            f.Manager.ResetAgitation();
            var deskObject=f.Object("Desk"); var desk=deskObject.AddComponent<LabDeskInteractable>(); var de=f.Emitter(deskObject); Set(desk,"brewingAgitation",de);
            var player=f.Object("Brewer").AddComponent<PlayerInteractor>(); player.enabled=false;
            var berry=ScriptableObject.CreateInstance<ItemDefinition>(); var potion=ScriptableObject.CreateInstance<ItemDefinition>();
            try
            {
                Set(desk,"berryDef",berry); Set(desk,"potionDef",potion);
                desk.BeginHoldInteract(player); Check(m.CurrentState==MonsterState.Roaming,"No ingredients means no noise");
                player.Inventory.AddItem(berry); desk.BeginHoldInteract(player);
                Check(m.CurrentState==MonsterState.Investigating,"Successful brew starts noise");
                Call(m,"AdvanceInvestigation",2f); desk.HoldInteract(player,1f); Near(m.InvestigationSecondsRemaining,12f,"Brewing repeats");
                desk.EndHoldInteract(player); Call(m,"AdvanceInvestigation",1f); desk.HoldInteract(player,1f);
                Near(m.InvestigationSecondsRemaining,11f,"Cancelled brew stops pulses");
                desk.BeginHoldInteract(player); player.Inventory.RemoveItem(berry); desk.HoldInteract(player,1f);
                Check(!Get<bool>(de,"_continuous"),"Lost ingredient stops emitter");
                player.Inventory.AddItem(berry); desk.BeginHoldInteract(player); desk.HoldInteract(player,5f);
                Check(player.Inventory.HasItem(potion) && !player.Inventory.HasItem(berry),"Brew preserves reward behaviour");
                Check(!Get<bool>(de,"_continuous"),"Completion stops emitter");
                desk.EndHoldInteract(player); player.Inventory.AddItem(berry); desk.BeginHoldInteract(player); desk.enabled=false;
                Check(!Get<bool>(de,"_continuous"),"Disabled desk stops emitter");
            }
            finally { UnityEngine.Object.DestroyImmediate(berry); UnityEngine.Object.DestroyImmediate(potion); }

            f.Manager.ResetAgitation();
            var runner=f.Object("Runner"); runner.transform.position=f.Origin+Vector3.right*8f;
            var controller=runner.AddComponent<CharacterController>(); controller.height=2; controller.center=Vector3.up;
            var movement=runner.AddComponent<PlayerController>(); movement.enabled=false;
            Set(movement,"_characterController",controller); Set(movement,"sprintAgitation",f.Emitter(runner));
            Set(movement,"_isSprinting",true); Set(movement,"_moveInput",Vector2.zero);
            controller.Move(Vector3.down*0.1f); Call(movement,"ProcessMovement");
            Check(m.CurrentState==MonsterState.Roaming,"Stationary sprint silent");
            Set(movement,"_moveInput",Vector2.up); Call(movement,"ProcessMovement");
            Check(m.CurrentState==MonsterState.Investigating,"Actual grounded sprint emits");
            f.Manager.ResetAgitation(); Set(movement,"_isSprinting",false); Call(movement,"ProcessMovement");
            Check(m.CurrentState==MonsterState.Roaming,"Unconfigured walking emitter is silent");
            Set(movement,"_isSprinting",true); Set(movement,"_stamina",0f); Call(movement,"ProcessMovement");
            Check(m.CurrentState==MonsterState.Roaming,"Exhausted sprint silent");
            Set(movement,"_stamina",100f); Set(movement,"_isSprinting",true);
            controller.enabled=false; runner.transform.position+=Vector3.up*5f; controller.enabled=true; Physics.SyncTransforms();
            Call(movement,"ProcessMovement"); Check(m.CurrentState==MonsterState.Roaming,"Airborne sprint silent");
            // Facing directly into a wall: collision flags must suppress attempted sprint noise.
            controller.enabled=false; runner.transform.position=f.Origin+Vector3.right*8f; controller.enabled=true;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(f.Root.transform);
            wall.transform.position=runner.transform.position+Vector3.forward*0.65f+Vector3.up; wall.transform.localScale=new Vector3(5,4,0.3f);
            Physics.SyncTransforms(); controller.Move(Vector3.forward); controller.Move(Vector3.down*0.1f);
            Call(movement,"ProcessMovement"); Check(m.CurrentState==MonsterState.Roaming,"Blocked sprint silent");
        }
        return "Action integration checks passed";
    }

    public static string WalkingIntegration()
    {
        using(var f=new Fixture())
        {
            var m=f.Monster;
            var runner=f.Object("Walking player"); runner.transform.position=f.Origin+Vector3.back*10f;
            var controller=runner.AddComponent<CharacterController>(); controller.height=2; controller.center=Vector3.up;
            var movement=runner.AddComponent<PlayerController>(); movement.enabled=false;
            var sprint=f.Emitter(runner); var walk=f.Emitter(runner);
            var walkProfile=ScriptableObject.CreateInstance<AgitationProfile>();
            walkProfile.Strength=0.25f; walkProfile.HearingRadius=20f; walkProfile.InvestigationSeconds=2f;
            try
            {
                Set(walk,"profile",walkProfile);
                Set(movement,"_characterController",controller); Set(movement,"sprintAgitation",sprint); Set(movement,"walkAgitation",walk);
                m.RoomTracker=runner.AddComponent<RoomTracker>();
                controller.Move(Vector3.down*0.1f); Physics.SyncTransforms();
                Check(!m.CanDetectPlayer(),"Player behind Monster is outside visual detection");
                Set(movement,"_moveInput",Vector2.up); Call(movement,"ProcessMovement");
                Check(m.CurrentState==MonsterState.Investigating,"Walking behind Monster draws investigation without sight");
                Near(Get<float>(m,"_agitationStrength"),0.25f,"Walking uses its own profile");
                Check(!Get<bool>(sprint,"_continuous") && Get<bool>(walk,"_continuous"),"Only walking emitter active");
                Check(m.InvestigationOrigin==runner.transform.position,"Walking reports actual position");
                Call(m,"AdvanceInvestigation",1f); Set(walk,"_untilNext",Time.deltaTime); Call(movement,"ProcessMovement");
                Near(m.InvestigationSecondsRemaining,2f,"Walking repeats on its emitter cadence");
                Set(movement,"_isSprinting",true); Call(movement,"ProcessMovement");
                Near(Get<float>(m,"_agitationStrength"),f.Profile.Strength,"Sprint takes over with stronger profile");
                Check(Get<bool>(sprint,"_continuous") && !Get<bool>(walk,"_continuous"),"Walking stops during sprint");
                f.Manager.ResetAgitation(); Set(movement,"_isSprinting",false); Call(movement,"ProcessMovement");
                Check(Get<bool>(walk,"_continuous") && !Get<bool>(sprint,"_continuous"),"Walking resumes after sprint");
                f.Manager.ResetAgitation(); Set(movement,"_isSprinting",true); Set(movement,"_stamina",0f); Call(movement,"ProcessMovement");
                Near(Get<float>(m,"_agitationStrength"),0.25f,"Exhausted player moving at walking speed emits walking noise");
                f.Manager.ResetAgitation(); Set(movement,"_moveInput",Vector2.zero); Call(movement,"ProcessMovement");
                Check(m.CurrentState==MonsterState.Roaming && !Get<bool>(walk,"_continuous"),"Standing stops walking noise");
                Set(movement,"_moveInput",Vector2.up); controller.enabled=false; runner.transform.position+=Vector3.up*5f; controller.enabled=true;
                Physics.SyncTransforms(); Call(movement,"ProcessMovement");
                Check(m.CurrentState==MonsterState.Roaming,"Airborne walking input stays silent");
                controller.enabled=false; runner.transform.position=f.Origin+Vector3.back*10f; controller.enabled=true;
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(f.Root.transform);
                wall.transform.position=runner.transform.position+Vector3.forward*0.65f+Vector3.up; wall.transform.localScale=new Vector3(5f,4f,0.3f);
                Physics.SyncTransforms(); controller.Move(Vector3.forward); controller.Move(Vector3.down*0.1f); Call(movement,"ProcessMovement");
                Check(m.CurrentState==MonsterState.Roaming,"Blocked walking input stays silent");
                walk.TickContinuous(true,0f); Call(movement,"OnDisable");
                Check(!Get<bool>(walk,"_continuous") && !Get<bool>(sprint,"_continuous"),"Disabling movement stops both emitters");
            }
            finally { UnityEngine.Object.DestroyImmediate(walkProfile); }
        }
        return "Walking integration checks passed";
    }

    public static string AssetConfiguration()
    {
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        Check(new SerializedObject(player.GetComponent<PlayerController>()).FindProperty("sprintAgitation").objectReferenceValue!=null,"Player emitter assigned");
        var playerFields=new SerializedObject(player.GetComponent<PlayerController>());
        var walk=(AgitationEmitter)playerFields.FindProperty("walkAgitation").objectReferenceValue;
        var sprintEmitter=(AgitationEmitter)playerFields.FindProperty("sprintAgitation").objectReferenceValue;
        Check(walk!=null && walk!=sprintEmitter,"Separate walking emitter assigned");
        Check(walk.Profile==AssetDatabase.LoadAssetAtPath<AgitationProfile>("Assets/SOInstances/Agitation/Walking.asset"),"User walking profile assigned");
        var sprint=sprintEmitter.Profile;
        Near(sprint.Strength,1,"Sprint strength"); Near(sprint.HearingRadius,75,"Sprint radius"); Near(sprint.InvestigationSeconds,5,"Sprint duration");
        var lab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Building/CR_Laboratory.prefab");
        foreach(var desk in lab.GetComponentsInChildren<LabDeskInteractable>(true))
        {
            var e=(AgitationEmitter)new SerializedObject(desk).FindProperty("brewingAgitation").objectReferenceValue;
            Check(desk.GetComponents<AgitationEmitter>().Length==1,"One inherited emitter per desk");
            Check(e!=null,"Desk emitter assigned"); Near(e.Profile.Strength,2,"Brew strength"); Near(e.Profile.HearingRadius,112.5f,"Brew radius"); Near(e.Profile.InvestigationSeconds,12,"Brew duration");
        }
        return "Asset configuration checks passed";
    }
}
#endif
