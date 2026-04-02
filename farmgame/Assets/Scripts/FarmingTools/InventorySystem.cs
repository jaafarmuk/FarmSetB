using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [SerializeField] private int _inventorySize = 20;

    private List<InventorySlotData> _slots;

    public event Action InventoryChanged;

    private void Awake()
    {
        _slots = new List<InventorySlotData>(_inventorySize);

        for (int i = 0; i < _inventorySize; i++)
        {
            _slots.Add(new InventorySlotData());
        }
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        bool changed = false;

        foreach (InventorySlotData slot in _slots)
        {
            if (slot.Item == item && slot.Quantity < item.MaxStack)
            {
                int spaceLeft = item.MaxStack - slot.Quantity;
                int addAmount = Mathf.Min(spaceLeft, amount);

                if (addAmount <= 0)
                {
                    continue;
                }

                slot.Quantity += addAmount;
                amount -= addAmount;
                changed = true;

                if (amount <= 0)
                {
                    NotifyInventoryChanged();
                    return true;
                }
            }
        }

        foreach (InventorySlotData slot in _slots)
        {
            if (slot.Item == null)
            {
                int addAmount = Mathf.Min(item.MaxStack, amount);

                if (addAmount <= 0)
                {
                    continue;
                }

                slot.Item = item;
                slot.Quantity = addAmount;
                amount -= addAmount;
                changed = true;

                if (amount <= 0)
                {
                    NotifyInventoryChanged();
                    return true;
                }
            }
        }

        if (changed)
        {
            NotifyInventoryChanged();
        }

        return false;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        bool changed = false;

        foreach (InventorySlotData slot in _slots)
        {
            if (slot.Item == item)
            {
                int removeAmount = Mathf.Min(slot.Quantity, amount);

                if (removeAmount <= 0)
                {
                    continue;
                }

                slot.Quantity -= removeAmount;
                amount -= removeAmount;
                changed = true;

                if (slot.Quantity <= 0)
                {
                    slot.Item = null;
                    slot.Quantity = 0;
                }

                if (amount <= 0)
                {
                    NotifyInventoryChanged();
                    return true;
                }
            }
        }

        if (changed)
        {
            NotifyInventoryChanged();
        }

        return false;
    }

    public bool TryAddToSlot(int slotIndex, ItemData item, int amount, out int amountRemaining)
    {
        amountRemaining = amount;

        if (!IsValidSlotIndex(slotIndex) || item == null || amount <= 0)
        {
            return false;
        }

        InventorySlotData slot = _slots[slotIndex];

        if (!SlotHasItem(slot))
        {
            int amountToPlace = Mathf.Min(amount, item.MaxStack);

            if (amountToPlace <= 0)
            {
                return false;
            }

            slot.Item = item;
            slot.Quantity = amountToPlace;
            amountRemaining = amount - amountToPlace;
            NotifyInventoryChanged();
            return true;
        }

        if (slot.Item != item)
        {
            return false;
        }

        int spaceLeft = item.MaxStack - slot.Quantity;

        if (spaceLeft <= 0)
        {
            return false;
        }

        int amountToAdd = Mathf.Min(spaceLeft, amount);
        slot.Quantity += amountToAdd;
        amountRemaining = amount - amountToAdd;
        NotifyInventoryChanged();
        return true;
    }

    public bool TryAddOneToSlot(int slotIndex, ItemData item)
    {
        return TryAddToSlot(slotIndex, item, 1, out _);
    }

    public bool DropItemFromSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return false;
        }

        InventorySlotData slot = _slots[slotIndex];

        if (!SlotHasItem(slot))
        {
            return false;
        }

        slot.Item = null;
        slot.Quantity = 0;
        NotifyInventoryChanged();
        return true;
    }

    public void MoveOrSwapItem(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex))
        {
            return;
        }

        if (fromIndex == toIndex)
        {
            return;
        }

        InventorySlotData fromSlot = _slots[fromIndex];
        InventorySlotData toSlot = _slots[toIndex];

        if (!SlotHasItem(fromSlot))
        {
            return;
        }

        if (!SlotHasItem(toSlot))
        {
            toSlot.Item = fromSlot.Item;
            toSlot.Quantity = fromSlot.Quantity;

            fromSlot.Item = null;
            fromSlot.Quantity = 0;
        }
        else if (fromSlot.Item == toSlot.Item)
        {
            int maxStack = fromSlot.Item.MaxStack;
            int combinedQuantity = fromSlot.Quantity + toSlot.Quantity;

            if (combinedQuantity <= maxStack)
            {
                toSlot.Quantity = combinedQuantity;
                fromSlot.Item = null;
                fromSlot.Quantity = 0;
            }
            else
            {
                toSlot.Quantity = maxStack;
                fromSlot.Quantity = combinedQuantity - maxStack;
            }
        }
        else
        {
            ItemData cachedItem = toSlot.Item;
            int cachedQuantity = toSlot.Quantity;

            toSlot.Item = fromSlot.Item;
            toSlot.Quantity = fromSlot.Quantity;

            fromSlot.Item = cachedItem;
            fromSlot.Quantity = cachedQuantity;
        }

        NotifyInventoryChanged();
    }

    public List<InventorySlotData> GetSlots()
    {
        return _slots;
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < _slots.Count;
    }

    private bool SlotHasItem(InventorySlotData slot)
    {
        return slot.Item != null && slot.Quantity > 0;
    }

    private void NotifyInventoryChanged()
    {
        PrintInventory();
        InventoryChanged?.Invoke();
    }

    private void PrintInventory()
    {
        string result = "Inventory: ";

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Item != null)
            {
                result += $"[{i}: {_slots[i].Item.ItemName} x{_slots[i].Quantity}] ";
            }
        }

        Debug.Log(result);
    }
}
