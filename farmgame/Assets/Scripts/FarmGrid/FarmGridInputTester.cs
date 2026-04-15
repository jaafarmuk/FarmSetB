using UnityEngine;
using UnityEngine.EventSystems;

public class FarmGridInputTester : MonoBehaviour
{
    [SerializeField] private FarmGridManager _farmGridManager;
    [SerializeField] private InventorySystem _inventorySystem;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private KeyCode _advanceDayKey = KeyCode.N;

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

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (_farmGridManager == null || _inventorySystem == null || _worldCamera == null)
        {
            return;
        }

        if (Input.GetKeyDown(_advanceDayKey))
        {
            _farmGridManager.AdvanceDay();
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
            _farmGridManager.TryApplyTool(coordinates, toolType);
            return;
        }

        if (TryGetCropType(selectedItem, out FarmCropType cropType))
        {
            _farmGridManager.TryPlantCrop(coordinates, cropType);
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
