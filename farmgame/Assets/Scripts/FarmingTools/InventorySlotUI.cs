using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image _slotBackground;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private TextMeshProUGUI _quantityText;
    [SerializeField] private TextMeshProUGUI _itemLabelText;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _hoverColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _selectedColor = new Color(0.93f, 0.82f, 0.42f, 1f);

    private InventoryUI _inventoryUi;
    private ItemData _item;
    private int _quantity;
    private int _slotIndex;
    private bool _isDragging;
    private bool _isSelected;
    private bool _wasDroppedOnSlot;

    private Transform _originalParent;
    private Vector2 _originalAnchoredPosition;
    private Vector2 _originalAnchorMin;
    private Vector2 _originalAnchorMax;
    private Vector2 _originalPivot;
    private Vector2 _originalSizeDelta;
    private Vector3 _originalScale;
    private int _originalSiblingIndex;
    private bool _originalRaycastTarget;

    public int SlotIndex => _slotIndex;
    public bool HasItem => _item != null && _quantity > 0;

    private void Awake()
    {
        ApplyCurrentBackgroundColor();
    }

    public void Setup(InventoryUI inventoryUi, int slotIndex)
    {
        _inventoryUi = inventoryUi;
        _slotIndex = slotIndex;
    }

    public void ConfigureRuntimeReferences(Image slotBackground, Image itemIcon, TextMeshProUGUI quantityText, TextMeshProUGUI itemLabelText)
    {
        _slotBackground = slotBackground;
        _itemIcon = itemIcon;
        _quantityText = quantityText;
        _itemLabelText = itemLabelText;
        ApplyCurrentBackgroundColor();
    }

    public void SetSlot(ItemData item, int quantity)
    {
        _item = item;
        _quantity = quantity;

        if (!HasItem)
        {
            _itemIcon.enabled = false;
            _itemIcon.sprite = null;
            _quantityText.text = string.Empty;
            SetLabel(string.Empty, false);
            return;
        }

        bool hasIcon = item.Icon != null;
        _itemIcon.enabled = hasIcon;
        _itemIcon.sprite = item.Icon;
        _quantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
        SetLabel(GetShortLabel(item.ItemName), !hasIcon);
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        ApplyCurrentBackgroundColor();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !HasItem || _inventoryUi == null || _itemIcon == null)
        {
            return;
        }

        Canvas dragCanvas = _inventoryUi.DragCanvas;

        if (dragCanvas == null)
        {
            return;
        }

        _inventoryUi.BeginSlotDrag(_slotIndex);
        _wasDroppedOnSlot = false;
        BeginDragVisual(dragCanvas, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _inventoryUi == null || _itemIcon == null)
        {
            return;
        }

        UpdateDraggedIconPosition(_inventoryUi.DragCanvas, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RestoreDraggedIcon();

        if (_inventoryUi != null)
        {
            if (!_wasDroppedOnSlot)
            {
                _inventoryUi.HandleSlotDropOutside(_slotIndex);
            }

            _inventoryUi.EndSlotDrag();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_inventoryUi == null || eventData.pointerDrag == null)
        {
            return;
        }

        InventorySlotUI sourceSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();

        if (sourceSlot == null)
        {
            return;
        }

        sourceSlot._wasDroppedOnSlot = true;
        _inventoryUi.HandleSlotDrop(sourceSlot.SlotIndex, _slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !_isDragging)
        {
            _inventoryUi?.HandleSlotClicked(_slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyBackgroundColor(_hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyCurrentBackgroundColor();
    }

    private void BeginDragVisual(Canvas dragCanvas, PointerEventData eventData)
    {
        RectTransform iconRect = _itemIcon.rectTransform;

        _originalParent = iconRect.parent;
        _originalAnchoredPosition = iconRect.anchoredPosition;
        _originalAnchorMin = iconRect.anchorMin;
        _originalAnchorMax = iconRect.anchorMax;
        _originalPivot = iconRect.pivot;
        _originalSizeDelta = iconRect.sizeDelta;
        _originalScale = iconRect.localScale;
        _originalSiblingIndex = iconRect.GetSiblingIndex();
        _originalRaycastTarget = _itemIcon.raycastTarget;

        iconRect.SetParent(dragCanvas.transform, true);
        iconRect.SetAsLastSibling();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);

        _itemIcon.raycastTarget = false;
        _isDragging = true;
        _quantityText.text = string.Empty;

        UpdateDraggedIconPosition(dragCanvas, eventData);
    }

    private void UpdateDraggedIconPosition(Canvas dragCanvas, PointerEventData eventData)
    {
        if (dragCanvas == null || _itemIcon == null)
        {
            return;
        }

        RectTransform canvasRect = dragCanvas.transform as RectTransform;
        RectTransform iconRect = _itemIcon.rectTransform;
        Camera eventCamera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            iconRect.localPosition = localPoint;
        }
    }

    private void RestoreDraggedIcon()
    {
        if (!_isDragging || _itemIcon == null || _originalParent == null)
        {
            return;
        }

        RectTransform iconRect = _itemIcon.rectTransform;

        iconRect.SetParent(_originalParent, false);
        iconRect.SetSiblingIndex(_originalSiblingIndex);
        iconRect.anchorMin = _originalAnchorMin;
        iconRect.anchorMax = _originalAnchorMax;
        iconRect.pivot = _originalPivot;
        iconRect.sizeDelta = _originalSizeDelta;
        iconRect.localScale = _originalScale;
        iconRect.anchoredPosition = _originalAnchoredPosition;

        _itemIcon.raycastTarget = _originalRaycastTarget;
        _isDragging = false;
        _quantityText.text = _quantity > 1 ? _quantity.ToString() : string.Empty;
        _wasDroppedOnSlot = false;
    }

    private void ApplyCurrentBackgroundColor()
    {
        ApplyBackgroundColor(_isSelected ? _selectedColor : _normalColor);
    }

    private void ApplyBackgroundColor(Color color)
    {
        if (_slotBackground != null)
        {
            _slotBackground.color = color;
        }
    }

    private void SetLabel(string labelText, bool visible)
    {
        if (_itemLabelText == null)
        {
            return;
        }

        _itemLabelText.text = visible ? labelText : string.Empty;
        _itemLabelText.enabled = visible;
    }

    private static string GetShortLabel(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return string.Empty;
        }

        string[] words = itemName.Split(' ');

        if (words.Length >= 2)
        {
            return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";
        }

        string trimmedName = itemName.Trim();
        return trimmedName.Length <= 3 ? trimmedName.ToUpperInvariant() : trimmedName.Substring(0, 3).ToUpperInvariant();
    }
}
