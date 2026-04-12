using UnityEngine;

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
    [SerializeField] private Color _tomatoColor = new Color(0.85f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color _carrotColor = new Color(0.95f, 0.52f, 0.14f, 1f);
    [SerializeField] private Color _wheatColor = new Color(0.89f, 0.76f, 0.29f, 1f);
    [SerializeField] private Color _cornColor = new Color(0.95f, 0.86f, 0.19f, 1f);
    [SerializeField] private int _maxGrowthStage = 3;

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
            return false;
        }

        if (!TryGetCell(coordinates, out FarmGridCellData cell))
        {
            return false;
        }

        if (cell.State != FarmTileState.TilledSoil)
        {
            return false;
        }

        if (cell.CropType != FarmCropType.None)
        {
            return false;
        }

        cell.CropType = cropType;
        cell.GrowthStage = 1;
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

        FarmTileState? nextState = GetToolResult(cell.State, toolType);

        if (!nextState.HasValue)
        {
            return false;
        }

        return SetCellState(coordinates, nextState.Value);
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
        cropRenderer.sprite = GetCellSprite();
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
            cropRenderer.color = GetColorForCrop(cropType);
            cropRenderer.transform.localScale = Vector3.one * GetCropVisualScale(_cells[coordinates.x, coordinates.y].GrowthStage);
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

    private Color GetColorForCrop(FarmCropType cropType)
    {
        return cropType switch
        {
            FarmCropType.Tomato => _tomatoColor,
            FarmCropType.Carrot => _carrotColor,
            FarmCropType.Wheat => _wheatColor,
            FarmCropType.Corn => _cornColor,
            _ => Color.clear
        };
    }

    private float GetCropVisualScale(int growthStage)
    {
        return growthStage switch
        {
            1 => 0.3f,
            2 => 0.45f,
            _ => 0.6f
        };
    }

    private FarmTileState? GetToolResult(FarmTileState currentState, FarmToolType toolType)
    {
        return toolType switch
        {
            FarmToolType.Hoe when currentState == FarmTileState.NormalSoil => FarmTileState.TilledSoil,
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
}
