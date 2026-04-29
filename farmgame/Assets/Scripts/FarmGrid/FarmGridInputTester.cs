using UnityEngine;
using UnityEngine.EventSystems;

public class FarmGridInputTester : MonoBehaviour
{
    [SerializeField] private FarmGridManager _farmGridManager;
    [SerializeField] private InventorySystem _inventorySystem;
    [SerializeField] private StaminaSystem _staminaSystem;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private KeyCode _advanceDayKey = KeyCode.N;

    [Header("Stamina Costs")]
    [SerializeField] private int _hoeCost = 10;
    [SerializeField] private int _wateringCanCost = 5;
    [SerializeField] private int _plantingCost = 5;
    [SerializeField] private int _harvestCost = 10;
    [SerializeField] private string _notEnoughStaminaMessage = "Not enough stamina.";

    private void Awake()
    {
        if (_farmGridManager == null)
        {
            _farmGridManager = GetComponent<FarmGridManager>();
        }

        if (_inventorySystem == null)
        {
            _inventorySystem = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
        }

        if (_staminaSystem == null)
        {
            _staminaSystem = GetComponent<StaminaSystem>();
        }

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (_farmGridManager == null || _inventorySystem == null || _staminaSystem == null || _worldCamera == null)
        {
            return;
        }

        if (Input.GetKeyDown(_advanceDayKey))
        {
            _farmGridManager.AdvanceDay();
            _staminaSystem.RestoreToMax();
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (!TryGetCellUnderMouse(out Vector2Int coordinates))
        {
            return;
        }

        ItemData selectedItem = _inventorySystem.SelectedHotbarItem;

        if (selectedItem == null)
        {
            return;
        }

        if (TryGetToolType(selectedItem, out FarmToolType toolType))
        {
            TryApplyToolAction(coordinates, toolType);
            return;
        }

        if (TryGetCropType(selectedItem, out FarmCropType cropType))
        {
            TryPlantCropAction(coordinates, cropType);
        }
    }

    private void TryApplyToolAction(Vector2Int coordinates, FarmToolType toolType)
    {
        int staminaCost = GetToolCost(toolType);

        if (!_staminaSystem.CanAfford(staminaCost))
        {
            ShowNotEnoughStaminaFeedback(staminaCost);
            return;
        }

        if (_farmGridManager.TryApplyTool(coordinates, toolType))
        {
            _staminaSystem.TrySpend(staminaCost);
        }
    }

    private void TryPlantCropAction(Vector2Int coordinates, FarmCropType cropType)
    {
        if (!_staminaSystem.CanAfford(_plantingCost))
        {
            ShowNotEnoughStaminaFeedback(_plantingCost);
            return;
        }

        if (_farmGridManager.TryPlantCrop(coordinates, cropType))
        {
            int selectedSlotIndex = _inventorySystem.SelectedHotbarSlotIndex;
            InventorySlotData selectedSlotBeforeConsumption = _inventorySystem.GetSlot(selectedSlotIndex);
            string selectedItemIdBeforeConsumption = selectedSlotBeforeConsumption != null && selectedSlotBeforeConsumption.Item != null
                ? selectedSlotBeforeConsumption.Item.ItemId
                : "null";
            int quantityBeforeConsumption = selectedSlotBeforeConsumption != null ? selectedSlotBeforeConsumption.Quantity : 0;

            Debug.Log($"Before seed consumption -> HotbarIndex: {selectedSlotIndex}, ItemId: {selectedItemIdBeforeConsumption}, QuantityBefore: {quantityBeforeConsumption}");

            if (_inventorySystem.TryConsumeSelectedHotbarItem(1))
            {
                InventorySlotData selectedSlotAfterConsumption = _inventorySystem.GetSlot(selectedSlotIndex);
                string selectedItemIdAfterConsumption = selectedSlotAfterConsumption != null && selectedSlotAfterConsumption.Item != null
                    ? selectedSlotAfterConsumption.Item.ItemId
                    : "null";
                int quantityAfterConsumption = selectedSlotAfterConsumption != null ? selectedSlotAfterConsumption.Quantity : 0;

                Debug.Log($"After seed consumption -> HotbarIndex: {selectedSlotIndex}, ItemId: {selectedItemIdAfterConsumption}, QuantityAfter: {quantityAfterConsumption}");
                _staminaSystem.TrySpend(_plantingCost);
            }
        }
    }

    private bool TryGetCellUnderMouse(out Vector2Int coordinates)
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldPosition = _worldCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Mathf.Abs(_worldCamera.transform.position.z)));
        worldPosition.z = 0f;
        return _farmGridManager.TryWorldToCell(worldPosition, out coordinates);
    }

    private static bool TryGetToolType(ItemData item, out FarmToolType toolType)
    {
        toolType = default;

        if (item == null)
        {
            return false;
        }

        switch (item.ItemId)
        {
            case "tool_shovel":
            case "tool_hoe":
                toolType = FarmToolType.Shovel;
                return true;
            case "tool_watering_can":
                toolType = FarmToolType.WateringCan;
                return true;
            case "tool_axe":
            case "tool_sickle":
                toolType = FarmToolType.Axe;
                return true;
            default:
                return false;
        }
    }

    private int GetToolCost(FarmToolType toolType)
    {
        return toolType switch
        {
            FarmToolType.Shovel => _hoeCost,
            FarmToolType.WateringCan => _wateringCanCost,
            FarmToolType.Axe => _harvestCost,
            _ => 0
        };
    }

    private void ShowNotEnoughStaminaFeedback(int requiredStamina)
    {
        Debug.Log($"{_notEnoughStaminaMessage} Required: {requiredStamina}, Current: {_staminaSystem.CurrentStamina}.");
    }

    private static bool TryGetCropType(ItemData item, out FarmCropType cropType)
    {
        cropType = FarmCropType.None;

        if (item == null)
        {
            return false;
        }

        switch (item.ItemId)
        {
            case "crop_beetroot":
            case "crop_tomato":
                cropType = FarmCropType.Beetroot;
                return true;
            case "crop_carrot":
                cropType = FarmCropType.Carrot;
                return true;
            case "crop_potato":
            case "crop_corn":
                cropType = FarmCropType.Potato;
                return true;
            case "crop_wheat":
                cropType = FarmCropType.Wheat;
                return true;
            default:
                return false;
        }
    }
}
