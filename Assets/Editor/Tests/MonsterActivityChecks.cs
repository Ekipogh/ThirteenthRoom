#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class MonsterActivityChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, Private).GetValue(obj);
    static void Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < 0.002f, message + ": " + actual);

    sealed class Fixture : IDisposable
    {
        public readonly GameObject Root = new GameObject("Monster activity fixture");
        public readonly List<Room> Rooms = new List<Room>();
        public readonly List<RoomConnection> Connections = new List<RoomConnection>();
        public readonly Vector3 Origin = new Vector3(7000f, 0f, 7000f);
        public NavMeshSurface Surface;
        public MonsterController Monster;
        public GameManager Manager;
        public MonsterDefinition Definition;
        public AgitationProfile Noise;
        public Room Room(float x, float y = 0f)
        {
            var go = new GameObject("Room " + Rooms.Count); go.transform.SetParent(Root.transform);
            go.transform.position = Origin + new Vector3(x, y, 0f);
            var room = go.AddComponent<Room>(); Rooms.Add(room); return room;
        }
        public void Link(Room a, Room b, RoomDirection direction = RoomDirection.East)
        {
            Vector3 point = (a.transform.position + b.transform.position) * 0.5f;
            Connections.Add(new RoomConnection(a, b, direction, point));
            Connections.Add(new RoomConnection(b, a, direction == RoomDirection.Up ? RoomDirection.Down : RoomDirection.West, point));
        }
        public MansionModel Model() => new MansionModel(Rooms, Connections, Array.Empty<SpawnItemDefinition>());
        public void Navigation()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(Root.transform);
            floor.transform.position = Origin + Vector3.down * 0.1f; floor.transform.localScale = new Vector3(200f, 0.2f, 100f);
            Surface = Root.AddComponent<NavMeshSurface>(); Surface.collectObjects = CollectObjects.Children;
            Surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; Physics.SyncTransforms(); Surface.BuildNavMesh();
            var go = new GameObject("Manager"); go.transform.SetParent(Root.transform);
            Manager = go.AddComponent<GameManager>(); Manager.enabled = false;
            go = new GameObject("Monster"); go.transform.SetParent(Root.transform); go.transform.position = Origin;
            Definition = ScriptableObject.CreateInstance<MonsterDefinition>();
            Definition.Activity.HearingRadiusMultiplier = Definition.Activity.NoiseStrengthMultiplier = Definition.Activity.InvestigationDurationMultiplier = Vector2.one;
            Monster = go.AddComponent<MonsterController>(); Monster.Definition = Definition; Monster.GameManager = Manager;
            Monster.Mansion = Model(); Monster.StartingRoom = Rooms[0];
            Monster.GetComponent<NavMeshAgent>().Warp(Origin); Call(Monster, "Start");
            Noise = ScriptableObject.CreateInstance<AgitationProfile>(); Noise.HearingRadius = 100f;
        }
        public void Dispose()
        {
            if (Surface != null) Surface.RemoveData();
            UnityEngine.Object.DestroyImmediate(Root);
            if (Definition != null) UnityEngine.Object.DestroyImmediate(Definition);
            if (Noise != null) UnityEngine.Object.DestroyImmediate(Noise);
        }
    }

    public static string AgitationAndLifecycle()
    {
        using (var f = new Fixture())
        {
            f.Room(0); f.Navigation(); var m = f.Monster;
            Near(m.CurrentAgitation, 20f, "Aggression is baseline");
            f.Manager.ReportAgitation(f.Origin + Vector3.right * 101, f.Noise);
            Near(m.CurrentAgitation, 20f, "Inaudible noise adds nothing");
            f.Manager.ReportAgitation(f.Origin, f.Noise);
            Near(m.CurrentAgitation, 30f, "Strength one adds ten");
            f.Manager.ReportAgitation(f.Origin, f.Noise); Near(m.CurrentAgitation, 30f, "Repeated noise does not stack");
            Call(m, "AdvanceActivity", 2f); Near(m.CurrentAgitation, 26f, "Bonus decays two per second");
            f.Manager.ReportAgitation(f.Origin, f.Noise); Near(m.CurrentAgitation, 30f, "Repeat refreshes minimum");
            f.Noise.Strength = 10f; f.Manager.ReportAgitation(f.Origin, f.Noise); Near(m.CurrentAgitation, 50f, "Noise capped at thirty");
            f.Manager.SetAggressionModifier("test", 75f); Near(m.CurrentAgitation, 100f, "Combined agitation capped");
            Check(!m.IsMaximumHunt, "Agitation cannot activate maximum hunt");
            f.Manager.RemoveAggressionModifier("test");
            var ritual = new object(); f.Manager.Aggression.SetRitualFailed(ritual, true);
            Near(m.CurrentAgitation, 55f, "Failure raises baseline");
            f.Manager.Aggression.SetRitualFailed(ritual, false); Near(m.CurrentAgitation, 40f, "Restoration immediately calms baseline");
            foreach (var state in new[] { GameState.Paused, GameState.GameOver })
            {
                f.Manager.currentState = state; Call(m, "AdvanceActivity", 100f);
                f.Manager.ReportAgitation(f.Origin, f.Noise); Near(m.CurrentAgitation, 40f, "Inactive gameplay freezes noise bonus");
            }
            f.Manager.currentState = GameState.Playing; Call(m, "AdvanceActivity", 100f);
            Near(m.CurrentAgitation, m.CurrentAggression, "Decay stops at baseline");
            f.Manager.ReportAgitation(f.Origin, f.Noise); Call(m, "ChangeState", MonsterState.Chasing);
            Set(m, "_noiseBonus", 0f); Vector3 lastSeen = new Vector3(1, 2, 3); Set(m, "_lastSeen", lastSeen);
            f.Manager.ReportAgitation(f.Origin + Vector3.right, f.Noise);
            Near(m.CurrentAgitation, 40f, "Chase still hears agitation");
            Check(m.CurrentState == MonsterState.Chasing && Get<Vector3>(m, "_lastSeen") == lastSeen, "Noise cannot redirect chase");
            f.Manager.ResetAgitation(); Near(m.CurrentAgitation, m.CurrentAggression, "Restart clears noise");
            f.Manager.ReportAgitation(f.Origin, f.Noise); m.enabled = false;
            Near(m.CurrentAgitation, m.CurrentAggression, "Disable clears noise");
        }
        return "Agitation and lifecycle passed";
    }

    public static string AggressionAmplifiesNoise()
    {
        using (var f = new Fixture())
        {
            f.Room(0); f.Navigation(); var m = f.Monster;
            f.Definition.Activity = new MonsterActivityProfile();
            f.Noise.HearingRadius = 75f; f.Noise.InvestigationSeconds = 5f; f.Noise.Strength = 1f;
            // Default run progression: 20 at spawn, 60 at ten minutes, 80 at fifteen.
            float[] radii = { 30f, 75f, 97.5f };
            float[] durations = { 2.5f, 6.25f, 8.125f };
            float[] strengths = { .5f, 1.25f, 1.625f };
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) f.Manager.Aggression.Advance(i == 1 ? 600f : 300f);
                f.Manager.ResetAgitation();
                f.Manager.ReportAgitation(f.Origin + Vector3.right * (radii[i] + .1f), f.Noise);
                Check(m.CurrentState == MonsterState.Roaming, "Outside scaled hearing radius ignored");
                f.Manager.ReportAgitation(f.Origin + Vector3.right * radii[i], f.Noise);
                Check(m.CurrentState == MonsterState.Investigating, "Scaled hearing boundary included");
                Near(m.InvestigationSecondsRemaining, durations[i], "Investigation lasts longer as time passes");
                Near(Get<float>(m, "_agitationStrength"), strengths[i], "Heard strength increases with aggression");
                Near(m.CurrentAgitation, Mathf.Min(100, m.CurrentAggression + 10 * strengths[i]), "Scaled strength drives transient bonus");
                float before = m.CurrentAgitation;
                f.Manager.ReportAgitation(f.Origin + Vector3.right * radii[i], f.Noise);
                Near(m.CurrentAgitation, before, "Scaling uses aggression, not its own bonus; repeated pulses do not amplify themselves");
            }
            Near(f.Noise.HearingRadius,75,"Base hearing profile remains immutable");
            Near(f.Noise.Strength,1,"Base strength remains immutable");
            Near(f.Noise.InvestigationSeconds,5,"Base duration remains immutable");
            float heardDuration = m.InvestigationSecondsRemaining;
            var ritual = new object(); f.Manager.Aggression.SetRitualFailed(ritual,true);
            f.Manager.Aggression.SetRitualFailed(ritual,false);
            Near(m.InvestigationSecondsRemaining,heardDuration,"Already heard event retains snapshot during calming");
            f.Manager.ResetAgitation();
            f.Manager.ReportAgitation(f.Origin + Vector3.right * 95f,f.Noise);
            Check(m.CurrentState==MonsterState.Roaming,"Restoration reduces future hearing radius");
            f.Manager.ReportAgitation(f.Origin,f.Noise);
            Near(m.InvestigationSecondsRemaining,7.1875f,"Restoration also reduces future event persistence");
            f.Manager.ResetAgitation(); f.Manager.Aggression.Reset();
            f.Manager.ReportAgitation(f.Origin+Vector3.right*31f,f.Noise);
            Check(m.CurrentState==MonsterState.Roaming,"Restart restores forgiving hearing");
            f.Manager.ReportAgitation(f.Origin,f.Noise);
            Near(m.InvestigationSecondsRemaining,2.5f,"Restart restores short early investigation");
            Call(m,"AdvanceActivity",1f); Near(m.CurrentAgitation,23f,"Small early bonus decays quickly");
        }
        return "Aggression amplification passed";
    }

    public static string MovementAndPauses()
    {
        using (var f = new Fixture())
        {
            f.Room(0); f.Navigation(); var m = f.Monster; var agent = m.GetComponent<NavMeshAgent>();
            Near(Get<float>(m, "_roamAfter"), Time.time, "Spawn patrol starts immediately");
            foreach (float agitation in new[] { 0f, 50f, 100f })
            {
                var p = f.Definition.Activity;
                Near(p.Speed(MonsterState.Roaming, agitation), Mathf.Lerp(3, 5, agitation/100), "Patrol range");
                Near(p.Speed(MonsterState.Searching, agitation), Mathf.Lerp(4, 6, agitation/100), "Search range");
                Near(p.Speed(MonsterState.Investigating, agitation), Mathf.Lerp(4, 6, agitation/100), "Investigation range");
                float chase = p.Speed(MonsterState.Chasing, agitation);
                Near(chase, Mathf.Lerp(6, 8.5f, agitation/100), "Chase range");
                Check(chase > 5 && chase < 10, "Walk loses distance, sprint gains distance");
                Near(p.Pause(agitation), Mathf.Lerp(2, .25f, agitation/100), "Pause range");
            }
            Call(m, "SchedulePatrolPause"); float before = Get<float>(m, "_roamAfter");
            f.Manager.SetAggressionModifier("test", 60f); Call(m, "Update");
            Check(Get<float>(m, "_roamAfter") < before, "Rising agitation shortens existing pause");
            Near(agent.speed, 4.6f, "Patrol speed updates live");
            f.Manager.RemoveAggressionModifier("test"); Call(m, "Update"); Near(agent.speed, 3.4f, "Calming updates live");
            Call(m, "ChangeState", MonsterState.Chasing); Call(m, "Update"); Near(agent.speed, 6.5f, "Chase speed applied");
            f.Manager.SetMaximumHunt("test", true); Call(m, "Update"); Near(agent.speed, 8.5f, "Maximum speed applied");
            f.Manager.SetMaximumHunt("test", false);
        }
        return "Movement and pauses passed";
    }

    public static string PatrolGraphCoverage()
    {
        var random = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(719);
            using (var f = new Fixture())
            {
                Room a = f.Room(0), b = f.Room(37.5f), c = f.Room(75), d = f.Room(-37.5f), isolated = f.Room(150);
                f.Link(a,b); f.Link(b,c); f.Link(c,a); f.Link(a,d);
                var model = f.Model(); var memory = new MonsterPatrolMemory(); memory.Visit(a);
                var covered = new HashSet<Room> { a }; Room current = a;
                for (int i=0; i<3; i++)
                {
                    Room next = memory.Choose(model,current,0,3);
                    Check(next != null && next != isolated && !covered.Contains(next), "Unvisited reachable rooms win");
                    covered.Add(next); memory.Visit(next); memory.Complete(next); current = next;
                }
                Check(covered.Count==4, "Branch and cycle covered without local loop");
                Check(memory.Choose(model,current,0,3)==a, "Oldest visited room wins after coverage");
                memory.Fail(a,0,15); Check(memory.Choose(model,current,14.9f,3)!=a,"Failed destination skipped");
                Check(memory.Choose(model,current,15,3)==a,"Failure expires at fifteen Playing seconds");
                memory.Reset(); memory.Visit(a); memory.Visit(b); memory.Visit(c); memory.Visit(d);
                memory.Fail(a,0,15); memory.Fail(b,0,15); memory.Fail(c,0,15);
                Check(memory.Choose(model,d,0,3)==null,"No destination signals local fallback");
                memory.Reset(); Check(memory.Choose(model,isolated,0,3)==null,"Disconnected room stays local");
            }
        }
        finally { UnityEngine.Random.state = random; }
        return "Patrol graph coverage passed";
    }

    public static string FloorCoverageAndVisits()
    {
        using (var f = new Fixture())
        {
            Room a=f.Room(0), b=f.Room(37.5f), c=f.Room(0,13.75f), d=f.Room(0,27.5f);
            f.Link(a,b); f.Link(a,c,RoomDirection.Up); f.Link(c,d,RoomDirection.Up);
            var memory=new MonsterPatrolMemory(); var model=f.Model();
            memory.Visit(a); memory.Visit(c); memory.Visit(d); memory.Visit(a);
            for(int i=0;i<3;i++) memory.Complete(a);
            Check(memory.Choose(model,a,0,3)==c,"Third completion favors least recently visited other floor over unvisited same-floor room");
            memory.Visit(c); memory.Complete(c);
            Check(memory.Choose(model,c,0,3)==b,"Floor completion count resets after changing floors");
            f.Navigation(); var m=f.Monster;
            Call(m,"ChangeState",MonsterState.Chasing);
            Set(m,"_currentRoom",c); m.transform.position=c.transform.position; Call(m,"Update");
            var visits=Get<MonsterPatrolMemory>(m,"_patrol");
            Check(Get<Room>(visits,"_lastRoom")==c,"Chase updates room and floor memory");
            m.ResetActivity(); Check(Get<Room>(visits,"_lastRoom")==null,"Reset clears patrol history");
        }
        return "Floor coverage and visits passed";
    }

    public static string DestinationRetentionAndRecovery()
    {
        using(var f=new Fixture())
        {
            Room a=f.Room(0), b=f.Room(37.5f), c=f.Room(75); f.Link(a,b); f.Link(b,c); f.Navigation();
            var m=f.Monster; var memory=Get<MonsterPatrolMemory>(m,"_patrol"); memory.Visit(b); memory.Visit(a);
            Call(m,"Roam"); Check(Get<Room>(m,"_roamRoom")==c,"Nonadjacent room selected");
            Vector3 destination=Get<Vector3>(m,"_roamTarget");
            m.GetComponent<NavMeshAgent>().Warp(b.transform.position); Set(m,"_currentRoom",b); memory.Visit(b); Call(m,"Roam");
            Check(Get<Room>(m,"_roamRoom")==c && Get<Vector3>(m,"_roamTarget")==destination,"Intermediate room retains destination");
            Call(m,"FailPatrolDestination"); Call(m,"Roam");
            Check(Get<Room>(m,"_roamRoom")!=c,"Failed destination replaced immediately");
            Check(Get<bool>(m,"_hasRoamTarget"),"Replacement patrol target active");
            Call(m,"ChangeState",MonsterState.Chasing);
            Check(!Get<bool>(m,"_hasRoamTarget"),"Chase clears stale patrol target before progress recovery");
            f.Manager.currentState=GameState.Paused; float clock=Get<float>(m,"_activitySeconds"); Call(m,"AdvanceActivity",30f);
            Near(Get<float>(m,"_activitySeconds"),clock,"Failure cooldown uses Playing time");
        }
        return "Destination retention and recovery passed";
    }

    public static string AssetConfiguration()
    {
        var definition=AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/SOInstances/MonsterDefinitions/StalkerMonsterDefinition.asset");
        Check(definition.Activity!=null,"Activity serialized on actual definition");
        Near(definition.Activity.HearingRadiusMultiplier.x,.1f,"Authored hearing lower endpoint");
        Near(definition.Activity.HearingRadiusMultiplier.y,1.6f,"Authored hearing upper endpoint");
        Near(definition.Activity.NoiseStrengthMultiplier.y,2f,"Authored strength upper endpoint");
        Near(definition.Activity.InvestigationDurationMultiplier.y,2f,"Authored duration upper endpoint");
        Near(definition.Activity.PatrolSpeed.x,3,"Authored patrol speed"); Near(definition.Activity.ChaseSpeed.y,8.5f,"Authored chase speed");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/Monster.prefab");
        Near(prefab.GetComponent<NavMeshAgent>().speed,3,"Prefab initial agent speed updated");
        var fields=new SerializedObject(prefab.GetComponent<MonsterController>());
        Check(fields.FindProperty("travelSpeed")==null && fields.FindProperty("chaseSpeed")==null && fields.FindProperty("moveInterval")==null,"Legacy speed overrides removed");
        return "Activity asset configuration passed";
    }
}
#endif
