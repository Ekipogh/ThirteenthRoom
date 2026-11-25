#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class InventoryChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object o, string field, object value) => o.GetType().GetField(field, Private).SetValue(o, value);
    static T Get<T>(object o, string field) => (T)o.GetType().GetField(field, Private).GetValue(o);
    static void Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Private).Invoke(o, args);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    // The package exposes only parameterless Update publicly; an unfocused Editor may otherwise select its Editor buffer.
    static void UpdatePlayerInput() => typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic,
        null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
    static ItemDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SOInstances/ItemDefinitions/" + name + ".asset");

    public static string StacksAndShortcuts()
    {
        var inventory = new PlayerInventory(); var potion = Load("PotionDefenition"); var gun = Load("GunDefiniton");
        int changes = 0; inventory.Changed += () => changes++;
        inventory.AddItem(potion); inventory.AddItems(potion, 2); inventory.AddItem(null);
        Check(inventory.Quantity(potion) == 3 && inventory.Entries.Count == 1 && changes == 2, "Stacks combine and notify once per valid change");
        inventory.AddInstanceItem(potion, "safe-a"); inventory.AddInstanceItem(potion, "safe-b");
        Check(inventory.Entries.Count == 3 && inventory.HasInstanceItem(potion, "safe-a"), "Targeted keys stay separate from ordinary stack");
        using (var slots = new ActiveItemSlots(inventory))
        {
            var stack = new InventoryEntry(potion); var key = new InventoryEntry(potion, "safe-b");
            Check(slots.Assign(0, stack) && slots.Assign(1, key), "Assign stacks and targeted instances");
            Check(inventory.Quantity(potion) == 3, "Assigning does not move stock");
            slots.Assign(2, stack); Check(!slots[0].HasValue && slots[2].Value.Equals(stack), "Reassigning moves shortcut");
            slots.Assign(1, stack); Check(!slots[2].HasValue && slots[1].Value.Equals(stack), "Replacing shortcut does not remove displaced item");
            Check(inventory.HasInstanceItem(potion, "safe-b"), "Replacement preserves keys");
            inventory.RemoveItems(potion, 3); Check(!slots[1].HasValue && !inventory.HasItem(potion), "Exhausted stack clears shortcuts");
            slots.Assign(3, key); inventory.RemoveInstanceItem(potion, "safe-a");
            Check(slots[3].HasValue && inventory.HasInstanceItem(potion, "safe-b"), "Removing a different key preserves shortcut");
            inventory.RemoveInstanceItem(potion, "safe-b"); Check(!slots[3].HasValue, "Consumed key clears exact shortcut");
            slots.Cycle(-1); Check(slots.SelectedIndex == 8, "Selection wraps backwards");
            slots.Cycle(1); Check(slots.SelectedIndex == 0, "Selection wraps forwards");
            Check(!slots.Assign(9, stack) && !inventory.RemoveItem(potion), "Invalid actions do not alter state");
        }
        inventory.AddItem(gun); inventory.AddItems(gun.Ammunition, 4);
        Check(inventory.LoadedRounds(gun) == 0 && inventory.Reload(gun) == 4, "Gun starts empty and supports partial loading");
        inventory.AddItems(gun.Ammunition, 10);
        Check(inventory.Reload(gun) == 2 && inventory.LoadedRounds(gun) == 6 && inventory.Quantity(gun.Ammunition) == 8, "Reload respects capacity and preserves reserve");
        Check(inventory.Reload(gun) == 0, "Full magazine cannot consume reserve");
        var other = new PlayerInventory(); other.AddItem(gun);
        Check(other.LoadedRounds(gun) == 0, "Loaded rounds belong to owner, not shared definition");
        inventory.RemoveItem(gun); inventory.AddItem(gun); Check(inventory.LoadedRounds(gun) == 0, "Removed gun discards owned loaded state");
        return "Stacks and shortcuts passed";
    }

    sealed class Fixture : IDisposable
    {
        public readonly GameObject Root = new("Inventory fixture");
        public readonly PlayerController Player;
        public readonly PlayerInteractor Interactor;
        public readonly PlayerItemController Items;
        public readonly GameManager Manager;
        public readonly Camera Camera;
        public readonly Vector3 Origin = new(24000, 0, 24000);
        public Fixture()
        {
            var manager = new GameObject("Manager"); manager.transform.SetParent(Root.transform);
            Manager = manager.AddComponent<GameManager>(); Manager.enabled = false;
            var go = new GameObject("Player"); go.SetActive(false); go.transform.SetParent(Root.transform); go.transform.position = Origin;
            Player = go.AddComponent<PlayerController>(); Interactor = go.AddComponent<PlayerInteractor>(); Items = go.AddComponent<PlayerItemController>();
            var camera = new GameObject("Camera"); camera.transform.SetParent(go.transform, false); camera.transform.localPosition = Vector3.up * 2;
            Camera = camera.AddComponent<Camera>(); Camera.enabled = false; Player.PlayerCamera = Camera;
            go.SetActive(true); Player.enabled = false; Interactor.enabled = false;
            Call(Items, "Start"); Set(Items, "_manager", Manager); Set(Player, "_items", Items);
        }
        public MonsterController AddMonster()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(Root.transform);
            floor.transform.position = Origin + Vector3.down * .1f; floor.transform.localScale = new Vector3(60, .2f, 60);
            var surface = Root.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; Physics.SyncTransforms(); surface.BuildNavMesh();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/Monster.prefab");
            var go = UnityEngine.Object.Instantiate(prefab, Origin + Vector3.forward * 10, Quaternion.identity, Root.transform);
            var monster = go.GetComponent<MonsterController>(); monster.enabled = false; monster.GameManager = Manager;
            var animation = go.GetComponent<MonsterAnimationController>(); if (animation != null) animation.enabled = false;
            Check(go.GetComponentsInChildren<Collider>().Any(x => x.enabled && x.isTrigger), "Authored monster prefab has a non-solid shot hitbox");
            monster.GetComponent<NavMeshAgent>().Warp(go.transform.position); Call(monster, "Start");
            var tracker = Player.gameObject.AddComponent<RoomTracker>(); tracker.enabled = false; monster.RoomTracker = tracker;
            Physics.SyncTransforms(); return monster;
        }
        public void Dispose()
        {
            Items.SetInventoryOpen(false);
            var surface = Root.GetComponent<NavMeshSurface>(); if (surface != null) surface.RemoveData();
            UnityEngine.Object.DestroyImmediate(Root);
        }
    }

    public static string PotionAndInputBlocking()
    {
        using (var f = new Fixture())
        {
            var potion = Load("PotionDefenition"); f.Items.Inventory.AddItems(potion, 5); f.Items.Slots.Assign(0, new InventoryEntry(potion));
            foreach (float stamina in new[] { 0f, 25f, 85f })
            {
                Set(f.Player, "_stamina", stamina); int before = f.Items.Inventory.Quantity(potion);
                Check(f.Items.TryUseSelected() && Mathf.Abs(f.Player.Stamina - Mathf.Min(100, stamina + 50)) < .001f, "Potion restores capped stamina from " + stamina);
                Check(f.Items.Inventory.Quantity(potion) == before - 1, "Successful potion consumes exactly one");
            }
            Set(f.Player, "_stamina", 100f); int remaining = f.Items.Inventory.Quantity(potion);
            Check(!f.Items.TryUseSelected() && f.Items.Inventory.Quantity(potion) == remaining, "Full stamina preserves potion");
            Set(f.Player, "_stamina", 0f); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            Set(f.Player, "_moveInput", Vector2.one); Set(f.Player, "_isSprinting", true);
            f.Items.SetInventoryOpen(true);
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible && f.Player.GameplayInputBlocked, "Inventory unlocks cursor and blocks player");
            Check(Get<Vector2>(f.Player, "_moveInput") == Vector2.zero && !Get<bool>(f.Player, "_isSprinting"), "Opening cancels movement and sprint");
            Check(!f.Items.TryUseSelected() && f.Player.Stamina == 0 && !f.Items.TryReload(), "Open inventory prevents use and reload");
            Check(f.Manager.currentState == GameState.Playing && Time.timeScale == 1, "Inventory does not pause world");
            f.Items.SetInventoryOpen(false); Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "Closing restores cursor");
            f.Manager.currentState = GameState.Paused; Check(!f.Items.TryUseSelected(), "Paused gameplay blocks use");
            f.Manager.currentState = GameState.Playing;
            var desk = f.Root.AddComponent<LabDeskInteractable>();
            Set(desk, "berryDef", potion); Set(desk, "potionDef", potion);
            Call(f.Interactor, "BeginHeldInteraction", desk);
            Check(Get<PlayerInteractor>(desk, "_activePlayer") == f.Interactor, "Brewing begins through existing held interaction");
            f.Items.SetInventoryOpen(true);
            Check(Get<PlayerInteractor>(desk, "_activePlayer") == null, "Opening inventory cancels held interaction");
            f.Items.SetInventoryOpen(false);
            f.Manager.currentState = GameState.Playing; f.Items.SetInventoryOpen(true); f.Manager.currentState = GameState.GameOver; Call(f.Items, "Update");
            Check(!f.Items.IsInventoryOpen && Cursor.lockState == CursorLockMode.None && Cursor.visible && f.Player.GameplayInputBlocked, "Death closes inventory without relocking cursor or enabling player");
        }
        return "Potion and input blocking passed";
    }

    public static string GunAndStun()
    {
        using (var f = new Fixture())
        {
            var gun = Load("GunDefiniton"); f.Items.Inventory.AddItem(gun); f.Items.Inventory.AddItems(gun.Ammunition, 12);
            f.Items.Slots.Assign(0, new InventoryEntry(gun)); var monster = f.AddMonster(); monster.enabled = true;
            Check(!f.Items.TryUseSelected() && f.Items.Inventory.Quantity(gun.Ammunition) == 12, "Empty gun does not auto-load");
            Check(f.Items.TryReload() && f.Items.Inventory.LoadedRounds(gun) == 6, "R-style reload transfers six rounds");
            Check(f.Items.TryUseSelected() && monster.IsStunned && f.Items.Inventory.LoadedRounds(gun) == 5, "Hit consumes loaded round and stuns");
            Check(monster.CurrentAgitation > 0, "Shot reports noise through existing agitation system");
            Check(monster.GetComponent<NavMeshAgent>().isStopped && !monster.CanAttackPlayer(), "Stun stops navigation and attacks immediately");
            Check(!f.Items.TryUseSelected() && f.Items.Inventory.LoadedRounds(gun) == 5, "Cooldown blocks duplicate shot");
            Call(monster, "AdvanceStun", 2f); monster.ApplyStun(3f); Check(Mathf.Abs(monster.StunSecondsRemaining - 3) < .001f, "Repeated stun refreshes rather than stacks");
            Call(monster, "AdvanceStun", 3f); Check(!monster.IsStunned, "Stun expires");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(f.Root.transform);
            wall.transform.position = f.Origin + new Vector3(0, 2, 5); wall.transform.localScale = new Vector3(4, 6, .5f); Physics.SyncTransforms();
            Set(f.Items, "_nextShot", 0f); Check(f.Items.TryUseSelected() && !monster.IsStunned && f.Items.Inventory.LoadedRounds(gun) == 4, "Wall blocks hit but consumes round");
            UnityEngine.Object.DestroyImmediate(wall);
            f.Camera.transform.rotation = Quaternion.Euler(0, 90, 0); Set(f.Items, "_nextShot", 0f);
            Check(f.Items.TryUseSelected() && !monster.IsStunned && f.Items.Inventory.LoadedRounds(gun) == 3, "Miss consumes one round");
            Check(monster.CurrentState == MonsterState.Investigating, "Miss still attracts a listening monster");
            f.Items.Slots.Assign(1, new InventoryEntry(gun.Ammunition)); f.Items.Slots.Select(1);
            int reserve = f.Items.Inventory.Quantity(gun.Ammunition);
            Check(!f.Items.TryUseSelected() && f.Items.Inventory.Quantity(gun.Ammunition) == reserve, "Items without effects are not consumed");
            f.Items.Slots.Select(0);
            monster.transform.position = f.Origin + Vector3.forward * 4; monster.GetComponent<NavMeshAgent>().Warp(monster.transform.position); Physics.SyncTransforms();
            Call(monster, "ChangeState", MonsterState.Chasing); Set(monster, "_lastSeen", f.Player.transform.position);
            Call(monster, "ChangeState", MonsterState.Attacking); Set(monster, "_attackStarted", Time.time - 1);
            monster.ApplyStun(3); Call(monster, "Update"); Check(f.Manager.currentState == GameState.Playing, "Stun cancels committed attack damage");
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(f.Root.transform);
            wall.transform.position = f.Origin + new Vector3(0, 3, 2); wall.transform.localScale = new Vector3(20, 8, .5f); Physics.SyncTransforms();
            f.Player.transform.position = f.Origin + Vector3.right * 3; Physics.SyncTransforms();
            Vector3 remembered = Get<Vector3>(monster, "_lastSeen"); Call(monster, "AdvanceStun", 3f); Call(monster, "Update");
            Check(monster.CurrentState == MonsterState.Searching && Get<Vector3>(monster, "_lastSeen") == remembered, "Recovery searches memory without revealing concealed player");
            f.Manager.SetMaximumHunt("test", true); monster.ApplyStun(3); Call(monster, "AdvanceStun", 3f); Call(monster, "Update");
            Check(monster.CurrentState == MonsterState.Chasing, "Maximum hunt resumes after stun");
        }
        return "Gun and stun passed";
    }

    public static string UIAndPickup()
    {
        using (var f = new Fixture())
        {
            var ammoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Ammo.prefab");
            var pickup = UnityEngine.Object.Instantiate(ammoPrefab, f.Root.transform).GetComponent<ItemPickup>();
            var gun = Load("GunDefiniton"); pickup.Interact(f.Interactor);
            Check(f.Items.Inventory.Quantity(gun.Ammunition) == 6, "Ammo pickup enters existing inventory API as six bullets");
            f.Items.Inventory.AddItem(gun);
            var gameOverPanel = new GameObject("Hidden game-over panel"); gameOverPanel.transform.SetParent(f.Root.transform); gameOverPanel.SetActive(false);
            var eventObject = new GameObject("Existing EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            eventObject.transform.SetParent(gameOverPanel.transform);
            var ui = f.Player.gameObject.AddComponent<InventoryUI>(); ui.enabled = false; Call(ui, "Start");
            Check(eventObject.activeInHierarchy && EventSystem.current == eventObject.GetComponent<EventSystem>(), "Inventory reuses the inactive game-over EventSystem");
            gameOverPanel.SetActive(true);
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Count(x => x.isActiveAndEnabled) == 1, "Showing game-over panel cannot activate a second EventSystem");
            eventObject.transform.SetParent(f.Root.transform);
            f.Items.SetInventoryOpen(true); Call(ui, "LateUpdate"); Canvas.ForceUpdateCanvases();
            Check(ui.InventoryPanel.rect.width > 0 && ui.GridContent.childCount >= 2, "Inventory grid lays out actual owned items");
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = new Vector2(100, 100) };
            ui.BeginDrag(new InventoryEntry(gun), data); ui.Drop(5); Call(ui, "LateUpdate");
            Check(f.Items.Slots[5]?.Definition == gun && f.Items.Inventory.Quantity(gun) == 1, "Drag and drop assigns without moving inventory stock");
            var cell = ui.InventoryPanel.Find("ActiveSlot6").GetComponent<InventoryCellUI>(); data.button = PointerEventData.InputButton.Right; cell.OnPointerClick(data);
            Check(!f.Items.Slots[5].HasValue, "Right click clears slot");
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null) { f.Items.Inventory.AddItem(definition); count++; }
            }
            // Distinct targets exercise scrolling beyond the catalog size without adding content assets.
            for (int i = 0; i < 30; i++) f.Items.Inventory.AddInstanceItem(gun, "fixture-" + i);
            Call(ui, "LateUpdate"); Canvas.ForceUpdateCanvases();
            Check(ui.GridContent.rect.height > 420, "Inventory grows beyond viewport and remains scrollable");
        }
        return "UI and pickup passed";
    }

    public static string KeyboardAndWheel()
    {
        var oldKeyboard = Keyboard.current; var oldMouse = Mouse.current;
        var keyboard = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            using (var f = new Fixture())
            {
                void KeyPress(Key key)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                    UpdatePlayerInput();
                    Check(keyboard[key].wasPressedThisFrame, "Synthetic key press reaches player input: " + key + "; current=" + Keyboard.current?.deviceId + "; expected=" + keyboard.deviceId);
                    Call(f.Items, "Update");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    UpdatePlayerInput();
                }
                KeyPress(Key.I); Check(f.Items.IsInventoryOpen, "I opens inventory");
                KeyPress(Key.Escape); Check(!f.Items.IsInventoryOpen, "Escape closes inventory");
                KeyPress(Key.Tab); Check(f.Items.IsInventoryOpen, "Tab opens inventory");
                KeyPress(Key.Tab); Check(!f.Items.IsInventoryOpen, "Tab closes inventory");
                for (int i = 0; i < 9; i++) { KeyPress((Key)((int)Key.Digit1 + i)); Check(f.Items.Slots.SelectedIndex == i, "Number selects slot " + (i + 1)); }
                Cursor.lockState = CursorLockMode.Locked;
                InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, -120) }); UpdatePlayerInput(); Call(f.Items, "Update");
                Check(f.Items.Slots.SelectedIndex == 0, "Mouse wheel wraps selected slot");
                f.Items.SetInventoryOpen(true);
                InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, -120) }); UpdatePlayerInput(); Call(f.Items, "Update");
                Check(f.Items.Slots.SelectedIndex == 0, "Inventory scrolling does not change active slot");
            }
        }
        finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); oldKeyboard?.MakeCurrent(); oldMouse?.MakeCurrent(); }
        return "Keyboard and wheel passed";
    }
}
#endif
