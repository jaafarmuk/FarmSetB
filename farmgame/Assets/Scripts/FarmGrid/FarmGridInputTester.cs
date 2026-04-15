using UnityEngine;
using UnityEngine.EventSystems;

public class FarmGridInputTester : MonoBehaviour
{
    [SerializeField] private FarmGridManager _farmGridManager;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private HotbarController _hotbarController;
    [SerializeField] private FarmToolType _selectedTool = FarmToolType.Shovel;
    [SerializeField] private FarmCropType _selectedCrop = FarmCropType.None;
    [SerializeField] private KeyCode _advanceDayKey = KeyCode.N;

    private bool _hasSelection = true;

    public FarmToolType SelectedTool => _selectedTool;
    public FarmCropType SelectedCrop => _selectedCrop;
    public bool IsToolMode => _selectedCrop == FarmCropType.None;

    private void Awake()
    {
        if (_farmGridManager == null)
        {
            _farmGridManager = GetComponent<FarmGridManager>();
        }

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        if (_hotbarController == null)
        {
            _hotbarController = Object.FindAnyObjectByType<HotbarController>();
        }

        SyncSelectionFromHotbar();
    }

    private void Update()
    {
        if (_farmGridManager == null || _worldCamera == null)
        {
            return;
        }

        EnsureHotbarControllerReference();
        SyncSelectionFromHotbar();

        if (Input.GetKeyDown(_advanceDayKey))
        {
            _farmGridManager.AdvanceDay();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryUseSelectedToolOnTile();
        }
    }

    private void TryUseSelectedToolOnTile()
    {
        if (!_hasSelection)
        {
            return;
        }

        if (TryGetCellUnderMouse(out Vector2Int coordinates))
        {
            if (IsToolMode)
            {
                _farmGridManager.TryApplyTool(coordinates, _selectedTool);
            }
            else
            {
                _farmGridManager.TryPlantCrop(coordinates, _selectedCrop);
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

    private void SyncSelectionFromHotbar()
    {
        if (_hotbarController == null)
        {
            _hasSelection = false;
            _selectedCrop = FarmCropType.None;
            return;
        }

        ItemData selectedItem = _hotbarController.SelectedItem;

        if (selectedItem == null)
        {
            _hasSelection = false;
            _selectedCrop = FarmCropType.None;
            return;
        }

        ApplySelectionFromItem(selectedItem);
    }

    private void EnsureHotbarControllerReference()
    {
        if (_hotbarController == null)
        {
            _hotbarController = Object.FindAnyObjectByType<HotbarController>();
        }
    }

    private void ApplySelectionFromItem(ItemData selectedItem)
    {
        _hasSelection = true;

        switch (selectedItem.ItemId)
        {
            case "tool_shovel":
            case "tool_hoe":
                _selectedTool = FarmToolType.Shovel;
                _selectedCrop = FarmCropType.None;
                break;
            case "tool_watering_can":
                _selectedTool = FarmToolType.WateringCan;
                _selectedCrop = FarmCropType.None;
                break;
            case "tool_axe":
            case "tool_sickle":
                _selectedTool = FarmToolType.Axe;
                _selectedCrop = FarmCropType.None;
                break;
            case "crop_beetroot":
            case "crop_tomato":
                _selectedCrop = FarmCropType.Beetroot;
                break;
            case "crop_carrot":
                _selectedCrop = FarmCropType.Carrot;
                break;
            case "crop_potato":
            case "crop_corn":
                _selectedCrop = FarmCropType.Potato;
                break;
            case "crop_wheat":
                _selectedCrop = FarmCropType.Wheat;
                break;
            default:
                _hasSelection = false;
                _selectedCrop = FarmCropType.None;
                break;
        }
    }
}
