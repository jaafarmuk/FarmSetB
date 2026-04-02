using System;
using UnityEngine;

public class HeldItemSystem : MonoBehaviour
{
    private ItemData _heldItem;
    private int _heldQuantity;

    public event Action HeldItemChanged;

    public ItemData HeldItem => _heldItem;
    public int HeldQuantity => _heldQuantity;
    public bool HasItem => _heldItem != null && _heldQuantity > 0;

    public void SetHeldItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            ClearHeldItem();
            return;
        }

        _heldItem = item;
        _heldQuantity = Mathf.Min(quantity, item.MaxStack);
        NotifyHeldItemChanged();
    }

    public void ClearHeldItem()
    {
        _heldItem = null;
        _heldQuantity = 0;
        NotifyHeldItemChanged();
    }

    public bool AddToHeldItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        if (!HasItem)
        {
            SetHeldItem(item, amount);
            return true;
        }

        if (_heldItem != item)
        {
            return false;
        }

        int newQuantity = Mathf.Min(_heldQuantity + amount, item.MaxStack);

        if (newQuantity == _heldQuantity)
        {
            return false;
        }

        _heldQuantity = newQuantity;
        NotifyHeldItemChanged();
        return true;
    }

    public int RemoveAmount(int amount)
    {
        if (!HasItem || amount <= 0)
        {
            return 0;
        }

        int removedAmount = Mathf.Min(amount, _heldQuantity);
        _heldQuantity -= removedAmount;

        if (_heldQuantity <= 0)
        {
            _heldItem = null;
            _heldQuantity = 0;
        }

        NotifyHeldItemChanged();
        return removedAmount;
    }

    public bool PlaceOne()
    {
        return RemoveAmount(1) > 0;
    }

    public int PlaceFullStack()
    {
        return RemoveAmount(_heldQuantity);
    }

    private void NotifyHeldItemChanged()
    {
        HeldItemChanged?.Invoke();
    }
}
