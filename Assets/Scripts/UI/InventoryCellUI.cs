using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryCellUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    InventoryUI _ui;
    int _slot;
    InventoryEntry? _entry;
    Image _background, _icon;
    TextMeshProUGUI _name, _count, _number, _target;
    Outline _outline;
    bool _selected;
    public void Initialize(InventoryUI ui, int slot)
    {
        _ui = ui; _slot = slot; _background = GetComponent<Image>();
        var size = ((RectTransform)transform).sizeDelta;
        _icon = InventoryUI.Icon(InventoryUI.Rect("Icon", transform, Vector2.zero, new Vector2((size.x - 60) / 2, 43), new Vector2(60, 60)));
        _name = InventoryUI.Label("Name", transform, new Vector2(8, 8), new Vector2(size.x - 16, 30), 18);
        _name.alignment = TextAlignmentOptions.Center;
        _target = InventoryUI.Label("Target", transform, new Vector2(8, 3), new Vector2(size.x - 16, 18), 14);
        _target.alignment = TextAlignmentOptions.Center;
        _target.color = InventoryUI.AccentColor;
        _count = InventoryUI.Label("StackSize", transform, new Vector2(size.x - 58, 40), new Vector2(50, 26), 20);
        _count.alignment = TextAlignmentOptions.Right; _count.fontStyle = FontStyles.Bold;
        _number = InventoryUI.Label("SlotNumber", transform, new Vector2(8, size.y - 28), new Vector2(46, 24), 18);
        _number.color = InventoryUI.AccentColor;
        _outline = gameObject.AddComponent<Outline>(); _outline.effectDistance = new Vector2(2, -2); _outline.useGraphicAlpha = false;
    }
    public void Bind(InventoryEntry? entry, int count, bool selected)
    {
        _entry = entry; _selected = selected;
        _icon.sprite = entry?.Definition != null ? entry.Value.Definition.ItemIcon : null;
        _icon.enabled = _icon.sprite != null;
        bool targeted = entry.HasValue && entry.Value.IsInstance;
        _name.text = entry.HasValue ? entry.Value.Definition.DisplayName : "Empty";
        _name.rectTransform.anchoredPosition = new Vector2(8, targeted ? 22 : 8);
        _name.rectTransform.sizeDelta = new Vector2(((RectTransform)transform).sizeDelta.x - 16, targeted ? 24 : 30);
        string target = targeted ? entry.Value.TargetID ?? "" : "";
        _target.text = targeted ? "ID " + (target.Length > 6 ? target.Substring(target.Length - 6) : target) : "";
        _count.text = entry.HasValue ? "x" + count : "";
        _number.text = _slot >= 0 ? (_slot + 1).ToString() : "";
        _background.color = InventoryUI.CellColor;
        _outline.effectColor = selected ? InventoryUI.AccentColor : new Color(0, 0, 0, .35f);
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
    public void OnPointerEnter(PointerEventData data) { _background.color = new Color(.21f, .23f, .23f, 1); _outline.effectColor = InventoryUI.AccentColor; }
    public void OnPointerExit(PointerEventData data) { _background.color = InventoryUI.CellColor; _outline.effectColor = _selected ? InventoryUI.AccentColor : new Color(0, 0, 0, .35f); }
}
