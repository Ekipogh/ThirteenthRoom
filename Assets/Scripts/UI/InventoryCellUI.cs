using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryCellUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    InventoryUI _ui;
    int _slot;
    InventoryEntry? _entry;
    Image _background, _icon, _indicator;
    readonly Image[] _border = new Image[4];
    TextMeshProUGUI _name, _count, _number, _detail, _fallback;
    bool _selected, _hovered;
    public InventoryEntry? Entry => _entry;

    public void Initialize(InventoryUI ui, int slot)
    {
        _ui = ui; _slot = slot; _background = GetComponent<Image>();
        var size = ((RectTransform)transform).sizeDelta;
        bool row = slot < 0;
        _icon = InventoryUI.Icon(InventoryUI.Rect("Icon", transform, Vector2.zero,
            row ? new Vector2(18, 28) : new Vector2((size.x - 40) / 2, 26), row ? new Vector2(48, 48) : new Vector2(40, 40)));
        _fallback = InventoryUI.Label("IconFallback", transform, _icon.rectTransform.anchoredPosition, _icon.rectTransform.sizeDelta, row ? 28 : 24);
        _fallback.alignment = TextAlignmentOptions.Center; _fallback.color = InventoryUI.AccentColor;
        _name = InventoryUI.Label("Name", transform, new Vector2(86, 34), new Vector2(size.x - 154, 34), 24);
        _detail = InventoryUI.Label("Detail", transform, new Vector2(86, 18), new Vector2(size.x - 102, 24), 16);
        _detail.color = InventoryUI.MutedColor;
        _detail.overflowMode = TextOverflowModes.Truncate;
        _count = InventoryUI.Label("StackSize", transform, new Vector2(size.x - 64, row ? 34 : 4), new Vector2(48, 30), row ? 20 : 16);
        _count.alignment = TextAlignmentOptions.Right;
        _number = InventoryUI.Label("SlotNumber", transform, new Vector2(8, size.y - 27), new Vector2(32, 24), 16);
        _number.color = InventoryUI.MutedColor;
        if (row)
        {
            _indicator = InventoryUI.Background(InventoryUI.Rect("Selection", transform, Vector2.zero, new Vector2(0, 16), new Vector2(2, size.y - 32)), InventoryUI.AccentColor, false);
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                var edge = InventoryUI.Rect("Border" + i, transform, Vector2.zero, Vector2.zero, Vector2.zero);
                if (i < 2) { edge.anchorMax = new Vector2(1, 0); edge.sizeDelta = new Vector2(0, 2); edge.anchoredPosition = new Vector2(0, i == 0 ? 0 : size.y - 2); }
                else { edge.anchorMax = new Vector2(0, 1); edge.sizeDelta = new Vector2(2, 0); edge.anchoredPosition = new Vector2(i == 2 ? 0 : size.x - 2, 0); }
                _border[i] = InventoryUI.Background(edge, Color.clear, false);
            }
        }
    }
    public void Bind(InventoryEntry? entry, int count, bool selected, string detail = null)
    {
        _entry = entry; _selected = selected;
        bool row = _slot < 0;
        _icon.sprite = entry?.Definition != null ? entry.Value.Definition.ItemIcon : null;
        _icon.enabled = _icon.sprite != null;
        _fallback.gameObject.SetActive(entry.HasValue && _icon.sprite == null);
        string displayName = entry.HasValue ? entry.Value.Definition.DisplayName : "";
        _fallback.text = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Substring(0, 1).ToUpperInvariant();
        bool hasDetail = entry.HasValue && !string.IsNullOrWhiteSpace(detail);
        _name.text = entry.HasValue ? entry.Value.Definition.DisplayName : "";
        _name.gameObject.SetActive(row);
        _name.rectTransform.anchoredPosition = new Vector2(86, hasDetail ? 44 : 34);
        _detail.text = hasDetail ? detail : "";
        _detail.gameObject.SetActive(row && hasDetail);
        _count.text = entry.HasValue ? "x" + count : "";
        _number.text = row ? "" : (_slot + 1).ToString();
        Paint();
    }
    void Paint()
    {
        bool row = _slot < 0;
        _background.color = row
            ? (_selected ? new Color(.71f, .67f, .53f, .12f) : _hovered ? new Color(.55f, .57f, .53f, .10f) : InventoryUI.CellColor)
            : new Color(.035f, .043f, .039f, _hovered ? .72f : .54f);
        if (_indicator != null) _indicator.enabled = _selected;
        foreach (var edge in _border)
            if (edge != null) edge.color = _selected ? InventoryUI.AccentColor : new Color(.34f, .36f, .31f, _hovered ? .85f : .5f);
        _number.color = _selected ? InventoryUI.AccentColor : InventoryUI.MutedColor;
    }
    public void OnBeginDrag(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left && _entry.HasValue) _ui.BeginDrag(_entry.Value, data); }
    public void OnDrag(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) _ui.Drag(data); }
    public void OnEndDrag(PointerEventData data) => _ui.EndDrag();
    public void OnDrop(PointerEventData data) { if (_slot >= 0) _ui.Drop(_slot); }
    public void OnPointerClick(PointerEventData data)
    {
        if (_slot < 0 || data.dragging) return;
        if (data.button == PointerEventData.InputButton.Right) _ui.ClearSlot(_slot);
        else if (data.button == PointerEventData.InputButton.Left) _ui.SelectSlot(_slot);
    }
    public void OnPointerEnter(PointerEventData data) { _hovered = true; Paint(); if (_slot >= 0) _ui.ShowSlotHint(_entry); }
    public void OnPointerExit(PointerEventData data) { _hovered = false; Paint(); if (_slot >= 0) _ui.ShowSlotHint(null); }
}
