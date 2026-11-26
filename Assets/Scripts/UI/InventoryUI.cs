using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerItemController))]
public class InventoryUI : MonoBehaviour
{
    public static readonly Color PanelColor = new(.035f, .043f, .043f, .65f);
    public static readonly Color CellColor = new(.145f, .16f, .149f, .20f);
    public static readonly Color AccentColor = new(.737f, .655f, .518f, .9f);
    public static readonly Color TextColor = new(.84f, .81f, .75f, 1f);
    public static readonly Color MutedColor = new(.65f, .65f, .59f, 1f);
    [Header("Inventory background")]
    [SerializeField, Range(0f, 24f)] float backgroundBlurRadius = 8f;
    [SerializeField, Range(0f, .6f)] float backgroundDimming = .18f;
    PlayerItemController _owner;
    InventoryBackgroundBlur _blur;
    RectTransform _canvas, _overlay, _content, _viewport, _hud;
    TextMeshProUGUI _empty, _hudTitle, _hudCount, _hudHelp, _feedback, _slotHint;
    Image _hudIcon, _dragIcon, _dim;
    readonly List<InventoryCellUI> _cells = new();
    readonly List<InventoryCellUI> _slots = new();
    bool _dirty = true, _gridDirty = true, _wasActive = true;
    InventoryEntry? _dragEntry, _hintEntry;
    Vector2 _lastCanvasSize;
    float _lastViewportHeight;
    const float RowHeight = 104f, RowSpacing = 12f;
    public RectTransform InventoryPanel { get; private set; }
    public RectTransform ItemsContent => _content;
    // Preserve existing callers that used this before the grid became a list.
    public RectTransform GridContent => _content;
    public RectTransform CanvasRect => _canvas;
    public bool IsOpen => isActiveAndEnabled && _owner != null && _owner.IsInventoryOpen && _owner.GameplayActive;
    public float BlurRadius => backgroundBlurRadius;

    void Start()
    {
        _owner = GetComponent<PlayerItemController>();
        var camera = GetComponent<PlayerController>().PlayerCamera;
        if (camera != null)
        {
            _blur = camera.GetComponent<InventoryBackgroundBlur>();
            if (_blur == null) _blur = camera.gameObject.AddComponent<InventoryBackgroundBlur>();
            _blur.Owner = this;
        }
        Build();
        _owner.ViewChanged += MarkDirty;
        _owner.Inventory.Changed += MarkGridDirty;
        Refresh();
    }
    void MarkDirty() => _dirty = true;
    void MarkGridDirty() { _gridDirty = true; _dirty = true; }
    void LateUpdate()
    {
        if (_owner == null || _canvas == null) return;
        bool active = _owner.GameplayActive;
        if (_wasActive != active) { _wasActive = active; _dirty = true; }
        if (_canvas.rect.size != _lastCanvasSize || !Mathf.Approximately(_lastViewportHeight, _viewport.rect.height)) UpdateGeometry();
        if (_dirty) Refresh();
    }
    void Build()
    {
        var root = new GameObject("InventoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        _canvas = root.GetComponent<RectTransform>();
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var events = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (events == null) events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
        if (!events.gameObject.activeInHierarchy) { events.transform.SetParent(null, false); events.gameObject.SetActive(true); }
        events.enabled = true;
        if (events.GetComponent<InputSystemUIInputModule>() == null)
        {
            foreach (var old in events.GetComponents<BaseInputModule>()) old.enabled = false;
            events.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        _hud = Rect("SelectedItem", _canvas, Vector2.zero, new Vector2(32, 36), new Vector2(330, 112));
        Background(_hud, new Color(.065f, .075f, .078f, .98f), false);
        Background(Rect("Accent", _hud, Vector2.zero, Vector2.zero, new Vector2(3, 112)), AccentColor, false);
        _hudIcon = Icon(Rect("Icon", _hud, Vector2.zero, new Vector2(18, 30), new Vector2(64, 64)));
        _hudTitle = Label("Item", _hud, new Vector2(98, 72), new Vector2(216, 34), 22);
        _hudCount = Label("Quantity", _hud, new Vector2(98, 48), new Vector2(216, 24), 20);
        _hudHelp = Label("Controls", _hud, new Vector2(18, 10), new Vector2(300, 20), 16);

        _overlay = Stretch("InventoryOverlay", _canvas, Vector2.zero, Vector2.zero);
        _dim = Background(_overlay, new Color(0, 0, 0, backgroundDimming), true);
        InventoryPanel = Stretch("InventoryPanel", _overlay, Vector2.zero, Vector2.zero);
        var pockets = Rect("PocketsPanel", InventoryPanel, Vector2.zero, Vector2.zero, Vector2.zero);
        pockets.anchorMax = new Vector2(0, 1); pockets.offsetMin = new Vector2(36, 164); pockets.offsetMax = new Vector2(496, -64);
        Background(pockets, PanelColor, true);
        var title = Label("Title", pockets, Vector2.zero, new Vector2(360, 36), 24);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0, 1);
        title.rectTransform.pivot = new Vector2(0, 1); title.rectTransform.anchoredPosition = new Vector2(32, -28);
        title.text = "POCKETS"; title.characterSpacing = 4; title.color = MutedColor;
        var closeRect = Rect("Close", pockets, Vector2.zero, new Vector2(32, 22), new Vector2(396, 30));
        var close = closeRect.gameObject.AddComponent<Button>(); close.targetGraphic = Background(closeRect, Color.clear, true);
        var colors = close.colors; colors.highlightedColor = new Color(1, 1, 1, .05f); colors.pressedColor = new Color(1, 1, 1, .09f); close.colors = colors;
        close.onClick.AddListener(() => _owner.SetInventoryOpen(false));
        var closeText = Label("Label", closeRect, Vector2.zero, new Vector2(396, 30), 16);
        closeText.text = "I / TAB / ESC    CLOSE"; closeText.characterSpacing = 1; closeText.color = MutedColor;

        var scrollRect = Stretch("ItemsScroll", pockets, new Vector2(24, 72), new Vector2(-24, -88));
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        _viewport = Stretch("Viewport", scrollRect, Vector2.zero, Vector2.zero);
        Background(_viewport, Color.clear, true); _viewport.gameObject.AddComponent<RectMask2D>();
        _content = Rect("ItemsList", _viewport, new Vector2(0, 1), Vector2.zero, Vector2.zero);
        _content.anchorMax = Vector2.one; _content.pivot = new Vector2(0, 1);
        var list = _content.gameObject.AddComponent<VerticalLayoutGroup>();
        list.spacing = RowSpacing; list.childControlWidth = true; list.childForceExpandWidth = true;
        list.childControlHeight = false; list.childForceExpandHeight = false;
        scroll.content = _content; scroll.viewport = _viewport; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
        var rail = Rect("ScrollRail", pockets, Vector2.zero, Vector2.zero, Vector2.zero);
        rail.anchorMax = new Vector2(0, 1); rail.offsetMin = new Vector2(442, 72); rail.offsetMax = new Vector2(445, -88);
        Background(rail, new Color(.65f, .65f, .59f, .06f), true);
        var sliding = Stretch("SlidingArea", rail, Vector2.zero, Vector2.zero);
        var handle = Stretch("Handle", sliding, Vector2.zero, Vector2.zero);
        var handleImage = Background(handle, new Color(.65f, .65f, .59f, .35f), true);
        var scrollbar = rail.gameObject.AddComponent<Scrollbar>(); scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handle; scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        _empty = Label("EmptyInventory", _viewport, Vector2.zero, new Vector2(380, 72), 22);
        _empty.rectTransform.anchorMin = _empty.rectTransform.anchorMax = new Vector2(0, 1);
        _empty.rectTransform.pivot = new Vector2(0, 1); _empty.rectTransform.anchoredPosition = new Vector2(8, -24);
        _empty.textWrappingMode = TextWrappingModes.Normal; _empty.text = "Your pockets are empty."; _empty.color = MutedColor;

        for (int i = 0; i < ActiveItemSlots.Count; i++)
        {
            var cell = CreateCell("ActiveSlot" + (i + 1), InventoryPanel, Vector2.zero, new Vector2(104, 88));
            cell.Initialize(this, i); _slots.Add(cell);
        }
        _slotHint = Label("SlotHint", InventoryPanel, Vector2.zero, new Vector2(1064, 28), 16);
        _slotHint.alignment = TextAlignmentOptions.Center; _slotHint.color = MutedColor;
        ShowSlotHint(null);
        _feedback = Label("ItemFeedback", _canvas, new Vector2(32, 162), new Vector2(600, 32), 22); _feedback.color = AccentColor;
        _dragIcon = Icon(Rect("DraggedItem", _canvas, Vector2.zero, Vector2.zero, new Vector2(56, 56)));
        _dragIcon.rectTransform.pivot = new Vector2(.5f, .5f); _dragIcon.gameObject.SetActive(false);
        Canvas.ForceUpdateCanvases(); UpdateGeometry();
    }
    void UpdateGeometry()
    {
        _lastCanvasSize = _canvas.rect.size; _lastViewportHeight = _viewport.rect.height;
        float start = 532 + Mathf.Max(0, (_lastCanvasSize.x - 568 - 1064) * .5f);
        for (int i = 0; i < _slots.Count; i++) ((RectTransform)_slots[i].transform).anchoredPosition = new Vector2(start + i * 120, 36);
        _slotHint.rectTransform.anchoredPosition = new Vector2(start, 140);
        UpdateContentHeight();
    }
    void UpdateContentHeight() => _content.sizeDelta = new Vector2(0, Mathf.Max(_viewport.rect.height, _cells.Count * (RowHeight + RowSpacing) - RowSpacing));
    InventoryCellUI CreateCell(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = Rect(name, parent, Vector2.zero, position, size); Background(rect, CellColor, true);
        return rect.gameObject.AddComponent<InventoryCellUI>();
    }
    void Refresh()
    {
        if (_owner.Slots == null) return;
        _dirty = false;
        _overlay.gameObject.SetActive(IsOpen);
        _dim.color = new Color(0, 0, 0, backgroundDimming);
        _hud.gameObject.SetActive(_owner.GameplayActive && !IsOpen);
        _feedback.gameObject.SetActive(_owner.GameplayActive && !IsOpen); _feedback.text = _owner.Feedback ?? "";
        if (!IsOpen) EndDrag();
        if (_gridDirty && _dragEntry == null)
        {
            _gridDirty = false;
            foreach (var cell in _cells) { cell.gameObject.SetActive(false); Destroy(cell.gameObject); }
            _cells.Clear();
            foreach (var entry in _owner.Inventory.Entries)
            {
                var cell = CreateCell("InventoryItem", _content, Vector2.zero, new Vector2(412, RowHeight));
                cell.Initialize(this, -1); cell.Bind(entry, _owner.Inventory.Quantity(entry), false, _owner.Inventory.GetInventoryDetail(entry)); _cells.Add(cell);
            }
            UpdateContentHeight(); _empty.gameObject.SetActive(_cells.Count == 0);
        }
        foreach (var cell in _cells)
        {
            var entry = cell.Entry.Value;
            cell.Bind(entry, _owner.Inventory.Quantity(entry), _owner.Slots.Selected.HasValue && _owner.Slots.Selected.Value.Equals(entry), _owner.Inventory.GetInventoryDetail(entry));
        }
        for (int i = 0; i < _slots.Count; i++)
        {
            var entry = _owner.Slots[i];
            _slots[i].Bind(entry, entry.HasValue ? _owner.Inventory.Quantity(entry.Value) : 0, i == _owner.Slots.SelectedIndex, entry.HasValue ? _owner.Inventory.GetInventoryDetail(entry.Value) : null);
        }
        if (_hintEntry.HasValue && _owner.Inventory.Quantity(_hintEntry.Value) == 0) ShowSlotHint(null);
        var selected = _owner.Slots.Selected; var definition = selected?.Definition;
        _hudIcon.sprite = definition != null ? definition.ItemIcon : null; _hudIcon.enabled = _hudIcon.sprite != null;
        _hudTitle.text = $"{_owner.Slots.SelectedIndex + 1}  {(definition != null ? definition.DisplayName : "Empty slot")}";
        _hudCount.text = definition == null ? "No item assigned" : definition.UseEffect == ItemUseEffect.Gun
            ? $"{_owner.Inventory.LoadedRounds(definition)} / {definition.MagazineCapacity}   |   {_owner.Inventory.Quantity(definition.Ammunition)} reserve" : $"x{_owner.Inventory.Quantity(selected.Value)}";
        _hudHelp.text = definition != null && definition.UseEffect == ItemUseEffect.Gun ? "LMB fire   /   R reload   /   I inventory"
            : definition != null && definition.UseEffect == ItemUseEffect.StaminaPotion ? "LMB drink   /   I inventory" : "I / Tab inventory   /   1-9 select";
    }
    public void ShowSlotHint(InventoryEntry? entry)
    {
        _hintEntry = entry;
        if (_slotHint == null) return;
        string detail = entry.HasValue ? _owner.Inventory.GetInventoryDetail(entry.Value) : null;
        _slotHint.text = entry.HasValue ? entry.Value.Definition.DisplayName + (string.IsNullOrWhiteSpace(detail) ? "" : "   /   " + detail) : "DRAG TO EQUIP    /    RIGHT CLICK TO CLEAR";
    }
    public void SelectSlot(int slot) => _owner.Slots.Select(slot);
    public void ClearSlot(int slot) { _owner.Slots.Clear(slot); ShowSlotHint(null); }
    public void BeginDrag(InventoryEntry entry, PointerEventData data)
    {
        if (!IsOpen) return;
        _dragEntry = entry; _dragIcon.sprite = entry.Definition.ItemIcon; _dragIcon.gameObject.SetActive(_dragIcon.sprite != null); Drag(data);
    }
    public void Drag(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, data.position, data.pressEventCamera, out var point);
        _dragIcon.rectTransform.localPosition = point;
    }
    public void Drop(int slot) { if (IsOpen && _dragEntry.HasValue) _owner.Slots.Assign(slot, _dragEntry.Value); EndDrag(); }
    public void EndDrag() { _dragEntry = null; if (_dragIcon != null) _dragIcon.gameObject.SetActive(false); if (_gridDirty) _dirty = true; }
    void OnEnable() { if (_canvas != null) _canvas.gameObject.SetActive(true); _dirty = true; }
    void OnDisable()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(false);
        if (_owner != null && _owner.IsInventoryOpen) _owner.SetInventoryOpen(false);
        EndDrag();
    }
    void OnValidate() => _dirty = true;
    void OnDestroy()
    {
        if (_blur != null && _blur.Owner == this) Destroy(_blur);
        if (_owner == null) return;
        _owner.ViewChanged -= MarkDirty;
        if (_owner.Inventory != null) _owner.Inventory.Changed -= MarkGridDirty;
    }
    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static RectTransform Stretch(string name, Transform parent, Vector2 bottomLeft, Vector2 topRight)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero); rect.anchorMax = Vector2.one;
        rect.offsetMin = bottomLeft; rect.offsetMax = topRight; return rect;
    }
    public static Image Background(RectTransform rect, Color color, bool raycast)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return image;
    }
    public static Image Icon(RectTransform rect) { var icon = Background(rect, Color.white, false); icon.preserveAspect = true; return icon; }
    public static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        var text = Rect(name, parent, Vector2.zero, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize; text.color = TextColor; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Truncate; text.richText = false; return text;
    }
}
