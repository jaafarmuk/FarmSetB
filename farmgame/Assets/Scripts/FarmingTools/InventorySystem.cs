using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [SerializeField] private int _hotbarSize = 8;
    [SerializeField] private int _inventorySize = 20;
    [SerializeField] private ItemData[] _startingHotbarItems;
    [SerializeField] private int _selectedHotbarSlotIndex;

    private List<InventorySlotData> _slots;

    public event Action InventoryChanged;
    public event Action<int> HotbarSelectionChanged;

    public int HotbarSize => Mathf.Max(1, _hotbarSize);
    public int InventorySize => Mathf.Max(0, _inventorySize);
    public int InventoryStartIndex => HotbarSize;
    public int TotalSlotCount => HotbarSize + InventorySize;
    public int SelectedHotbarSlotIndex => Mathf.Clamp(_selectedHotbarSlotIndex, 0, HotbarSize - 1);
    public ItemData SelectedHotbarItem => TryGetSelectedHotbarItem(out ItemData item) ? item : null;
    public IReadOnlyList<InventorySlotData> Slots => _slots;

    private void Awake()
    {
        InitializeSlots();
        SeedStartingHotbarItems();
        SelectHotbarSlot(_selectedHotbarSlotIndex, true);
    }

    public InventorySlotData GetSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return null;
        }

        return _slots[slotIndex];
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (!IsValidItemRequest(item, amount))
        {
            return false;
        }

        int amountRemaining = AddToRange(item, amount, InventoryStartIndex, TotalSlotCount);

        if (amountRemaining > 0)
        {
            amountRemaining = AddToRange(item, amountRemaining, 0, HotbarSize);
        }

        if (amountRemaining == amount)
        {
            return false;
        }

        NotifyInventoryChanged();
        return amountRemaining == 0;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        if (!IsValidItemRequest(item, amount))
        {
            return false;
        }

        int amountRemaining = RemoveFromRange(item, amount, InventoryStartIndex, TotalSlotCount);
        amountRemaining = RemoveFromRange(item, amountRemaining, 0, HotbarSize);

        if (amountRemaining == amount)
        {
            return false;
        }

        NotifyInventoryChanged();
        return amountRemaining == 0;
    }

    public bool TryAddToSlot(int slotIndex, ItemData item, int amount, out int amountRemaining)
    {
        amountRemaining = amount;

        if (!IsValidSlotIndex(slotIndex) || !IsValidItemRequest(item, amount))
        {
            return false;
        }

        InventorySlotData slot = _slots[slotIndex];

        if (!HasItem(slot))
        {
            int amountToPlace = Mathf.Min(item.MaxStack, amount);
            slot.Item = item;
            slot.Quantity = amountToPlace;
            amountRemaining = amount - amountToPlace;
            NotifyInventoryChanged();
            return true;
        }

        if (slot.Item != item || slot.Quantity >= item.MaxStack)
        {
            return false;
        }

        int amountToAdd = Mathf.Min(item.MaxStack - slot.Quantity, amount);
        slot.Quantity += amountToAdd;
        amountRemaining = amount - amountToAdd;
        NotifyInventoryChanged();
        return true;
    }

    public bool ClearSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex) || !HasItem(_slots[slotIndex]))
        {
            return false;
        }

        _slots[slotIndex].Item = null;
        _slots[slotIndex].Quantity = 0;
        NotifyInventoryChanged();
        return true;
    }

    public bool MoveOrSwapItem(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex) || fromIndex == toIndex)
        {
            return false;
        }

        InventorySlotData fromSlot = _slots[fromIndex];
        InventorySlotData toSlot = _slots[toIndex];

        if (!HasItem(fromSlot))
        {
            return false;
        }

        if (!HasItem(toSlot))
        {
            toSlot.Item = fromSlot.Item;
            toSlot.Quantity = fromSlot.Quantity;
            fromSlot.Item = null;
            fromSlot.Quantity = 0;
            NotifyInventoryChanged();
            return true;
        }

        if (fromSlot.Item == toSlot.Item)
        {
            int combinedQuantity = fromSlot.Quantity + toSlot.Quantity;

            if (combinedQuantity <= fromSlot.Item.MaxStack)
            {
                toSlot.Quantity = combinedQuantity;
                fromSlot.Item = null;
                fromSlot.Quantity = 0;
            }
            else
            {
                toSlot.Quantity = fromSlot.Item.MaxStack;
                fromSlot.Quantity = combinedQuantity - fromSlot.Item.MaxStack;
            }

            NotifyInventoryChanged();
            return true;
        }

        ItemData cachedItem = toSlot.Item;
        int cachedQuantity = toSlot.Quantity;

        toSlot.Item = fromSlot.Item;
        toSlot.Quantity = fromSlot.Quantity;
        fromSlot.Item = cachedItem;
        fromSlot.Quantity = cachedQuantity;

        NotifyInventoryChanged();
        return true;
    }

    public bool SelectHotbarSlot(int slotIndex)
    {
        return SelectHotbarSlot(slotIndex, false);
    }

    public bool TryGetSelectedHotbarItem(out ItemData item)
    {
        item = null;

        InventorySlotData slot = GetSlot(SelectedHotbarSlotIndex);

        if (!HasItem(slot))
        {
            return false;
        }

        item = slot.Item;
        return true;
    }

    public void SelectNextHotbarSlot(int direction)
    {
        if (HotbarSize <= 0 || direction == 0)
        {
            return;
        }

        int nextIndex = (SelectedHotbarSlotIndex + direction + HotbarSize) % HotbarSize;
        SelectHotbarSlot(nextIndex);
    }

    private void InitializeSlots()
    {
        _slots = new List<InventorySlotData>(TotalSlotCount);

        for (int i = 0; i < TotalSlotCount; i++)
        {
            _slots.Add(new InventorySlotData());
        }
    }

    private void SeedStartingHotbarItems()
    {
        if (_startingHotbarItems == null)
        {
            return;
        }

        int count = Mathf.Min(_startingHotbarItems.Length, HotbarSize);

        for (int i = 0; i < count; i++)
        {
            ItemData item = _startingHotbarItems[i];

            if (item == null)
            {
                continue;
            }

            InventorySlotData slot = _slots[i];
            slot.Item = item;
            slot.Quantity = 1;
        }
    }

    private int AddToRange(ItemData item, int amount, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            InventorySlotData slot = _slots[i];

            if (slot.Item != item || slot.Quantity >= item.MaxStack)
            {
                continue;
            }

            int amountToAdd = Mathf.Min(item.MaxStack - slot.Quantity, amount);
            slot.Quantity += amountToAdd;
            amount -= amountToAdd;

            if (amount == 0)
            {
                return 0;
            }
        }

        for (int i = startIndex; i < endIndex; i++)
        {
            InventorySlotData slot = _slots[i];

            if (HasItem(slot))
            {
                continue;
            }

            int amountToAdd = Mathf.Min(item.MaxStack, amount);
            slot.Item = item;
            slot.Quantity = amountToAdd;
            amount -= amountToAdd;

            if (amount == 0)
            {
                return 0;
            }
        }

        return amount;
    }

    private int RemoveFromRange(ItemData item, int amount, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            InventorySlotData slot = _slots[i];

            if (slot.Item != item)
            {
                continue;
            }

            int amountToRemove = Mathf.Min(slot.Quantity, amount);
            slot.Quantity -= amountToRemove;
            amount -= amountToRemove;

            if (slot.Quantity == 0)
            {
                slot.Item = null;
            }

            if (amount == 0)
            {
                return 0;
            }
        }

        return amount;
    }

    private bool SelectHotbarSlot(int slotIndex, bool forceNotify)
    {
        if (slotIndex < 0 || slotIndex >= HotbarSize)
        {
            return false;
        }

        if (!forceNotify && _selectedHotbarSlotIndex == slotIndex)
        {
            return false;
        }

        _selectedHotbarSlotIndex = slotIndex;
        HotbarSelectionChanged?.Invoke(_selectedHotbarSlotIndex);
        return true;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return _slots != null && slotIndex >= 0 && slotIndex < _slots.Count;
    }

    private static bool IsValidItemRequest(ItemData item, int amount)
    {
        return item != null && amount > 0;
    }

    private static bool HasItem(InventorySlotData slot)
    {
        return slot != null && slot.Item != null && slot.Quantity > 0;
    }

    private void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }
}
