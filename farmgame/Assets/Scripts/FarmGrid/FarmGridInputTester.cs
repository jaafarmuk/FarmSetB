using UnityEngine;

public class FarmGridInputTester : MonoBehaviour
{
    [SerializeField] private FarmGridManager _farmGridManager;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private HotbarController _hotbarController;
    [SerializeField] private FarmToolType _selectedTool = FarmToolType.Hoe;
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

        if (_hotbarController != null)
        {
            SyncSelectionFromHotbar();
        }
        else
        {
            HandleToolSelectionInput();
        }

        if (Input.GetKeyDown(_advanceDayKey))
        {
            _farmGridManager.AdvanceDay();
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryUseSelectedToolOnTile();
        }
    }

    private void HandleToolSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _selectedTool = FarmToolType.Hoe;
            _selectedCrop = FarmCropType.None;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _selectedTool = FarmToolType.WateringCan;
            _selectedCrop = FarmCropType.None;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _selectedCrop = FarmCropType.Tomato;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            _selectedCrop = FarmCropType.Carrot;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            _selectedCrop = FarmCropType.Wheat;
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _selectedCrop = FarmCropType.Corn;
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            _selectedTool = FarmToolType.Sickle;
            _selectedCrop = FarmCropType.None;
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
            return;
        }

        ItemData selectedItem = _hotbarController.SelectedItem;

        if (selectedItem == null)
        {
            _hasSelection = false;
            _selectedCrop = FarmCropType.None;
            return;
        }

        _hasSelection = true;

        switch (selectedItem.ItemId)
        {
            case "tool_hoe":
                _selectedTool = FarmToolType.Hoe;
                _selectedCrop = FarmCropType.None;
                break;
            case "tool_watering_can":
                _selectedTool = FarmToolType.WateringCan;
                _selectedCrop = FarmCropType.None;
                break;
            case "tool_sickle":
                _selectedTool = FarmToolType.Sickle;
                _selectedCrop = FarmCropType.None;
                break;
            case "crop_tomato":
                _selectedCrop = FarmCropType.Tomato;
                break;
            case "crop_carrot":
                _selectedCrop = FarmCropType.Carrot;
                break;
            case "crop_wheat":
                _selectedCrop = FarmCropType.Wheat;
                break;
            case "crop_corn":
                _selectedCrop = FarmCropType.Corn;
                break;
            default:
                _hasSelection = false;
                _selectedCrop = FarmCropType.None;
                break;
        }
    }
}
