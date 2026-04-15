using System;
using UnityEngine;

public class HotbarController : MonoBehaviour
{
    private const int DefaultHotbarFillCount = 7;

    [SerializeField] private InventorySystem _inventorySystem;
    [SerializeField] private int _selectedSlotIndex;

    public event Action<int> SelectedSlotChanged;

    public int SelectedSlotIndex => _selectedSlotIndex;
    public ItemData SelectedItem => TryGetItemAtSlot(_selectedSlotIndex, out ItemData item) ? item : null;

    private void Awake()
    {
        if (_inventorySystem == null)
        {
            _inventorySystem = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
        }
    }

    private void Start()
    {
        SeedDefaultHotbarItems();
        SelectSlot(0);
    }

    private void Update()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        HandleNumberKeySelection();
        HandleMouseWheelSelection();
    }

    public void SelectSlot(int slotIndex)
    {
        if (_inventorySystem == null)
        {
            return;
        }

        if (slotIndex < 0 || slotIndex >= _inventorySystem.HotbarSize)
        {
            return;
        }

        if (_selectedSlotIndex == slotIndex)
        {
            return;
        }

        _selectedSlotIndex = slotIndex;
        SelectedSlotChanged?.Invoke(_selectedSlotIndex);
    }

    private void HandleNumberKeySelection()
    {
        for (int i = 0; i < _inventorySystem.HotbarSize; i++)
        {
            KeyCode keyCode = (KeyCode)((int)KeyCode.Alpha1 + i);

            if (Input.GetKeyDown(keyCode))
            {
                SelectSlot(i);
            }
        }
    }

    private void HandleMouseWheelSelection()
    {
        float scrollAmount = Input.mouseScrollDelta.y;

        if (Mathf.Approximately(scrollAmount, 0f) || _inventorySystem.HotbarSize <= 0)
        {
            return;
        }

        int direction = scrollAmount > 0f ? -1 : 1;
        int wrappedIndex = (_selectedSlotIndex + direction + _inventorySystem.HotbarSize) % _inventorySystem.HotbarSize;
        SelectSlot(wrappedIndex);
    }

    private bool TryGetItemAtSlot(int slotIndex, out ItemData item)
    {
        item = null;

        if (_inventorySystem == null || slotIndex < 0 || slotIndex >= _inventorySystem.HotbarSize)
        {
            return false;
        }

        InventorySlotData slot = _inventorySystem.GetSlots()[slotIndex];

        if (slot.Item == null || slot.Quantity <= 0)
        {
            return false;
        }

        item = slot.Item;
        return true;
    }

    private void SeedDefaultHotbarItems()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        CreateDefaultHotbarItem(0, "tool_hoe", "Hoe", new Color(0.58f, 0.39f, 0.21f, 1f));
        CreateDefaultHotbarItem(1, "tool_watering_can", "Watering Can", new Color(0.23f, 0.49f, 0.78f, 1f));
        CreateDefaultHotbarItem(2, "crop_tomato", "Tomato", new Color(0.82f, 0.18f, 0.2f, 1f));
        CreateDefaultHotbarItem(3, "crop_carrot", "Carrot", new Color(0.94f, 0.5f, 0.12f, 1f));
        CreateDefaultHotbarItem(4, "crop_wheat", "Wheat", new Color(0.88f, 0.74f, 0.28f, 1f));
        CreateDefaultHotbarItem(5, "crop_corn", "Corn", new Color(0.95f, 0.85f, 0.21f, 1f));
        CreateDefaultHotbarItem(6, "tool_sickle", "Sickle", new Color(0.72f, 0.72f, 0.74f, 1f));

        for (int i = DefaultHotbarFillCount; i < _inventorySystem.HotbarSize; i++)
        {
            InventorySlotData slot = _inventorySystem.GetSlots()[i];

            if (slot.Item == null)
            {
                slot.Quantity = 0;
            }
        }
    }

    private void CreateDefaultHotbarItem(int slotIndex, string itemId, string itemName, Color iconColor)
    {
        if (slotIndex < 0 || slotIndex >= _inventorySystem.HotbarSize)
        {
            return;
        }

        InventorySlotData slot = _inventorySystem.GetSlots()[slotIndex];

        if (slot.Item != null)
        {
            return;
        }

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.ItemId = itemId;
        item.ItemName = itemName;
        item.Icon = CreateSolidIcon(iconColor);
        item.MaxStack = 1;

        _inventorySystem.TryAddToSlot(slotIndex, item, 1, out _);
    }

    private static Sprite CreateSolidIcon(Color color)
    {
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[32 * 32];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
    }
}
