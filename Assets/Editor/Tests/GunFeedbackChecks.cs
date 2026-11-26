#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class GunFeedbackChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object o, string field, object value) => o.GetType().GetField(field, Private).SetValue(o, value);
    static T Get<T>(object o, string field) => (T)o.GetType().GetField(field, Private).GetValue(o);
    static void Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Private).Invoke(o, args);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static ItemDefinition Gun => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SOInstances/ItemDefinitions/GunDefiniton.asset");

    sealed class Fixture : IDisposable
    {
        public readonly GameObject Root = new("Gun feedback fixture");
        public readonly Vector3 Origin = new(32000, 0, 32000);
        public readonly GameManager Manager;
        public readonly PlayerController Player;
        public readonly PlayerItemController Items;
        public readonly PlayerAudioManager Audio;
        public readonly AudioSource Fire, Reload;
        public MonsterController Monster;
        public MonsterAnimationController Animation;
        public Animator Animator;
        NavMeshSurface _surface;
        public Fixture()
        {
            var manager = new GameObject("Manager"); manager.transform.SetParent(Root.transform);
            Manager = manager.AddComponent<GameManager>(); Manager.enabled = false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            var player = UnityEngine.Object.Instantiate(prefab, Origin, Quaternion.identity, Root.transform);
            Player = player.GetComponent<PlayerController>(); Player.enabled = false; Player.PlayerCamera.enabled = false;
            player.GetComponent<PlayerInteractor>().enabled = false;
            player.GetComponent<InventoryUI>().enabled = false;
            var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>(); if (input != null) input.enabled = false;
            Items = player.GetComponent<PlayerItemController>(); Audio = player.GetComponent<PlayerAudioManager>();
            Fire = Get<AudioSource>(Audio, "ItemUseAudioSource"); Reload = Get<AudioSource>(Audio, "WeaponReloadAudioSource");
            foreach (var source in player.GetComponentsInChildren<AudioSource>(true)) { source.Stop(); source.enabled = source == Fire || source == Reload; }
            Cursor.lockState = CursorLockMode.None;
        }
        public void Ready()
        {
            Set(Items, "_manager", Manager);
            Items.Inventory.AddItem(Gun); Items.Inventory.AddItems(Gun.Ammunition, 18);
            Items.Slots.Assign(0, new InventoryEntry(Gun));
        }
        public void AddMonster(bool active)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(Root.transform);
            floor.transform.position = Origin + Vector3.down * .1f; floor.transform.localScale = new Vector3(180, .2f, 180);
            _surface = Root.AddComponent<NavMeshSurface>(); _surface.collectObjects = CollectObjects.Children;
            _surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; Physics.SyncTransforms(); _surface.BuildNavMesh();
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/Monster.prefab"),
                Origin + Vector3.forward * 40, Quaternion.identity, Root.transform);
            Monster = go.GetComponent<MonsterController>(); Monster.enabled = false; Monster.GameManager = Manager;
            Monster.Definition = AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/SOInstances/MonsterDefinitions/StalkerMonsterDefinition.asset");
            go.GetComponent<NavMeshAgent>().Warp(go.transform.position); Call(Monster, "Start");
            Animation = go.GetComponent<MonsterAnimationController>(); Animator = go.GetComponentInChildren<Animator>();
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Monster.enabled = active;
            Physics.SyncTransforms();
        }
        public void Dispose()
        {
            if (_surface != null) _surface.RemoveData();
            UnityEngine.Object.DestroyImmediate(Root);
        }
    }

    public static IEnumerator WeaponActionsAndCleanup()
    {
        using (var f = new Fixture())
        {
            yield return null; f.Ready();
            Check(Gun.UseSound == AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/gunshot.wav"), "Gun uses the requested firing clip");
            Check(Gun.ReloadSound == AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thirdparty/Audio/652631__percyfrench__revolver-reload.mp3"), "Gun uses the requested reload clip");
            Check(f.Fire != null && f.Reload != null && f.Fire != f.Reload && !f.Fire.playOnAwake && !f.Reload.loop, "Prefab has independent non-looping weapon sources");
            Check(!f.Items.TryUseSelected() && !f.Fire.isPlaying, "Empty gun stays silent");
            Check(f.Items.TryReload() && f.Items.Inventory.LoadedRounds(Gun) == 6 && f.Reload.isPlaying, "Reload transfers bullets immediately and plays its sound");
            Check(f.Items.TryUseSelected() && f.Fire.isPlaying && f.Reload.isPlaying, "Firing does not interrupt the reload audio");
            Check(!f.Items.TryUseSelected() && f.Items.Inventory.LoadedRounds(Gun) == 5, "Cooldown rejects another action");
            yield return new WaitForSecondsRealtime(.12f);
            Check(f.Reload.time > .03f, "Reload audio advances");
            Check(f.Items.TryReload(), "A new successful reload is allowed while audio plays");
            Check(f.Reload.time < .04f, "Another reload restarts rather than stacks the sound");
            f.Audio.StopItemSounds();
            Check(!f.Items.TryReload() && !f.Reload.isPlaying, "Full magazine produces no reload sound");
            Set(f.Items, "_nextShot", 0f); Check(f.Items.TryUseSelected(), "Fire after explicit cooldown expiry");
            f.Audio.StopItemSounds(); f.Items.Inventory.RemoveItems(Gun.Ammunition, f.Items.Inventory.Quantity(Gun.Ammunition));
            Check(!f.Items.TryReload() && !f.Reload.isPlaying, "No reserve ammunition produces no reload sound");
            f.Items.SetInventoryOpen(true); Set(f.Items, "_nextShot", 0f);
            Check(!f.Items.TryUseSelected() && !f.Items.TryReload() && !f.Fire.isPlaying && !f.Reload.isPlaying, "Inventory blocks both sounds");
            f.Items.SetInventoryOpen(false);
            f.Items.Inventory.AddItem(Gun.Ammunition); f.Items.TryReload(); Set(f.Items, "_nextShot", 0f); f.Items.TryUseSelected();
            f.Manager.currentState = GameState.GameOver; Call(f.Items, "Update");
            Check(!f.Fire.isPlaying && !f.Reload.isPlaying && !f.Items.TryUseSelected(), "Game-over stops weapon audio and blocks actions");
            f.Manager.currentState = GameState.Playing; f.Audio.PlayItemUseSound(Gun.UseSound); f.Audio.PlayWeaponReload(Gun.ReloadSound);
            f.Items.enabled = false; Check(!f.Fire.isPlaying && !f.Reload.isPlaying, "Disabling item controller stops sounds");
            f.Audio.PlayItemUseSound(Gun.UseSound); f.Audio.PlayWeaponReload(Gun.ReloadSound); f.Audio.enabled = false;
            Check(!f.Fire.isPlaying && !f.Reload.isPlaying, "Disabling audio manager stops sounds");
        }
    }

    public static IEnumerator OverlappingShotTails()
    {
        using (var f = new Fixture())
        {
            yield return null; f.Ready(); f.Items.TryReload(); f.Reload.Stop();
            Check(f.Items.TryUseSelected(), "First shot fires");
            yield return new WaitForSecondsRealtime(.6f);
            Check(f.Fire.isPlaying, "First shot has an audible tail longer than the shot cooldown");
            Set(f.Items, "_nextShot", 0f); Check(f.Items.TryUseSelected(), "Second shot fires while the first tail remains");
            yield return new WaitForSecondsRealtime(.6f);
            Check(f.Fire.isPlaying && f.Items.Inventory.LoadedRounds(Gun) == 4, "Second shot survives the first shot's natural end, with exactly two bullets consumed");
        }
    }

    public static IEnumerator AgitationIntegration()
    {
        using (var f = new Fixture())
        {
            f.AddMonster(true); yield return null; f.Ready();
            var profile = Gun.UseAgitation;
            Check(profile == AssetDatabase.LoadAssetAtPath<AgitationProfile>("Assets/SOInstances/Agitation/Gunshot.asset") &&
                profile.HearingRadius == 250 && profile.Strength == 10 && profile.InvestigationSeconds == 20, "Gun uses authored gunshot agitation values");
            f.Manager.SetMonsterDifficultyOffset(-99); f.Items.TryReload();
            Check(f.Monster.CurrentAgitation == 0, "Reload emits no agitation");
            f.Player.PlayerCamera.transform.rotation = Quaternion.Euler(0, 90, 0);
            Check(f.Items.TryUseSelected() && f.Monster.CurrentAgitation == 0, "Quiet baseline cannot hear beyond its scaled range");
            f.Manager.SetMonsterDifficultyOffset(0); f.Monster.ResetActivity(); Set(f.Items, "_nextShot", 0f);
            Check(f.Items.TryUseSelected() && f.Monster.CurrentState == MonsterState.Investigating, "Miss attracts the monster through the gunshot profile");
            float heardFor = f.Monster.InvestigationSecondsRemaining;
            Check(heardFor > 0 && f.Monster.InvestigationOrigin == f.Player.transform.position, "Noise stores the shot origin and scaled duration");
            f.Monster.ResetActivity(); float before = f.Monster.CurrentAgitation;
            Check(!f.Items.TryUseSelected() && f.Monster.CurrentAgitation == before && f.Monster.CurrentState == MonsterState.Roaming, "Cooldown rejection cannot report agitation");
            f.Manager.SetMonsterDifficultyOffset(99); Set(f.Items, "_nextShot", 0f); f.Items.TryUseSelected();
            // Maximum hunt intentionally keeps pursuit priority; strength/duration snapshots remain aggression-scaled.
            Check(f.Monster.CurrentAgitation > before, "Higher aggression amplifies gunshot noise");
            Check(profile.HearingRadius == 250 && profile.Strength == 10 && profile.InvestigationSeconds == 20, "Hearing never mutates the shared profile");
        }
    }

    public static IEnumerator PotionUseFeedback()
    {
        using (var f = new Fixture())
        {
            yield return null; f.Ready();
            var potion = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SOInstances/ItemDefinitions/PotionDefenition.asset");
            var drink = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thirdparty/Audio/41529__jamius__potiondrinklong.wav");
            Check(potion.UseSound == drink && drink != null && drink.length > 0, "Potion uses the supplied drink sound");
            Check(potion.UseAgitation == null, "Potion is configured for sound only by default");
            f.Items.Inventory.AddItems(potion, 2); f.Items.Slots.Assign(1, new InventoryEntry(potion)); f.Items.Slots.Select(1);
            Set(f.Player, "_stamina", 100f);
            Check(!f.Items.TryUseSelected() && !f.Fire.isPlaying && f.Items.Inventory.Quantity(potion) == 2, "Full stamina neither consumes a potion nor plays its sound");
            Set(f.Player, "_stamina", 0f); f.Items.SetInventoryOpen(true);
            Check(!f.Items.TryUseSelected() && !f.Fire.isPlaying, "Inventory blocks potion use audio");
            f.Items.SetInventoryOpen(false);
            Check(f.Items.TryUseSelected() && f.Fire.isPlaying && f.Player.Stamina == 50 && f.Items.Inventory.Quantity(potion) == 1, "Successful drink restores stamina, consumes one, and plays generic use audio");
            f.Audio.StopItemSounds(); Set(f.Player, "_stamina", 0f);
            Check(f.Items.TryUseSelected() && f.Fire.isPlaying && f.Items.Inventory.Quantity(potion) == 0 && !f.Items.Slots[1].HasValue, "Last potion still plays its sound after its shortcut is cleared");
            f.Manager.currentState = GameState.GameOver; Call(f.Items, "Update");
            Check(!f.Fire.isPlaying, "Game-over stops the potion's long use sound");
        }
    }

    public static IEnumerator OptionalItemUseFeedback()
    {
        var authored = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SOInstances/ItemDefinitions/PotionDefenition.asset");
        var potion = UnityEngine.Object.Instantiate(authored);
        var profile = ScriptableObject.CreateInstance<AgitationProfile>();
        var unsupported = ScriptableObject.CreateInstance<ItemDefinition>();
        try
        {
            using (var f = new Fixture())
            {
                f.AddMonster(true); yield return null; f.Ready();
                profile.HearingRadius = 250; profile.Strength = 1; profile.InvestigationSeconds = 5;
                potion.UseSound = null; potion.UseAgitation = profile;
                f.Items.Inventory.AddItems(potion, 5); f.Items.Slots.Assign(1, new InventoryEntry(potion)); f.Items.Slots.Select(1);
                Set(f.Player, "_stamina", 0f); f.Monster.ResetActivity();
                Check(f.Items.TryUseSelected() && !f.Fire.isPlaying && f.Monster.CurrentState == MonsterState.Investigating,
                    "Any successful potion use can emit configured agitation without an audible sound");
                Check(f.Monster.InvestigationOrigin == f.Player.transform.position, "Generic use noise reports the player position");
                potion.UseSound = authored.UseSound; potion.UseAgitation = null;
                f.Monster.ResetActivity(); Set(f.Player, "_stamina", 0f);
                Check(f.Items.TryUseSelected() && f.Fire.isPlaying && f.Monster.CurrentState == MonsterState.Roaming,
                    "An audible use sound does not implicitly generate AI noise");
                f.Audio.StopItemSounds(); potion.UseSound = null;
                Set(f.Player, "_stamina", 0f);
                Check(f.Items.TryUseSelected() && !f.Fire.isPlaying && f.Monster.CurrentState == MonsterState.Roaming,
                    "Both feedback fields are optional and do not prevent the item effect");
                potion.UseSound = authored.UseSound; potion.UseAgitation = profile;
                Set(f.Player, "_stamina", 100f);
                Check(!f.Items.TryUseSelected() && !f.Fire.isPlaying && f.Monster.CurrentState == MonsterState.Roaming,
                    "Rejected item use produces neither configured feedback effect");
                Set(f.Player, "_stamina", 0f); f.Manager.currentState = GameState.Paused;
                Check(!f.Items.TryUseSelected() && !f.Fire.isPlaying && f.Monster.CurrentState == MonsterState.Roaming,
                    "Paused gameplay blocks generic feedback");
                f.Manager.currentState = GameState.Playing; f.Audio.enabled = false;
                Check(f.Items.TryUseSelected() && !f.Fire.isPlaying && f.Monster.CurrentState == MonsterState.Investigating,
                    "AI noise remains independent of audio-source availability");
                f.Monster.ResetActivity(); f.Audio.enabled = true;
                unsupported.UseSound = authored.UseSound; unsupported.UseAgitation = profile;
                f.Items.Inventory.AddItem(unsupported); f.Items.Slots.Assign(2, new InventoryEntry(unsupported)); f.Items.Slots.Select(2);
                Check(!f.Items.TryUseSelected() && !f.Fire.isPlaying && f.Monster.CurrentState == MonsterState.Roaming && f.Items.Inventory.Quantity(unsupported) == 1,
                    "An item with no use effect does not fake a successful use or consume stock");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(potion); UnityEngine.Object.DestroyImmediate(profile); UnityEngine.Object.DestroyImmediate(unsupported);
        }
    }

    static void AdvanceAnimation(Fixture f, float seconds)
    {
        for (float time = 0; time < seconds; time += .025f)
        {
            if (f.Animation.isActiveAndEnabled) Call(f.Animation, "Update");
            f.Animator.Update(.025f);
        }
    }
    public static IEnumerator HitReactionLifecycle()
    {
        using (var f = new Fixture())
        {
            f.AddMonster(false); yield return null;
            f.Animator.Update(0f);
            int hits = 0; f.Monster.StunApplied += () => hits++;
            Call(f.Monster, "ChangeState", MonsterState.Attacking);
            f.Monster.ApplyStun(3f); AdvanceAnimation(f, .15f);
            Check(f.Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Getting Shot"), "Shot enters the authored hit state");
            Check(f.Animator.GetCurrentAnimatorClipInfo(0).Any(x => x.clip.name == "Zombie@Damage01"), "Damage clip drives the real monster rig");
            Check(f.Animator.speed == 1 && !f.Animator.GetBool("Attack") && Get<bool>(f.Monster, "_impactApplied") && !f.Monster.CanAttackPlayer(), "Reaction cancels attack without freezing Animator");
            Check(!f.Animator.applyRootMotion && f.Monster.GetComponent<NavMeshAgent>().isStopped, "Reaction cannot move the navigation root");
            var spine = f.Animator.GetBoneTransform(HumanBodyBones.Spine); Quaternion pose = spine.localRotation;
            AdvanceAnimation(f, .10f);
            Check(Quaternion.Angle(pose, spine.localRotation) > .01f, "Reaction advances the monster's bones");
            float previous = f.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            f.Monster.ApplyStun(3f); AdvanceAnimation(f, .10f);
            Check(hits == 2 && f.Monster.StunSecondsRemaining == 3 && f.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < previous, "Repeated shot restarts reaction and refreshes stun");
            float pausedAt = f.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            f.Manager.currentState = GameState.Paused; AdvanceAnimation(f, .25f); Call(f.Monster, "AdvanceStun", 1f);
            Check(Mathf.Abs(f.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime - pausedAt) < .001f && f.Monster.StunSecondsRemaining == 3, "Pause freezes both reaction and stun timer");
            f.Manager.currentState = GameState.Playing; AdvanceAnimation(f, .7f);
            Check(f.Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Stunned Idle") && f.Monster.IsStunned, "Damage settles into idle while stun remains active");
            Call(f.Monster, "AdvanceStun", 3f); AdvanceAnimation(f, .2f);
            Check(!f.Monster.IsStunned && !f.Animator.GetBool("Stunned") && !f.Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Stunned Idle"), "Recovery returns to normal animation behavior");
            f.Monster.ApplyStun(3); f.Animation.enabled = false; f.Animator.Update(.2f); f.Animation.enabled = true; AdvanceAnimation(f, .1f);
            Check(f.Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Stunned Idle"), "Re-enable does not replay a stale hit trigger");
            f.Monster.ApplyStun(3); f.Manager.currentState = GameState.GameOver; Call(f.Animation, "Update");
            Check(f.Animator.speed == 0 && !f.Animator.GetBool("Stunned"), "Game-over cancels pending hit requests and stops playback");
        }
    }
}
#endif
