using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        CreateDefaultHotbarItem(0, ResolveItemAsset(new[] { "Shovel_Item", "Hoe_Item" }, new[] { "tool_shovel", "tool_hoe" }), "tool_shovel", "Shovel", new Color(0.58f, 0.39f, 0.21f, 1f));
        CreateDefaultHotbarItem(1, ResolveItemAsset(new[] { "WateringCan_Item" }, new[] { "tool_watering_can" }), "tool_watering_can", "Watering Can", new Color(0.23f, 0.49f, 0.78f, 1f));
        CreateDefaultHotbarItem(2, ResolveItemAsset(new[] { "Beetroot_Item", "Tomato_Item" }, new[] { "crop_beetroot", "crop_tomato" }), "crop_beetroot", "Beetroot", new Color(0.62f, 0.12f, 0.24f, 1f));
        CreateDefaultHotbarItem(3, ResolveItemAsset(new[] { "Carrot_Item" }, new[] { "crop_carrot", "carrot" }), "crop_carrot", "Carrot", new Color(0.94f, 0.5f, 0.12f, 1f));
        CreateDefaultHotbarItem(4, ResolveItemAsset(new[] { "Potato_Item", "Corn_Item" }, new[] { "crop_potato", "crop_corn" }), "crop_potato", "Potato", new Color(0.73f, 0.59f, 0.34f, 1f));
        CreateDefaultHotbarItem(5, ResolveItemAsset(new[] { "Wheat_Item" }, new[] { "crop_wheat" }), "crop_wheat", "Wheat", new Color(0.88f, 0.74f, 0.28f, 1f));
        CreateDefaultHotbarItem(6, ResolveItemAsset(new[] { "Axe_Item", "Sickle_Item" }, new[] { "tool_axe", "tool_sickle" }), "tool_axe", "Axe", new Color(0.72f, 0.72f, 0.74f, 1f));

        for (int i = DefaultHotbarFillCount; i < _inventorySystem.HotbarSize; i++)
        {
            InventorySlotData slot = _inventorySystem.GetSlots()[i];

            if (slot.Item == null)
            {
                slot.Quantity = 0;
            }
        }
    }

    private void CreateDefaultHotbarItem(int slotIndex, ItemData assetItem, string fallbackItemId, string fallbackItemName, Color fallbackIconColor)
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

        ItemData item = assetItem != null
            ? assetItem
            : CreateRuntimeItem(fallbackItemId, fallbackItemName, fallbackIconColor);

        _inventorySystem.TryAddToSlot(slotIndex, item, 1, out _);
    }

    private static ItemData CreateRuntimeItem(string itemId, string itemName, Color iconColor)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.name = $"Runtime_{itemId}";
        item.ItemId = itemId;
        item.ItemName = itemName;
        item.Icon = CreateSolidIcon(iconColor);
        item.MaxStack = 1;
        return item;
    }

    private static ItemData ResolveItemAsset(string[] assetNames, string[] itemIds)
    {
#if UNITY_EDITOR
        foreach (string assetName in assetNames)
        {
            string[] candidatePaths =
            {
                $"Assets/_Core/Data/{assetName}.asset",
                $"Assets/{assetName}.asset"
            };

            foreach (string candidatePath in candidatePaths)
            {
                ItemData directAsset = AssetDatabase.LoadAssetAtPath<ItemData>(candidatePath);

                if (directAsset != null)
                {
                    return directAsset;
                }
            }

            string[] matchingGuids = AssetDatabase.FindAssets($"{assetName} t:ItemData");

            foreach (string guid in matchingGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ItemData namedAsset = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

                if (namedAsset != null)
                {
                    return namedAsset;
                }
            }
        }

        string[] allItemGuids = AssetDatabase.FindAssets("t:ItemData");

        foreach (string guid in allItemGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemAsset = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (itemAsset != null && HasMatchingItemId(itemAsset, itemIds))
            {
                return itemAsset;
            }
        }
#endif

        return null;
    }

    private static bool HasMatchingItemId(ItemData itemAsset, string[] itemIds)
    {
        foreach (string itemId in itemIds)
        {
            if (string.Equals(itemAsset.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
