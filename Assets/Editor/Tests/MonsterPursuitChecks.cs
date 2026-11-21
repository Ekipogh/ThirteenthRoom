#if UNITY_EDITOR
using System;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public static class MonsterPursuitChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < 0.02f, message + ": " + actual);

    public static string RunAggressionChecks()
    {
        var profile = new MonsterAggressionProfile();
        var state = new MonsterAggression();
        Near(state.GetLevel(profile), 20f, "Starting aggression");
        state.Advance(900f); Near(state.GetLevel(profile), 80f, "15 minute aggression");
        state.Advance(299f); Check(state.GetLevel(profile) < 100f, "No premature maximum");
        state.SetModifier("pressure", 1000f); Near(state.GetLevel(profile), 99f, "Ordinary events cap at 99");
        state.Advance(1f); Near(state.GetLevel(profile), 100f, "20 minute maximum");
        state.SetModifier("calm", -1000f); Near(state.GetLevel(profile), 100f, "Endgame resists calming");
        profile.EndgameSeconds = 5000f;
        Check(state.IsMaximumHunt(profile), "Endgame remains latched if tuning changes");
        state.Reset(); profile = new MonsterAggressionProfile();
        Near(state.ElapsedSeconds, 0f, "Reset clock"); Near(state.GetLevel(profile), 20f, "Reset event state");
        object first = new object(), second = new object();
        state.SetRitualFailed(first, true); state.SetRitualFailed(first, true);
        Near(state.GetLevel(profile), 25f, "Duplicate ritual failure is idempotent");
        state.SetRitualFailed(second, true); Near(state.GetLevel(profile), 30f, "Multiple failed rituals");
        state.SetRitualFailed(first, false); Near(state.GetLevel(profile), 15f, "Restoration removes pressure and calms");
        state.Advance(20f); state.SetRitualFailed(second, false);
        Near(state.GetLevel(profile), 20f + 20f / 15f - 10f, "Restorations refresh rather than stack");
        state.Advance(20f); Check(state.GetLevel(profile) < 20f, "Refreshed calm still active");
        state.SetRitualFailed(second, false); // Duplicate completion must not extend calm.
        state.Advance(10f); Near(state.GetLevel(profile), 20f + 50f / 15f, "Calm expires at refreshed deadline");
        state.SetRitualFailed(first, true); state.UnregisterRitual(first);
        Near(state.GetLevel(profile), 20f + 50f / 15f, "Destroyed ritual removes pressure without calming");
        state.Reset(); state.SetModifier("event", 7f, 5f); state.SetModifier("event", 3f, 5f);
        Near(state.GetLevel(profile), 23f, "Named modifiers replace");
        state.Advance(5f); Near(state.GetLevel(profile), 20f + 5f / 15f, "Timed modifier expiry");
        state.SetModifier("event", -5f); state.RemoveModifier("event");
        state.DifficultyOffset = 10f; Near(state.GetLevel(profile), 30f + 5f / 15f, "Difficulty input");
        state.SetMaximumHunt("first", true); state.SetMaximumHunt("second", true);
        Near(state.GetLevel(profile), 100f, "Explicit maximum before first sight");
        state.SetMaximumHunt("first", false); Check(state.IsMaximumHunt(profile), "Independent hunt owners");
        state.SetMaximumHunt("second", false); Check(!state.IsMaximumHunt(profile), "Temporary hunt ends");
        return "Aggression model checks passed";
    }

    public static string RunPursuitChecks()
    {
        Check(Application.isPlaying, "Pursuit checks require Play Mode");
        var root = new GameObject("Monster pursuit fixture");
        NavMeshSurface surface = null;
        try
        {
            Vector3 origin = new Vector3(2000f, 0f, 2000f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform); floor.transform.position = origin + Vector3.down * 0.1f;
            floor.transform.localScale = new Vector3(140f, 0.2f, 140f);
            surface = root.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms(); surface.BuildNavMesh();
            var monsterObject = new GameObject("Monster"); monsterObject.transform.SetParent(root.transform); monsterObject.transform.position = origin;
            var monster = monsterObject.AddComponent<MonsterController>(); monster.enabled = false;
            var agent = monster.GetComponent<NavMeshAgent>(); agent.Warp(origin);
            var player = new GameObject("Player"); player.transform.SetParent(root.transform); player.transform.position = origin + Vector3.forward * 10f;
            var body = player.AddComponent<CharacterController>(); body.height = 5f; body.radius = 1f; body.center = Vector3.up * 2.5f;
            monster.RoomTracker = player.AddComponent<RoomTracker>();
            var managerObject = new GameObject("Manager"); managerObject.transform.SetParent(root.transform);
            var manager = managerObject.AddComponent<GameManager>(); manager.enabled = false; monster.GameManager = manager;
            Physics.SyncTransforms(); Call(monster, "UpdatePursuit", 0f);
            Check(monster.CurrentState == MonsterState.Chasing, "Initial sight starts pursuit");
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); cover.transform.SetParent(root.transform);
            cover.transform.position = origin + new Vector3(0f, 3.65f, 5f);
            cover.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f); Physics.SyncTransforms();
            Check(monster.CanSeePlayer(), "Lamp blocking centre ray must not hide whole body");
            for (int i = 0; i < 20; i++) Call(monster, "UpdatePursuit", 1f);
            Check(monster.CurrentState == MonsterState.Chasing, "Lamp cannot expire pursuit");
            cover.transform.localScale = new Vector3(5f, 0.3f, 0.3f); Physics.SyncTransforms();
            Check(monster.CanSeePlayer(), "Horizontal stair rail must not hide head");
            Call(monster, "UpdatePursuit", 20f); Check(monster.CurrentState == MonsterState.Chasing, "Rail cannot expire pursuit");
            cover.transform.position = origin + new Vector3(0f, 3f, 5f); cover.transform.localScale = new Vector3(8f, 7f, 0.3f); Physics.SyncTransforms();
            Check(!monster.CanSeePlayer(), "Full cover hides all body samples");
            player.transform.position += Vector3.forward; Physics.SyncTransforms();
            Call(monster, "UpdatePursuit", 2f); Check(monster.CurrentState == MonsterState.Chasing, "Short concealment preserves chase");
            Check(Get<Vector3>(monster, "_lastSeen") == player.transform.position, "Grace tracks current position");
            Call(monster, "Update"); Near(agent.speed, 3f, "Occlusion grace retains chase speed");
            cover.SetActive(false); Physics.SyncTransforms(); Call(monster, "UpdatePursuit", 0f);
            Near(Get<float>(monster, "_concealedFor"), 0f, "Reacquisition resets concealment");
            cover.SetActive(true); Physics.SyncTransforms(); Call(monster, "UpdatePursuit", 3f);
            Check(monster.CurrentState == MonsterState.Chasing, "Reacquisition grants fresh grace");
            Call(monster, "UpdatePursuit", 2f); Check(monster.CurrentState == MonsterState.Searching, "Sustained full concealment starts search");
            Vector3 remembered = Get<Vector3>(monster, "_lastSeen"); player.transform.position += Vector3.forward; Physics.SyncTransforms();
            Call(monster, "UpdatePursuit", 10f); Check(Get<Vector3>(monster, "_lastSeen") == remembered, "Search does not track hidden player");
            float travelDeadline = Get<float>(monster, "_searchTravelUntil");
            Call(monster, "UpdatePursuit", 10f); Near(Get<float>(monster, "_searchTravelUntil"), travelDeadline, "Searching must not restart timers");
            Set(monster, "_searchTravelUntil", Time.time - 1f); Call(monster, "Search");
            Check(monster.CurrentState == MonsterState.Roaming, "Unreachable search expires");
            Set(monster, "_lastSeen", monster.transform.position);
            Call(monster, "ChangeState", MonsterState.Searching); Call(monster, "Search");
            Check(Get<bool>(monster, "_searchReached"), "Search reaches remembered location");
            Near(Get<float>(monster, "_searchUntil"), Time.time + Mathf.Lerp(8f, 20f, monster.CurrentAggression / 99f), "Search duration begins on arrival");
            Set(monster, "_searchUntil", Time.time - 1f); Call(monster, "Search");
            Check(monster.CurrentState == MonsterState.Roaming, "Arrived search expires normally");
            cover.SetActive(false); player.transform.position = origin + Vector3.forward * 10f; Physics.SyncTransforms(); Call(monster, "UpdatePursuit", 0f);
            player.transform.position = origin + Vector3.forward * 60f; Physics.SyncTransforms();
            Call(monster, "UpdatePursuit", 1f); Check(monster.CurrentState == MonsterState.Chasing, "Distant escape needs sustained absence");
            Call(monster, "UpdatePursuit", 1f); Check(monster.CurrentState == MonsterState.Searching, "Distant escape uses two seconds");
            Call(monster, "ChangeState", MonsterState.Roaming); manager.SetMaximumHunt("test", true);
            cover.SetActive(true); Physics.SyncTransforms(); Call(monster, "UpdatePursuit", 0f);
            Check(monster.CurrentState == MonsterState.Chasing, "Maximum hunt acquires unseen distant player from roaming");
            Call(monster, "UpdatePursuit", 100f); Check(monster.CurrentState == MonsterState.Chasing, "Maximum ignores concealment and escape distance");
            manager.SetMaximumHunt("test", false); Call(monster, "UpdatePursuit", 0f);
            Near(Get<float>(monster, "_concealedFor"), 0f, "Leaving maximum grants fresh concealment grace");
            player.transform.position = origin + Vector3.forward * 2f;
            cover.transform.position = origin + new Vector3(0f, 3f, 1f); Physics.SyncTransforms();
            manager.SetMaximumHunt("test", true); Check(!monster.CanAttackPlayer(), "Maximum cannot attack through wall");
            cover.SetActive(false); player.transform.position += Vector3.up * 13.75f; Physics.SyncTransforms();
            Check(!monster.CanAttackPlayer(), "Maximum cannot attack through floor");
            player.transform.position = origin + Vector3.forward * 2f; Physics.SyncTransforms();
            Check(monster.CanAttackPlayer(), "Unobstructed close player is attackable");
            Call(monster, "ChangeState", MonsterState.Attacking); cover.SetActive(true); Physics.SyncTransforms();
            Set(monster, "_attackStarted", Time.time - 0.6f); Call(monster, "Update");
            Check(manager.currentState == GameState.Playing, "Attack impact rechecks obstruction");
            manager.Aggression.Advance(1200f); Check(monster.IsMaximumHunt, "Run deadline enables maximum");
            float elapsed = manager.Aggression.ElapsedSeconds;
            manager.currentState = GameState.Paused; Call(manager, "Update"); Near(manager.Aggression.ElapsedSeconds, elapsed, "Pause freezes aggression");
            float concealed = Get<float>(monster, "_concealedFor"); Call(monster, "Update"); Near(Get<float>(monster, "_concealedFor"), concealed, "Pause freezes concealment");
            manager.currentState = GameState.GameOver; Call(manager, "Update"); Near(manager.Aggression.ElapsedSeconds, elapsed, "Game over freezes aggression");
            var ritualObject = new GameObject("Ritual"); ritualObject.transform.SetParent(root.transform);
            var ritual = ritualObject.AddComponent<RitualInteractable>(); ritual.enabled = false;
            Check(!ritual.IsComplete, "Ritual starts inactive");
            manager.Aggression.Reset(); manager.currentState = GameState.Playing;
            ritual.BindAggressionOwner(manager); ritual.BindAggressionOwner(manager);
            int transitions = 0; ritual.StateChanged += (_, __) => transitions++;
            Call(ritual, "DeactivateRitual"); Call(ritual, "DeactivateRitual");
            Near(manager.Aggression.GetLevel(new MonsterAggressionProfile()), 25f, "Ritual binding applies failure exactly once");
            Check(transitions == 0, "Already inactive ritual publishes no transition");
            Call(ritual, "CompleteRitual"); Call(ritual, "CompleteRitual");
            Check(transitions == 1 && ritual.IsComplete, "Completion publishes one transition");
            Near(manager.Aggression.GetLevel(new MonsterAggressionProfile()), 10f, "Ritual restoration calms without score dependency");
            Call(ritual, "DeactivateRitual"); Call(ritual, "DeactivateRitual");
            Check(transitions == 2 && !ritual.IsComplete, "Deactivation publishes one transition");
            Near(manager.Aggression.GetLevel(new MonsterAggressionProfile()), 15f, "Deactivation reapplies failure exactly once while calm remains");
            Call(ritual, "CompleteRitual"); Call(ritual, "CompleteRitual");
            Check(transitions == 3 && ritual.IsComplete, "Restoration publishes one transition");
            Near(manager.Aggression.GetLevel(new MonsterAggressionProfile()), 10f, "Restoration removes failure without stacking calm");
            manager.Aggression.Reset(); Call(ritual, "DeactivateRitual"); UnityEngine.Object.DestroyImmediate(ritualObject);
            Near(manager.Aggression.GetLevel(new MonsterAggressionProfile()), 20f, "Destroyed rituals unregister");
            return "Pursuit, cover, maximum hunt, pause and ritual integration checks passed";
        }
        finally
        {
            if (surface != null) surface.RemoveData();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
#endif
