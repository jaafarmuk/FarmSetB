using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventorySystem _inventorySystem;
    [SerializeField] private Transform _slotsContainer;
    [SerializeField] private InventorySlotUI _slotPrefab;
    [SerializeField] private Canvas _dragCanvas;

    private readonly List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();
    private int _dragSourceIndex = -1;

    public Canvas DragCanvas => _dragCanvas;
    private bool IsDraggingSlot => _dragSourceIndex >= 0;

    private void Awake()
    {
        if (_dragCanvas == null)
        {
            _dragCanvas = GetComponentInParent<Canvas>();
        }
    }

    private void OnEnable()
    {
        if (_inventorySystem != null)
        {
            _inventorySystem.InventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (_inventorySystem != null)
        {
            _inventorySystem.InventoryChanged -= HandleInventoryChanged;
        }

        _dragSourceIndex = -1;
    }

    private void Start()
    {
        CreateSlots();
        RefreshUI();
    }

    private void CreateSlots()
    {
        if (_inventorySystem == null || _slotsContainer == null || _slotPrefab == null)
        {
            return;
        }

        List<InventorySlotData> slots = _inventorySystem.GetSlots();

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotUI slotUI = Instantiate(_slotPrefab, _slotsContainer);
            slotUI.Setup(this, i);
            _slotUIs.Add(slotUI);
        }
    }

    private void HandleInventoryChanged()
    {
        if (IsDraggingSlot)
        {
            return;
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        List<InventorySlotData> slots = _inventorySystem.GetSlots();
        int count = Mathf.Min(slots.Count, _slotUIs.Count);

        for (int i = 0; i < count; i++)
        {
            _slotUIs[i].SetSlot(slots[i].Item, slots[i].Quantity);
        }
    }

    public void BeginSlotDrag(int slotIndex)
    {
        _dragSourceIndex = slotIndex;
    }

    public void HandleSlotDrop(int fromIndex, int toIndex)
    {
        if (_inventorySystem == null)
        {
            return;
        }

        _inventorySystem.MoveOrSwapItem(fromIndex, toIndex);
    }

    public bool HandleSourceDrop(int slotIndex, ItemData item, int amount, out int amountRemaining)
    {
        amountRemaining = amount;

        if (_inventorySystem == null)
        {
            return false;
        }

        return _inventorySystem.TryAddToSlot(slotIndex, item, amount, out amountRemaining);
    }

    public void HandleSlotDropOutside(int slotIndex)
    {
        if (_inventorySystem == null)
        {
            return;
        }

        _inventorySystem.DropItemFromSlot(slotIndex);
    }

    public void EndSlotDrag()
    {
        _dragSourceIndex = -1;
        RefreshUI();
    }
}
