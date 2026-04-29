using System;
using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [SerializeField] private int _maxStamina = 100;
    [SerializeField] private int _currentStamina = 100;

    public event Action StaminaChanged;

    public int MaxStamina => _maxStamina;
    public int CurrentStamina => _currentStamina;

    private void Awake()
    {
        _maxStamina = Mathf.Max(1, _maxStamina);
        _currentStamina = Mathf.Clamp(_currentStamina, 0, _maxStamina);
        NotifyStaminaChanged();
    }

    public bool CanAfford(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        return _currentStamina >= cost;
    }

    public bool TrySpend(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        if (!CanAfford(cost))
        {
            return false;
        }

        _currentStamina -= cost;
        NotifyStaminaChanged();
        return true;
    }

    public void RestoreToMax()
    {
        if (_currentStamina == _maxStamina)
        {
            return;
        }

        _currentStamina = _maxStamina;
        NotifyStaminaChanged();
    }

    private void NotifyStaminaChanged()
    {
        StaminaChanged?.Invoke();
    }
}
