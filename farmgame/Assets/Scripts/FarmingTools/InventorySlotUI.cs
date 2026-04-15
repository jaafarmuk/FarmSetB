using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _slotBackground;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private TextMeshProUGUI _quantityText;
    [SerializeField] private TextMeshProUGUI _itemLabelText;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _hoverColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _selectedColor = new Color(0.93f, 0.82f, 0.42f, 1f);

    private InventoryUI _inventoryUI;
    private Canvas _dragCanvas;
    private ItemData _item;
    private int _quantity;
    private int _slotIndex;
    private bool _isDragging;
    private bool _wasDroppedOnValidSlot;
    private bool _isSelected;

    private Transform _originalIconParent;
    private Vector2 _originalIconAnchoredPosition;
    private Vector2 _originalIconAnchorMin;
    private Vector2 _originalIconAnchorMax;
    private Vector2 _originalIconPivot;
    private Vector2 _originalIconSizeDelta;
    private Vector3 _originalIconLocalScale;
    private int _originalIconSiblingIndex;
    private bool _originalIconRaycastTarget;

    public int SlotIndex => _slotIndex;
    public bool HasItem => _item != null && _quantity > 0;

    private void Awake()
    {
        ApplyBackgroundColor(_normalColor);
    }

    public void Setup(InventoryUI inventoryUI, int slotIndex)
    {
        _inventoryUI = inventoryUI;
        _slotIndex = slotIndex;
        _dragCanvas = inventoryUI != null ? inventoryUI.DragCanvas : GetComponentInParent<Canvas>();
    }

    public void ConfigureRuntimeReferences(Image slotBackground, Image itemIcon, TextMeshProUGUI quantityText, TextMeshProUGUI itemLabelText)
    {
        _slotBackground = slotBackground;
        _itemIcon = itemIcon;
        _quantityText = quantityText;
        _itemLabelText = itemLabelText;
        ApplyCurrentBackgroundColor();
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        ApplyCurrentBackgroundColor();
    }

    public void SetSlot(ItemData item, int quantity)
    {
        _item = item;
        _quantity = quantity;

        if (item == null || quantity <= 0)
        {
            _itemIcon.enabled = false;
            _itemIcon.sprite = null;
            _quantityText.text = "";
            UpdateItemLabel(string.Empty, false);
            return;
        }

        bool hasIcon = item.Icon != null;
        bool shouldShowLabel = !hasIcon || IsGeneratedHotbarItem(item);
        _itemIcon.enabled = hasIcon;
        _itemIcon.sprite = item.Icon;
        _quantityText.text = quantity > 1 ? quantity.ToString() : "";
        UpdateItemLabel(GetShortLabel(item.ItemName), shouldShowLabel);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_inventoryUI == null || !HasItem || _itemIcon == null)
        {
            return;
        }

        if (_dragCanvas == null)
        {
            _dragCanvas = _inventoryUI.DragCanvas;
        }

        if (_dragCanvas == null)
        {
            return;
        }

        _inventoryUI.BeginSlotDrag(_slotIndex);
        _wasDroppedOnValidSlot = false;
        BeginIconDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        UpdateDraggedIconPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RestoreIconToSlot();

        if (_inventoryUI != null)
        {
            if (!_wasDroppedOnValidSlot)
            {
                _inventoryUI.HandleSlotDropOutside(_slotIndex);
            }

            _inventoryUI.EndSlotDrag();
        }

        _wasDroppedOnValidSlot = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_inventoryUI == null || eventData.pointerDrag == null)
        {
            return;
        }

        ItemSourceDragUI sourceDragUI = eventData.pointerDrag.GetComponent<ItemSourceDragUI>();

        if (sourceDragUI != null)
        {
            bool dropSucceeded = _inventoryUI.HandleSourceDrop(_slotIndex, sourceDragUI.SourceItem, sourceDragUI.DraggedAmount, out int amountRemaining);

            if (dropSucceeded)
            {
                sourceDragUI.RegisterValidDrop(amountRemaining);
            }

            return;
        }

        InventorySlotUI sourceSlotUI = eventData.pointerDrag.GetComponent<InventorySlotUI>();

        if (sourceSlotUI != null)
        {
            sourceSlotUI.RegisterValidSlotDrop();
            _inventoryUI.HandleSlotDrop(sourceSlotUI.SlotIndex, _slotIndex);
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

    private void BeginIconDrag(PointerEventData eventData)
    {
        RectTransform iconRect = _itemIcon.rectTransform;

        _originalIconParent = iconRect.parent;
        _originalIconAnchoredPosition = iconRect.anchoredPosition;
        _originalIconAnchorMin = iconRect.anchorMin;
        _originalIconAnchorMax = iconRect.anchorMax;
        _originalIconPivot = iconRect.pivot;
        _originalIconSizeDelta = iconRect.sizeDelta;
        _originalIconLocalScale = iconRect.localScale;
        _originalIconSiblingIndex = iconRect.GetSiblingIndex();
        _originalIconRaycastTarget = _itemIcon.raycastTarget;

        iconRect.SetParent(_dragCanvas.transform, true);
        iconRect.SetAsLastSibling();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);

        _itemIcon.raycastTarget = false;
        _isDragging = true;

        if (_quantityText != null)
        {
            _quantityText.text = string.Empty;
        }

        UpdateDraggedIconPosition(eventData);
    }

    private void UpdateDraggedIconPosition(PointerEventData eventData)
    {
        if (_dragCanvas == null || _itemIcon == null)
        {
            return;
        }

        RectTransform canvasRect = _dragCanvas.transform as RectTransform;
        RectTransform iconRect = _itemIcon.rectTransform;
        Camera eventCamera = _dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            iconRect.localPosition = localPoint;
        }
    }

    private void RestoreIconToSlot()
    {
        if (!_isDragging || _itemIcon == null || _originalIconParent == null)
        {
            return;
        }

        RectTransform iconRect = _itemIcon.rectTransform;

        iconRect.SetParent(_originalIconParent, false);
        iconRect.SetSiblingIndex(_originalIconSiblingIndex);
        iconRect.anchorMin = _originalIconAnchorMin;
        iconRect.anchorMax = _originalIconAnchorMax;
        iconRect.pivot = _originalIconPivot;
        iconRect.sizeDelta = _originalIconSizeDelta;
        iconRect.localScale = _originalIconLocalScale;
        iconRect.anchoredPosition = _originalIconAnchoredPosition;

        _itemIcon.raycastTarget = _originalIconRaycastTarget;
        _isDragging = false;

        UpdateQuantityTextDisplay();
    }

    private void UpdateQuantityTextDisplay()
    {
        if (_quantityText == null)
        {
            return;
        }

        _quantityText.text = _quantity > 1 ? _quantity.ToString() : string.Empty;
    }

    private void ApplyBackgroundColor(Color color)
    {
        if (_slotBackground == null)
        {
            return;
        }

        _slotBackground.color = color;
    }

    private void ApplyCurrentBackgroundColor()
    {
        ApplyBackgroundColor(_isSelected ? _selectedColor : _normalColor);
    }

    private void UpdateItemLabel(string labelText, bool isVisible)
    {
        if (_itemLabelText == null)
        {
            return;
        }

        _itemLabelText.text = isVisible ? labelText : string.Empty;
        _itemLabelText.enabled = isVisible;
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

    private static bool IsGeneratedHotbarItem(ItemData item)
    {
        return item != null &&
               !string.IsNullOrWhiteSpace(item.ItemId) &&
               (item.ItemId.StartsWith("tool_") || item.ItemId.StartsWith("crop_"));
    }

    private void RegisterValidSlotDrop()
    {
        _wasDroppedOnValidSlot = true;
    }
}
