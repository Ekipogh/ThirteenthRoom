using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerItemController))]
public class InventoryUI : MonoBehaviour
{
    public static readonly Color PanelColor = new(0.065f, 0.075f, 0.078f, 0.98f);
    public static readonly Color CellColor = new(0.12f, 0.135f, 0.14f, 1f);
    public static readonly Color AccentColor = new(0.84f, 0.68f, 0.39f, 1f);
    public static readonly Color TextColor = new(0.94f, 0.92f, 0.86f, 1f);
    PlayerItemController _owner;
    RectTransform _canvas, _overlay, _content, _hud;
    TextMeshProUGUI _empty, _hudTitle, _hudCount, _hudHelp, _feedback;
    Image _hudIcon, _dragIcon;
    readonly List<InventoryCellUI> _cells = new();
    readonly List<InventoryCellUI> _slots = new();
    bool _dirty = true, _gridDirty = true;
    InventoryEntry? _dragEntry;
    bool _wasActive = true;
    public RectTransform InventoryPanel { get; private set; }
    public RectTransform GridContent => _content;
    public RectTransform CanvasRect => _canvas;

    void Start()
    {
        _owner = GetComponent<PlayerItemController>();
        Build();
        _owner.ViewChanged += MarkDirty;
        _owner.Inventory.Changed += MarkGridDirty;
        Refresh();
    }
    void MarkDirty() => _dirty = true;
    void MarkGridDirty() { _gridDirty = true; _dirty = true; }
    void LateUpdate()
    {
        bool active = _owner.GameplayActive;
        if (_wasActive != active) { _wasActive = active; _dirty = true; }
        if (_dirty) Refresh();
    }
    void Build()
    {
        var root = new GameObject("InventoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        _canvas = root.GetComponent<RectTransform>();
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var events = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (events == null)
            events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
        // The scene's EventSystem may live under the initially hidden game-over panel.
        // Keep that same system available to inventory and game-over UI instead of creating a duplicate.
        if (!events.gameObject.activeInHierarchy)
        {
            events.transform.SetParent(null, false);
            events.gameObject.SetActive(true);
        }
        events.enabled = true;
        if (events.GetComponent<InputSystemUIInputModule>() == null)
        {
            foreach (var old in events.GetComponents<BaseInputModule>()) old.enabled = false;
            events.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        _hud = Rect("SelectedItem", _canvas, new Vector2(0, 0), new Vector2(32, 36), new Vector2(330, 112));
        Background(_hud, PanelColor, false);
        var accent = Rect("Accent", _hud, Vector2.zero, Vector2.zero, new Vector2(3, 112));
        Background(accent, AccentColor, false);
        _hudIcon = Icon(Rect("Icon", _hud, Vector2.zero, new Vector2(18, 30), new Vector2(64, 64)));
        _hudTitle = Label("Item", _hud, new Vector2(98, 72), new Vector2(216, 34), 22);
        _hudCount = Label("Quantity", _hud, new Vector2(98, 48), new Vector2(216, 24), 20);
        _hudHelp = Label("Controls", _hud, new Vector2(18, 10), new Vector2(300, 20), 16);

        _overlay = Rect("InventoryOverlay", _canvas, Vector2.zero, Vector2.zero, Vector2.zero);
        _overlay.anchorMax = Vector2.one;
        _overlay.offsetMin = _overlay.offsetMax = Vector2.zero;
        Background(_overlay, new Color(0, 0, 0, 0.62f), true);
        InventoryPanel = Rect("InventoryPanel", _overlay, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1360, 820));
        InventoryPanel.pivot = new Vector2(.5f, .5f);
        Background(InventoryPanel, PanelColor, true);
        var title = Label("Title", InventoryPanel, new Vector2(40, 744), new Vector2(1000, 44), 36);
        title.text = "INVENTORY";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 5;
        var closeRect = Rect("Close", InventoryPanel, Vector2.zero, new Vector2(1230, 748), new Vector2(90, 36));
        var closeImage = Background(closeRect, CellColor, true);
        var close = closeRect.gameObject.AddComponent<Button>(); close.targetGraphic = closeImage;
        close.onClick.AddListener(() => _owner.SetInventoryOpen(false));
        var closeLabel = Label("Label", closeRect, Vector2.zero, new Vector2(90, 36), 18);
        closeLabel.text = "CLOSE"; closeLabel.alignment = TextAlignmentOptions.Center;
        Label("Instructions", InventoryPanel, new Vector2(40, 702), new Vector2(1280, 28), 20).text = "Drag items to active slots. Stay alert - the mansion does not pause.";

        var scrollRect = Rect("ItemsScroll", InventoryPanel, Vector2.zero, new Vector2(40, 258), new Vector2(1280, 420));
        Background(scrollRect, new Color(.04f, .05f, .052f, 1), true);
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect("Viewport", scrollRect, Vector2.zero, Vector2.zero, new Vector2(1280, 420));
        viewport.gameObject.AddComponent<RectMask2D>();
        _content = Rect("ItemsGrid", viewport, new Vector2(0, 1), Vector2.zero, new Vector2(1280, 420));
        _content.pivot = new Vector2(0, 1);
        var grid = _content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(172, 132); grid.spacing = new Vector2(12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 7;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        scroll.content = _content; scroll.viewport = viewport;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35;
        _empty = Label("EmptyInventory", scrollRect, new Vector2(24, 180), new Vector2(1200, 40), 24);
        _empty.text = "Your inventory is empty. Search the mansion for supplies.";
        Label("ActiveSlotsTitle", InventoryPanel, new Vector2(40, 206), new Vector2(1280, 28), 22).text = "ACTIVE SLOTS   /   1 - 9";
        for (int i = 0; i < ActiveItemSlots.Count; i++)
        {
            var cell = CreateCell("ActiveSlot" + (i + 1), InventoryPanel, new Vector2(40 + i * 144, 68), new Vector2(128, 120));
            cell.Initialize(this, i); _slots.Add(cell);
        }
        Label("Footer", InventoryPanel, new Vector2(40, 24), new Vector2(1280, 26), 18).text = "1-9 select    |    Right-click clears a slot    |    I / Tab / Esc close    |    Mouse wheel switches items in game";
        _feedback = Label("ItemFeedback", _canvas, new Vector2(32, 162), new Vector2(600, 32), 22);
        _feedback.color = AccentColor;
        _dragIcon = Icon(Rect("DraggedItem", _canvas, Vector2.zero, Vector2.zero, new Vector2(72, 72)));
        _dragIcon.rectTransform.pivot = new Vector2(.5f, .5f);
        _dragIcon.gameObject.SetActive(false);
    }

    InventoryCellUI CreateCell(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = Rect(name, parent, Vector2.zero, position, size);
        Background(rect, CellColor, true);
        return rect.gameObject.AddComponent<InventoryCellUI>();
    }
    void Refresh()
    {
        if (_owner.Slots == null) return;
        _dirty = false;
        _overlay.gameObject.SetActive(_owner.IsInventoryOpen && _owner.GameplayActive);
        _hud.gameObject.SetActive(_owner.GameplayActive && !_owner.IsInventoryOpen);
        _feedback.gameObject.SetActive(_owner.GameplayActive && !_owner.IsInventoryOpen);
        _feedback.text = _owner.Feedback ?? "";
        if (!_owner.IsInventoryOpen) EndDrag();
        if (_gridDirty && _dragEntry == null)
        {
            _gridDirty = false;
            foreach (var cell in _cells) { cell.gameObject.SetActive(false); Destroy(cell.gameObject); }
            _cells.Clear();
            foreach (var entry in _owner.Inventory.Entries)
            {
                var cell = CreateCell("InventoryItem", _content, Vector2.zero, new Vector2(172, 132));
                cell.Initialize(this, -1); cell.Bind(entry, _owner.Inventory.Quantity(entry), false); _cells.Add(cell);
            }
            int rows = Mathf.CeilToInt(_cells.Count / 7f);
            _content.sizeDelta = new Vector2(1280, Mathf.Max(420, rows * 144 - 12));
            _empty.gameObject.SetActive(_cells.Count == 0);
        }
        for (int i = 0; i < _slots.Count; i++)
        {
            var entry = _owner.Slots[i];
            _slots[i].Bind(entry, entry.HasValue ? _owner.Inventory.Quantity(entry.Value) : 0, i == _owner.Slots.SelectedIndex);
        }
        var selected = _owner.Slots.Selected;
        var definition = selected?.Definition;
        _hudIcon.sprite = definition != null ? definition.ItemIcon : null;
        _hudIcon.enabled = _hudIcon.sprite != null;
        _hudTitle.text = $"{_owner.Slots.SelectedIndex + 1}  {(definition != null ? definition.DisplayName : "Empty slot")}";
        _hudCount.text = definition == null ? "No item assigned" : definition.UseEffect == ItemUseEffect.Gun
            ? $"{_owner.Inventory.LoadedRounds(definition)} / {definition.MagazineCapacity}   |   {_owner.Inventory.Quantity(definition.Ammunition)} reserve"
            : $"x{_owner.Inventory.Quantity(selected.Value)}";
        _hudHelp.text = definition != null && definition.UseEffect == ItemUseEffect.Gun ? "LMB fire   /   R reload   /   I inventory"
            : definition != null && definition.UseEffect == ItemUseEffect.StaminaPotion ? "LMB drink   /   I inventory" : "I / Tab inventory   /   1-9 select";
    }
    public void SelectSlot(int slot) => _owner.Slots.Select(slot);
    public void ClearSlot(int slot) => _owner.Slots.Clear(slot);
    public void BeginDrag(InventoryEntry entry, PointerEventData data)
    {
        if (!_owner.IsInventoryOpen) return;
        _dragEntry = entry; _dragIcon.sprite = entry.Definition.ItemIcon;
        _dragIcon.gameObject.SetActive(_dragIcon.sprite != null); Drag(data);
    }
    public void Drag(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, data.position, data.pressEventCamera, out var point);
        _dragIcon.rectTransform.localPosition = point;
    }
    public void Drop(int slot)
    {
        if (_owner.IsInventoryOpen && _dragEntry.HasValue) _owner.Slots.Assign(slot, _dragEntry.Value);
        EndDrag();
    }
    public void EndDrag() { _dragEntry = null; if (_dragIcon != null) _dragIcon.gameObject.SetActive(false); if (_gridDirty) _dirty = true; }
    void OnDestroy()
    {
        if (_owner == null) return;
        _owner.ViewChanged -= MarkDirty;
        if (_owner.Inventory != null) _owner.Inventory.Changed -= MarkGridDirty;
    }
    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    public static Image Background(RectTransform rect, Color color, bool raycast)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return image;
    }
    public static Image Icon(RectTransform rect)
    {
        var icon = Background(rect, Color.white, false); icon.preserveAspect = true; return icon;
    }
    public static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        var text = Rect(name, parent, Vector2.zero, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize; text.color = TextColor; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Truncate;
        text.richText = false;
        return text;
    }
}
