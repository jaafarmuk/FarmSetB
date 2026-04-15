using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FarmGridManager : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int _width = 20;
    [SerializeField] private int _height = 20;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private float _cellPadding = 0.08f;

    [Header("Mock Visuals")]
    [SerializeField] private Color _normalSoilColor = new Color(0.47f, 0.33f, 0.21f, 1f);
    [SerializeField] private Color _tilledSoilColor = new Color(0.36f, 0.24f, 0.14f, 1f);
    [SerializeField] private Color _wateredSoilColor = new Color(0.24f, 0.39f, 0.54f, 1f);
    [SerializeField] private FarmCropDefinition[] _cropDefinitions;
    [SerializeField] private float _cropSpriteScale = 0.8f;
    [SerializeField] private int _maxGrowthStage = 4;

    private FarmGridCellData[,] _cells;
    private SpriteRenderer[,] _cellRenderers;
    private SpriteRenderer[,] _cropRenderers;
    private Transform _visualRoot;
    private InventorySystem _inventorySystem;

    private static Sprite _cellSprite;
#if UNITY_EDITOR
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> EditorCropSpriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();
    private static readonly System.Collections.Generic.Dictionary<string, ItemData> EditorItemCache = new System.Collections.Generic.Dictionary<string, ItemData>();
#endif
    private int _currentDay = 1;

    public int Width => _width;
    public int Height => _height;
    public float CellSize => _cellSize;
    public int CurrentDay => _currentDay;

    private void Awake()
    {
        _inventorySystem = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
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
        if (cropType == FarmCropType.None)
        {
            Debug.LogWarning("Planting failed: no crop type selected.");
            return false;
        }

        if (!TryGetCell(coordinates, out FarmGridCellData cell))
        {
            Debug.LogWarning($"Planting failed: cell {coordinates} is outside the grid.");
            return false;
        }

        if (cell.State != FarmTileState.TilledSoil &&
            cell.State != FarmTileState.WateredSoil)
        {
            Debug.LogWarning($"Planting failed at {coordinates}: tile state is {cell.State}.");
            return false;
        }

        if (cell.CropType != FarmCropType.None)
        {
            Debug.LogWarning($"Planting failed at {coordinates}: tile already has {cell.CropType}.");
            return false;
        }

        cell.CropType = cropType;
        cell.GrowthStage = 0;
        RefreshCropVisual(coordinates);
        Debug.Log($"Planted {cropType} at {coordinates}.");
        return true;
    }

    public void AdvanceDay()
    {
        _currentDay++;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);
                FarmGridCellData cell = _cells[x, y];

                if (cell.CropType != FarmCropType.None &&
                    cell.State == FarmTileState.WateredSoil &&
                    cell.GrowthStage < _maxGrowthStage)
                {
                    cell.GrowthStage++;
                }

                if (cell.State == FarmTileState.WateredSoil)
                {
                    cell.State = FarmTileState.TilledSoil;
                }

                RefreshCellVisual(coordinates);
                RefreshCropVisual(coordinates);
            }
        }

        Debug.Log($"Advanced to day {_currentDay}.");
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

        FarmTileState? nextState = GetToolResult(cell.State, toolType);

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

        ItemData harvestedItem = ResolveHarvestItem(cell.CropType);

        if (harvestedItem == null)
        {
            Debug.LogWarning($"Harvest failed at {coordinates}: no inventory item is configured for {cell.CropType}.");
            return false;
        }

        if (_inventorySystem == null)
        {
            _inventorySystem = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
        }

        if (_inventorySystem == null)
        {
            Debug.LogWarning($"Harvest failed at {coordinates}: no InventorySystem was found in the scene.");
            return false;
        }

        if (!_inventorySystem.AddItem(harvestedItem, 1))
        {
            Debug.LogWarning($"Harvest failed at {coordinates}: inventory is full.");
            return false;
        }

        cell.CropType = FarmCropType.None;
        cell.GrowthStage = 0;
        cell.State = FarmTileState.TilledSoil;

        RefreshCellVisual(coordinates);
        RefreshCropVisual(coordinates);
        Debug.Log($"Harvested {harvestedItem.ItemName} from {coordinates}.");
        return true;
    }

    public bool CycleCellState(Vector2Int coordinates)
    {
        if (!TryGetCell(coordinates, out FarmGridCellData cell))
        {
            return false;
        }

        FarmTileState nextState = cell.State switch
        {
            FarmTileState.NormalSoil => FarmTileState.TilledSoil,
            FarmTileState.TilledSoil => FarmTileState.WateredSoil,
            _ => FarmTileState.NormalSoil
        };

        return SetCellState(coordinates, nextState);
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
                _cells[x, y] = new FarmGridCellData(x, y);
                CreateCellVisual(new Vector2Int(x, y));
                RefreshCellVisual(new Vector2Int(x, y));
            }
        }
    }

    private void CreateCellVisual(Vector2Int coordinates)
    {
        GameObject cellObject = new GameObject($"Cell_{coordinates.x}_{coordinates.y}");
        cellObject.transform.SetParent(_visualRoot, false);
        cellObject.transform.position = GetCellCenterWorldPosition(coordinates);
        cellObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, _cellSize - _cellPadding);

        SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetCellSprite();
        renderer.sortingOrder = 0;

        _cellRenderers[coordinates.x, coordinates.y] = renderer;

        GameObject cropObject = new GameObject($"Crop_{coordinates.x}_{coordinates.y}");
        cropObject.transform.SetParent(cellObject.transform, false);
        cropObject.transform.localPosition = Vector3.zero;
        cropObject.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer cropRenderer = cropObject.AddComponent<SpriteRenderer>();
        cropRenderer.sortingOrder = 1;
        cropRenderer.enabled = false;

        _cropRenderers[coordinates.x, coordinates.y] = cropRenderer;
    }

    private void RefreshCellVisual(Vector2Int coordinates)
    {
        if (_cellRenderers == null || !IsWithinBounds(coordinates))
        {
            return;
        }

        SpriteRenderer renderer = _cellRenderers[coordinates.x, coordinates.y];

        if (renderer == null)
        {
            return;
        }

        renderer.color = GetColorForState(_cells[coordinates.x, coordinates.y].State);
    }

    private void RefreshCropVisual(Vector2Int coordinates)
    {
        if (_cropRenderers == null || !IsWithinBounds(coordinates))
        {
            return;
        }

        SpriteRenderer cropRenderer = _cropRenderers[coordinates.x, coordinates.y];

        if (cropRenderer == null)
        {
            return;
        }

        FarmCropType cropType = _cells[coordinates.x, coordinates.y].CropType;
        bool hasCrop = cropType != FarmCropType.None;

        cropRenderer.enabled = hasCrop;

        if (hasCrop)
        {
            int growthStage = _cells[coordinates.x, coordinates.y].GrowthStage;
            Sprite stageSprite = GetCropStageSprite(cropType, growthStage);
            bool hasStageSprite = stageSprite != null;

            cropRenderer.enabled = true;
            cropRenderer.sprite = hasStageSprite ? stageSprite : GetCellSprite();
            cropRenderer.color = hasStageSprite ? Color.white : GetFallbackCropColor(cropType);
            cropRenderer.transform.localScale = hasStageSprite
                ? Vector3.one * GetSpriteScaleForCell(stageSprite)
                : Vector3.one * GetFallbackCropScale(growthStage);
        }
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

    private Color GetColorForState(FarmTileState state)
    {
        return state switch
        {
            FarmTileState.TilledSoil => _tilledSoilColor,
            FarmTileState.WateredSoil => _wateredSoilColor,
            _ => _normalSoilColor
        };
    }

    private Sprite GetCropStageSprite(FarmCropType cropType, int growthStage)
    {
        if (_cropDefinitions == null)
        {
            return ResolveEditorCropStageSprite(cropType, growthStage);
        }

        foreach (FarmCropDefinition cropDefinition in _cropDefinitions)
        {
            if (cropDefinition != null && cropDefinition.CropType == cropType)
            {
                Sprite stageSprite = cropDefinition.GetStageSprite(growthStage);

                if (stageSprite != null)
                {
                    return stageSprite;
                }

                break;
            }
        }

        return ResolveEditorCropStageSprite(cropType, growthStage);
    }

    private FarmTileState? GetToolResult(FarmTileState currentState, FarmToolType toolType)
    {
        return toolType switch
        {
            FarmToolType.Shovel when currentState == FarmTileState.NormalSoil => FarmTileState.TilledSoil,
            FarmToolType.WateringCan when currentState == FarmTileState.TilledSoil => FarmTileState.WateredSoil,
            _ => null
        };
    }

    private void ClearExistingVisuals()
    {
        Transform existingRoot = transform.Find("CellVisuals");

        if (existingRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
            }
            else
            {
                DestroyImmediate(existingRoot.gameObject);
            }
        }
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

    private static Sprite ResolveEditorCropStageSprite(FarmCropType cropType, int growthStage)
    {
#if UNITY_EDITOR
        string cropName = GetCropAssetName(cropType);

        if (string.IsNullOrEmpty(cropName))
        {
            return null;
        }

        string spriteKey = $"{cropName}_{Mathf.Clamp(growthStage, 0, 99):00}";

        if (EditorCropSpriteCache.TryGetValue(spriteKey, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        string[] matchingGuids = AssetDatabase.FindAssets($"{spriteKey} t:Sprite");

        foreach (string guid in matchingGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (UnityEngine.Object asset in assetsAtPath)
            {
                if (asset is Sprite sprite)
                {
                    EditorCropSpriteCache[spriteKey] = sprite;
                    return sprite;
                }
            }
        }
#endif

        return null;
    }

    private static string GetCropAssetName(FarmCropType cropType)
    {
        return cropType switch
        {
            FarmCropType.Beetroot => "beetroot",
            FarmCropType.Carrot => "carrot",
            FarmCropType.Potato => "potato",
            FarmCropType.Wheat => "wheat",
            _ => null
        };
    }

    private static string GetHarvestItemId(FarmCropType cropType)
    {
        return cropType switch
        {
            FarmCropType.Beetroot => "crop_beetroot",
            FarmCropType.Carrot => "crop_carrot",
            FarmCropType.Potato => "crop_potato",
            FarmCropType.Wheat => "crop_wheat",
            _ => null
        };
    }

    private static ItemData ResolveHarvestItem(FarmCropType cropType)
    {
        string itemId = GetHarvestItemId(cropType);

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        ItemData loadedItem = FindLoadedItemById(itemId);

        if (loadedItem != null)
        {
            return loadedItem;
        }

#if UNITY_EDITOR
        if (EditorItemCache.TryGetValue(itemId, out ItemData cachedItem))
        {
            return cachedItem;
        }

        string[] itemGuids = AssetDatabase.FindAssets("t:ItemData");

        foreach (string guid in itemGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemAsset = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (itemAsset != null && string.Equals(itemAsset.ItemId, itemId, System.StringComparison.OrdinalIgnoreCase))
            {
                EditorItemCache[itemId] = itemAsset;
                return itemAsset;
            }
        }
#endif

        return null;
    }

    private static ItemData FindLoadedItemById(string itemId)
    {
        ItemData[] loadedItems = Resources.FindObjectsOfTypeAll<ItemData>();

        foreach (ItemData item in loadedItems)
        {
            if (item != null && string.Equals(item.ItemId, itemId, System.StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private Color GetFallbackCropColor(FarmCropType cropType)
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

    private float GetFallbackCropScale(int growthStage)
    {
        return Mathf.Lerp(0.3f, 0.75f, Mathf.Clamp01(growthStage / (float)Mathf.Max(1, _maxGrowthStage)));
    }

    private float GetSpriteScaleForCell(Sprite sprite)
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

        float targetSize = _cellSize * _cropSpriteScale;
        return targetSize / largestDimension;
    }
}
