using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventorySystem _inventorySystem;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private Transform _inventorySlotsContainer;
    [SerializeField] private Transform _hotbarSlotsContainer;
    [SerializeField] private InventorySlotUI _slotPrefab;
    [SerializeField] private KeyCode _toggleInventoryKey = KeyCode.E;

    private readonly List<InventorySlotUI> _slotUis = new List<InventorySlotUI>();
    private Canvas _canvas;
    private int _dragSourceIndex = -1;

    public Canvas DragCanvas => _canvas;

    private void Awake()
    {
        if (_inventorySystem == null)
        {
            _inventorySystem = GetComponent<InventorySystem>();
        }

        if (_inventorySystem == null)
        {
            _inventorySystem = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
        }

        _canvas = GetComponentInParent<Canvas>();

        if (_canvas == null)
        {
            _canvas = CreateCanvas();
        }

        EnsureEventSystem();
        EnsureLayout();
    }

    private void OnEnable()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        _inventorySystem.InventoryChanged += RefreshUI;
        _inventorySystem.HotbarSelectionChanged += HandleHotbarSelectionChanged;
    }

    private void OnDisable()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        _inventorySystem.InventoryChanged -= RefreshUI;
        _inventorySystem.HotbarSelectionChanged -= HandleHotbarSelectionChanged;
    }

    private void Start()
    {
        CreateSlots();
        RefreshUI();
        SetInventoryVisible(false);
    }

    private void Update()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        HandleInventoryToggleInput();
        HandleHotbarInput();
    }

    public void BeginSlotDrag(int slotIndex)
    {
        _dragSourceIndex = slotIndex;
    }

    public void EndSlotDrag()
    {
        _dragSourceIndex = -1;
        RefreshUI();
    }

    public void HandleSlotDrop(int fromIndex, int toIndex)
    {
        _inventorySystem?.MoveOrSwapItem(fromIndex, toIndex);
    }

    public void HandleSlotDropOutside(int slotIndex)
    {
        _inventorySystem?.ClearSlot(slotIndex);
    }

    public void HandleSlotClicked(int slotIndex)
    {
        if (_inventorySystem == null || slotIndex < 0 || slotIndex >= _inventorySystem.HotbarSize)
        {
            return;
        }

        _inventorySystem.SelectHotbarSlot(slotIndex);
    }

    public void RefreshUI()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        IReadOnlyList<InventorySlotData> slots = _inventorySystem.Slots;
        int count = Mathf.Min(slots.Count, _slotUis.Count);

        for (int i = 0; i < count; i++)
        {
            InventorySlotUI slotUi = _slotUis[i];

            if (slotUi == null)
            {
                continue;
            }

            InventorySlotData slot = slots[i];
            slotUi.SetSlot(slot.Item, slot.Quantity);
            slotUi.SetSelected(i < _inventorySystem.HotbarSize && i == _inventorySystem.SelectedHotbarSlotIndex);
        }
    }

    private void HandleHotbarSelectionChanged(int selectedSlotIndex)
    {
        RefreshUI();
    }

    private void HandleInventoryToggleInput()
    {
        if (_dragSourceIndex < 0 && Input.GetKeyDown(_toggleInventoryKey))
        {
            SetInventoryVisible(!IsInventoryVisible());
        }
    }

    private void HandleHotbarInput()
    {
        for (int i = 0; i < _inventorySystem.HotbarSize; i++)
        {
            KeyCode keyCode = (KeyCode)((int)KeyCode.Alpha1 + i);

            if (Input.GetKeyDown(keyCode))
            {
                _inventorySystem.SelectHotbarSlot(i);
            }
        }

        float scrollAmount = Input.mouseScrollDelta.y;

        if (!Mathf.Approximately(scrollAmount, 0f))
        {
            _inventorySystem.SelectNextHotbarSlot(scrollAmount > 0f ? -1 : 1);
        }
    }

    private void CreateSlots()
    {
        if (_inventorySystem == null || _slotUis.Count > 0)
        {
            return;
        }

        EnsureSlotCapacity(_inventorySystem.TotalSlotCount);

        for (int i = 0; i < _inventorySystem.HotbarSize; i++)
        {
            _slotUis[i] = CreateSlotUi(i, _hotbarSlotsContainer);
        }

        for (int i = _inventorySystem.InventoryStartIndex; i < _inventorySystem.TotalSlotCount; i++)
        {
            _slotUis[i] = CreateSlotUi(i, _inventorySlotsContainer);
        }
    }

    private InventorySlotUI CreateSlotUi(int slotIndex, Transform parent)
    {
        InventorySlotUI slotUi = _slotPrefab != null
            ? Instantiate(_slotPrefab, parent)
            : CreateRuntimeSlot(parent);

        slotUi.Setup(this, slotIndex);
        return slotUi;
    }

    private void EnsureLayout()
    {
        if (_inventoryPanel == null)
        {
            _inventoryPanel = CreatePanel(
                "InventoryPanel",
                _canvas.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(452f, 384f),
                new Color(0.07f, 0.07f, 0.07f, 0.88f));
        }

        if (_inventorySlotsContainer == null)
        {
            _inventorySlotsContainer = CreateSlotContainer("InventorySlots", _inventoryPanel.transform, new Vector2(400f, 320f), 5);
        }

        if (_hotbarSlotsContainer == null)
        {
            GameObject hotbarPanel = CreatePanel(
                "HotbarPanel",
                _canvas.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(664f, 104f),
                new Color(0.07f, 0.07f, 0.07f, 0.8f));

            RectTransform panelRect = hotbarPanel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(0f, 24f);
            _hotbarSlotsContainer = CreateSlotContainer("HotbarSlots", hotbarPanel.transform, new Vector2(632f, 72f), 8);
        }
    }

    private void EnsureSlotCapacity(int totalSlots)
    {
        while (_slotUis.Count < totalSlots)
        {
            _slotUis.Add(null);
        }
    }

    private bool IsInventoryVisible()
    {
        return _inventoryPanel != null && _inventoryPanel.activeSelf;
    }

    private void SetInventoryVisible(bool isVisible)
    {
        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(isVisible);
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 sizeDelta,
        Color color)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = sizeDelta;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;

        return panelObject;
    }

    private static Transform CreateSlotContainer(string objectName, Transform parent, Vector2 sizeDelta, int columns)
    {
        GameObject containerObject = new GameObject(objectName, typeof(RectTransform), typeof(GridLayoutGroup));
        containerObject.transform.SetParent(parent, false);

        RectTransform rectTransform = containerObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = sizeDelta;

        GridLayoutGroup gridLayout = containerObject.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(72f, 72f);
        gridLayout.spacing = new Vector2(8f, 8f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.padding = new RectOffset(8, 8, 8, 8);

        return containerObject.transform;
    }

    private static InventorySlotUI CreateRuntimeSlot(Transform parent)
    {
        GameObject slotObject = new GameObject("InventorySlot", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(InventorySlotUI));
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRectTransform = slotObject.GetComponent<RectTransform>();
        slotRectTransform.sizeDelta = new Vector2(72f, 72f);

        Image slotBackground = slotObject.GetComponent<Image>();
        slotBackground.color = Color.white;

        Outline outline = slotObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);

        RectTransform iconRectTransform = iconObject.GetComponent<RectTransform>();
        iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
        iconRectTransform.sizeDelta = new Vector2(40f, 40f);

        Image itemIcon = iconObject.GetComponent<Image>();
        itemIcon.raycastTarget = false;

        GameObject quantityObject = new GameObject("QuantityText", typeof(RectTransform), typeof(TextMeshProUGUI));
        quantityObject.transform.SetParent(slotObject.transform, false);

        RectTransform quantityRect = quantityObject.GetComponent<RectTransform>();
        quantityRect.anchorMin = new Vector2(1f, 0f);
        quantityRect.anchorMax = new Vector2(1f, 0f);
        quantityRect.pivot = new Vector2(1f, 0f);
        quantityRect.anchoredPosition = new Vector2(-4f, 4f);
        quantityRect.sizeDelta = new Vector2(28f, 20f);

        TextMeshProUGUI quantityText = quantityObject.GetComponent<TextMeshProUGUI>();
        quantityText.font = TMP_Settings.defaultFontAsset;
        quantityText.fontSize = 18f;
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.raycastTarget = false;

        GameObject labelObject = new GameObject("ItemLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(slotObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(52f, 22f);

        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.font = TMP_Settings.defaultFontAsset;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        labelText.raycastTarget = false;
        labelText.enabled = false;

        InventorySlotUI slotUi = slotObject.GetComponent<InventorySlotUI>();
        slotUi.ConfigureRuntimeReferences(slotBackground, itemIcon, quantityText, labelText);
        return slotUi;
    }
}
