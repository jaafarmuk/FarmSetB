using UnityEngine;

public class FarmGridManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int _width = 20;
    [SerializeField] private int _height = 20;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private float _cellPadding = 0.08f;

    [Header("Visuals")]
    [SerializeField] private Color _normalSoilColor = new Color(0.47f, 0.33f, 0.21f, 1f);
    [SerializeField] private Color _tilledSoilColor = new Color(0.36f, 0.24f, 0.14f, 1f);
    [SerializeField] private Color _wateredSoilColor = new Color(0.24f, 0.39f, 0.54f, 1f);
    [SerializeField] private float _cropSpriteScale = 0.45f;
    [SerializeField] private int _maxGrowthStage = 4;
    [SerializeField] private FarmCropDefinition[] _cropDefinitions;

    [Header("Dependencies")]
    [SerializeField] private InventorySystem _inventorySystem;

    private FarmGridCellData[,] _cells;
    private SpriteRenderer[,] _cellRenderers;
    private SpriteRenderer[,] _cropRenderers;
    private Transform _visualRoot;

    private static Sprite _cellSprite;
    private int _currentDay = 1;

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;
    public int CurrentDay => _currentDay;

    private void Awake()
    {
        if (_inventorySystem == null)
        {
            _inventorySystem = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
        }

        BuildGrid();
    }

    public bool TryGetCell(Vector2Int coordinates, out FarmGridCellData cell)
    {
        cell = null;

        if (!IsWithinBounds(coordinates))
        {
            return false;
        }

        cell = _cells[coordinates.x, coordinates.y];
        return true;
    }

    public bool TryGetCellState(Vector2Int coordinates, out FarmTileState state)
    {
        state = FarmTileState.NormalSoil;

        if (!TryGetCell(coordinates, out FarmGridCellData cell))
        {
            return false;
        }

        state = cell.State;
        return true;
    }

    public bool SetCellState(Vector2Int coordinates, FarmTileState newState)
    {
        if (!TryGetCell(coordinates, out FarmGridCellData cell))
        {
            return false;
        }

        cell.State = newState;
        RefreshCellVisual(coordinates);
        return true;
    }

    public bool TryPlantCrop(Vector2Int coordinates, FarmCropType cropType)
    {
        if (cropType == FarmCropType.None || !TryGetCell(coordinates, out FarmGridCellData cell))
        {
            return false;
        }

        if (cell.CropType != FarmCropType.None)
        {
            return false;
        }

        if (cell.State != FarmTileState.TilledSoil && cell.State != FarmTileState.WateredSoil)
        {
            return false;
        }

        cell.CropType = cropType;
        cell.GrowthStage = 0;
        cell.WateredDaysSinceLastGrowth = 0;
        RefreshCropVisual(coordinates);
        return true;
    }

    public bool TryApplyTool(Vector2Int coordinates, FarmToolType toolType)
    {
        if (!TryGetCell(coordinates, out FarmGridCellData cell))
        {
            return false;
        }

        if (toolType == FarmToolType.Axe)
        {
            return TryHarvestCrop(coordinates);
        }

        FarmTileState? nextState = toolType switch
        {
            FarmToolType.Shovel when cell.State == FarmTileState.NormalSoil => FarmTileState.TilledSoil,
            FarmToolType.WateringCan when cell.State == FarmTileState.TilledSoil => FarmTileState.WateredSoil,
            _ => null
        };

        if (!nextState.HasValue)
        {
            return false;
        }

        return SetCellState(coordinates, nextState.Value);
    }

    public bool TryHarvestCrop(Vector2Int coordinates)
    {
        if (!TryGetCell(coordinates, out FarmGridCellData cell))
        {
            return false;
        }

        if (cell.CropType == FarmCropType.None || cell.GrowthStage < _maxGrowthStage)
        {
            return false;
        }

        if (_inventorySystem == null)
        {
            _inventorySystem = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
        }

        FarmCropDefinition cropDefinition = GetCropDefinition(cell.CropType);

        if (_inventorySystem == null || cropDefinition == null || cropDefinition.HarvestItem == null)
        {
            return false;
        }

        if (!_inventorySystem.AddItem(cropDefinition.HarvestItem, 1))
        {
            return false;
        }

        cell.CropType = FarmCropType.None;
        cell.GrowthStage = 0;
        cell.WateredDaysSinceLastGrowth = 0;
        cell.State = FarmTileState.TilledSoil;

        RefreshCellVisual(coordinates);
        RefreshCropVisual(coordinates);
        return true;
    }

    public void AdvanceDay()
    {
        _currentDay++;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                FarmGridCellData cell = _cells[x, y];
                Vector2Int coordinates = new Vector2Int(x, y);

                if (cell.CropType != FarmCropType.None &&
                    cell.State == FarmTileState.WateredSoil &&
                    cell.GrowthStage < _maxGrowthStage)
                {
                    FarmCropDefinition cropDefinition = GetCropDefinition(cell.CropType);
                    int daysPerGrowthStage = cropDefinition != null ? Mathf.Max(1, cropDefinition.DaysPerGrowthStage) : 1;
                    cell.WateredDaysSinceLastGrowth++;

                    if (cell.WateredDaysSinceLastGrowth >= daysPerGrowthStage)
                    {
                        cell.GrowthStage++;
                        cell.WateredDaysSinceLastGrowth = 0;
                    }
                }

                if (cell.State == FarmTileState.WateredSoil)
                {
                    cell.State = FarmTileState.TilledSoil;
                }

                RefreshCellVisual(coordinates);
                RefreshCropVisual(coordinates);
            }
        }
    }

    public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int coordinates)
    {
        Vector2 bottomLeft = GetBottomLeftWorldPosition();
        float gridWidth = _width * _cellSize;
        float gridHeight = _height * _cellSize;

        coordinates = default;

        if (worldPosition.x < bottomLeft.x || worldPosition.y < bottomLeft.y)
        {
            return false;
        }

        if (worldPosition.x >= bottomLeft.x + gridWidth || worldPosition.y >= bottomLeft.y + gridHeight)
        {
            return false;
        }

        int x = Mathf.FloorToInt((worldPosition.x - bottomLeft.x) / _cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - bottomLeft.y) / _cellSize);
        coordinates = new Vector2Int(x, y);
        return IsWithinBounds(coordinates);
    }

    public Vector3 GetCellCenterWorldPosition(Vector2Int coordinates)
    {
        Vector2 bottomLeft = GetBottomLeftWorldPosition();
        return new Vector3(
            bottomLeft.x + ((coordinates.x + 0.5f) * _cellSize),
            bottomLeft.y + ((coordinates.y + 0.5f) * _cellSize),
            0f);
    }

    private void BuildGrid()
    {
        ClearExistingVisuals();

        _cells = new FarmGridCellData[_width, _height];
        _cellRenderers = new SpriteRenderer[_width, _height];
        _cropRenderers = new SpriteRenderer[_width, _height];
        _visualRoot = new GameObject("CellVisuals").transform;
        _visualRoot.SetParent(transform, false);

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                _cells[x, y] = new FarmGridCellData(x, y);
                CreateCellVisual(coordinates);
                RefreshCellVisual(coordinates);
            }
        }
    }

    private void CreateCellVisual(Vector2Int coordinates)
    {
        GameObject cellObject = new GameObject($"Cell_{coordinates.x}_{coordinates.y}");
        cellObject.transform.SetParent(_visualRoot, false);
        cellObject.transform.position = GetCellCenterWorldPosition(coordinates);
        cellObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, _cellSize - _cellPadding);

        SpriteRenderer cellRenderer = cellObject.AddComponent<SpriteRenderer>();
        cellRenderer.sprite = GetCellSprite();
        cellRenderer.sortingOrder = 0;
        _cellRenderers[coordinates.x, coordinates.y] = cellRenderer;

        GameObject cropObject = new GameObject($"Crop_{coordinates.x}_{coordinates.y}");
        cropObject.transform.SetParent(cellObject.transform, false);
        cropObject.transform.localPosition = Vector3.zero;

        SpriteRenderer cropRenderer = cropObject.AddComponent<SpriteRenderer>();
        cropRenderer.sortingOrder = 1;
        cropRenderer.enabled = false;
        _cropRenderers[coordinates.x, coordinates.y] = cropRenderer;
    }

    private void RefreshCellVisual(Vector2Int coordinates)
    {
        if (!IsWithinBounds(coordinates))
        {
            return;
        }

        SpriteRenderer renderer = _cellRenderers[coordinates.x, coordinates.y];

        if (renderer == null)
        {
            return;
        }

        renderer.color = _cells[coordinates.x, coordinates.y].State switch
        {
            FarmTileState.TilledSoil => _tilledSoilColor,
            FarmTileState.WateredSoil => _wateredSoilColor,
            _ => _normalSoilColor
        };
    }

    private void RefreshCropVisual(Vector2Int coordinates)
    {
        if (!IsWithinBounds(coordinates))
        {
            return;
        }

        SpriteRenderer cropRenderer = _cropRenderers[coordinates.x, coordinates.y];

        if (cropRenderer == null)
        {
            return;
        }

        FarmGridCellData cell = _cells[coordinates.x, coordinates.y];

        if (cell.CropType == FarmCropType.None)
        {
            cropRenderer.enabled = false;
            cropRenderer.sprite = null;
            return;
        }

        FarmCropDefinition cropDefinition = GetCropDefinition(cell.CropType);
        Sprite stageSprite = cropDefinition != null ? cropDefinition.GetStageSprite(cell.GrowthStage) : null;

        cropRenderer.enabled = true;

        if (stageSprite != null)
        {
            cropRenderer.sprite = stageSprite;
            cropRenderer.color = Color.white;
            cropRenderer.transform.localScale = Vector3.one * GetSpriteScale(stageSprite);
        }
        else
        {
            cropRenderer.sprite = GetCellSprite();
            cropRenderer.color = GetFallbackCropColor(cell.CropType);
            cropRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.75f, cell.GrowthStage / (float)Mathf.Max(1, _maxGrowthStage));
        }
    }

    private FarmCropDefinition GetCropDefinition(FarmCropType cropType)
    {
        if (_cropDefinitions == null)
        {
            return null;
        }

        foreach (FarmCropDefinition cropDefinition in _cropDefinitions)
        {
            if (cropDefinition != null && cropDefinition.CropType == cropType)
            {
                return cropDefinition;
            }
        }

        return null;
    }

    private bool IsWithinBounds(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.x < _width && coordinates.y >= 0 && coordinates.y < _height;
    }

    private Vector2 GetBottomLeftWorldPosition()
    {
        return new Vector2(
            transform.position.x - (_width * _cellSize * 0.5f),
            transform.position.y - (_height * _cellSize * 0.5f));
    }

    private void ClearExistingVisuals()
    {
        Transform existingRoot = transform.Find("CellVisuals");

        if (existingRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existingRoot.gameObject);
        }
        else
        {
            DestroyImmediate(existingRoot.gameObject);
        }
    }

    private float GetSpriteScale(Sprite sprite)
    {
        if (sprite == null)
        {
            return _cropSpriteScale;
        }

        Vector2 spriteSize = sprite.bounds.size;
        float largestDimension = Mathf.Max(spriteSize.x, spriteSize.y);

        if (largestDimension <= 0.0001f)
        {
            return _cropSpriteScale;
        }

        return (_cellSize * _cropSpriteScale) / largestDimension;
    }

    private static Sprite GetCellSprite()
    {
        if (_cellSprite != null)
        {
            return _cellSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        _cellSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        return _cellSprite;
    }

    private static Color GetFallbackCropColor(FarmCropType cropType)
    {
        return cropType switch
        {
            FarmCropType.Beetroot => new Color(0.62f, 0.12f, 0.24f, 1f),
            FarmCropType.Carrot => new Color(0.94f, 0.5f, 0.12f, 1f),
            FarmCropType.Potato => new Color(0.73f, 0.59f, 0.34f, 1f),
            FarmCropType.Wheat => new Color(0.88f, 0.74f, 0.28f, 1f),
            _ => Color.white
        };
    }
}
