using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventorySystem _inventorySystem;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private Transform _inventorySlotsContainer;
    [SerializeField] private Transform _hotbarSlotsContainer;
    [SerializeField] private Transform _slotsContainer;
    [SerializeField] private InventorySlotUI _slotPrefab;
    [SerializeField] private Canvas _dragCanvas;
    [SerializeField] private KeyCode _toggleInventoryKey = KeyCode.E;

    private readonly List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();
    private int _dragSourceIndex = -1;
    private HotbarController _hotbarController;

    public Canvas DragCanvas => _dragCanvas;
    private bool IsDraggingSlot => _dragSourceIndex >= 0;

    private void Awake()
    {
        if (_inventorySystem == null)
        {
            _inventorySystem = Object.FindAnyObjectByType<InventorySystem>();
        }

        if (_dragCanvas == null)
        {
            _dragCanvas = GetComponentInParent<Canvas>();
        }

        if (_dragCanvas == null)
        {
            _dragCanvas = Object.FindAnyObjectByType<Canvas>();
        }

        _hotbarController = Object.FindAnyObjectByType<HotbarController>();
        EnsureUiReferences();
    }

    private void OnEnable()
    {
        if (_inventorySystem != null)
        {
            _inventorySystem.InventoryChanged += HandleInventoryChanged;
        }

        if (_hotbarController != null)
        {
            _hotbarController.SelectedSlotChanged += HandleSelectedHotbarSlotChanged;
        }
    }

    private void OnDisable()
    {
        if (_inventorySystem != null)
        {
            _inventorySystem.InventoryChanged -= HandleInventoryChanged;
        }

        if (_hotbarController != null)
        {
            _hotbarController.SelectedSlotChanged -= HandleSelectedHotbarSlotChanged;
        }

        _dragSourceIndex = -1;
    }

    private void Start()
    {
        CreateSlots();
        RefreshUI();
        SetInventoryVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleInventoryKey) && !IsDraggingSlot)
        {
            SetInventoryVisible(!IsInventoryVisible());
        }
    }

    private void EnsureUiReferences()
    {
        if (_inventorySlotsContainer == null && _slotsContainer != null)
        {
            _inventorySlotsContainer = _slotsContainer;
        }

        if (_inventoryPanel == null && (_inventorySlotsContainer != null || _slotsContainer != null))
        {
            Transform panelTransform = _inventorySlotsContainer != null ? _inventorySlotsContainer.parent : _slotsContainer.parent;
            _inventoryPanel = panelTransform != null ? panelTransform.gameObject : gameObject;
        }

        if (_dragCanvas == null)
        {
            return;
        }

        if (_inventoryPanel == null || _inventorySlotsContainer == null)
        {
            CreateInventoryPanel();
        }

        if (_hotbarSlotsContainer == null)
        {
            CreateHotbarPanel();
        }
    }

    private void CreateSlots()
    {
        if (_inventorySystem == null || (_inventorySlotsContainer == null && _hotbarSlotsContainer == null))
        {
            return;
        }

        if (_slotUIs.Count > 0)
        {
            return;
        }

        int totalSlots = _inventorySystem.TotalSlotCount;
        EnsureSlotUiCapacity(totalSlots);

        for (int i = 0; i < _inventorySystem.HotbarSize; i++)
        {
            if (_hotbarSlotsContainer == null)
            {
                break;
            }

            CreateSlotUi(i, _hotbarSlotsContainer);
        }

        for (int i = _inventorySystem.InventoryStartIndex; i < totalSlots; i++)
        {
            Transform parent = _inventorySlotsContainer != null ? _inventorySlotsContainer : _hotbarSlotsContainer;

            if (parent == null)
            {
                break;
            }

            CreateSlotUi(i, parent);
        }
    }

    private void CreateSlotUi(int slotIndex, Transform parent)
    {
        InventorySlotUI slotUI = _slotPrefab != null
            ? Instantiate(_slotPrefab, parent)
            : CreateRuntimeSlot(parent);

        slotUI.Setup(this, slotIndex);
        _slotUIs[slotIndex] = slotUI;
    }

    private void HandleInventoryChanged()
    {
        if (IsDraggingSlot)
        {
            return;
        }

        RefreshUI();
    }

    private void HandleSelectedHotbarSlotChanged(int slotIndex)
    {
        RefreshHotbarSelection();
    }

    public void RefreshUI()
    {
        if (_inventorySystem == null)
        {
            return;
        }

        List<InventorySlotData> slots = _inventorySystem.GetSlots();
        int count = Mathf.Min(slots.Count, _slotUIs.Count);

        for (int i = 0; i < count; i++)
        {
            InventorySlotUI slotUI = _slotUIs[i];

            if (slotUI == null)
            {
                continue;
            }

            slotUI.SetSlot(slots[i].Item, slots[i].Quantity);
        }

        RefreshHotbarSelection();
    }

    public void BeginSlotDrag(int slotIndex)
    {
        _dragSourceIndex = slotIndex;
    }

    public void HandleSlotDrop(int fromIndex, int toIndex)
    {
        if (_inventorySystem == null)
        {
            return;
        }

        _inventorySystem.MoveOrSwapItem(fromIndex, toIndex);
    }

    public bool HandleSourceDrop(int slotIndex, ItemData item, int amount, out int amountRemaining)
    {
        amountRemaining = amount;

        if (_inventorySystem == null)
        {
            return false;
        }

        return _inventorySystem.TryAddToSlot(slotIndex, item, amount, out amountRemaining);
    }

    public void HandleSlotDropOutside(int slotIndex)
    {
        if (_inventorySystem == null)
        {
            return;
        }

        _inventorySystem.DropItemFromSlot(slotIndex);
    }

    public void EndSlotDrag()
    {
        _dragSourceIndex = -1;
        RefreshUI();
    }

    private void EnsureSlotUiCapacity(int totalSlots)
    {
        while (_slotUIs.Count < totalSlots)
        {
            _slotUIs.Add(null);
        }
    }

    private bool IsInventoryVisible()
    {
        return _inventoryPanel != null && _inventoryPanel.activeSelf;
    }

    private void RefreshHotbarSelection()
    {
        if (_hotbarController == null)
        {
            _hotbarController = Object.FindAnyObjectByType<HotbarController>();
        }

        int selectedSlotIndex = _hotbarController != null ? _hotbarController.SelectedSlotIndex : -1;
        int hotbarSize = _inventorySystem != null ? _inventorySystem.HotbarSize : 0;

        for (int i = 0; i < _slotUIs.Count; i++)
        {
            InventorySlotUI slotUI = _slotUIs[i];

            if (slotUI == null)
            {
                continue;
            }

            slotUI.SetSelected(i < hotbarSize && i == selectedSlotIndex);
        }
    }

    private void SetInventoryVisible(bool isVisible)
    {
        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(isVisible);
        }
    }

    private void CreateInventoryPanel()
    {
        GameObject panelObject = CreatePanel(
            "InventoryPanel",
            _dragCanvas.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(452f, 384f),
            new Color(0.07f, 0.07f, 0.07f, 0.88f));

        _inventoryPanel = panelObject;
        _inventorySlotsContainer = CreateSlotContainer(
            "InventorySlots",
            panelObject.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(400f, 320f),
            5);
    }

    private void CreateHotbarPanel()
    {
        GameObject panelObject = CreatePanel(
            "HotbarPanel",
            _dragCanvas.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 24f),
            new Vector2(664f, 104f),
            new Color(0.07f, 0.07f, 0.07f, 0.8f));

        _hotbarSlotsContainer = CreateSlotContainer(
            "HotbarSlots",
            panelObject.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(632f, 72f),
            8);
    }

    private static GameObject CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = color;

        return panelObject;
    }

    private static Transform CreateSlotContainer(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        int columns)
    {
        GameObject containerObject = new GameObject(objectName, typeof(RectTransform), typeof(GridLayoutGroup));
        containerObject.transform.SetParent(parent, false);

        RectTransform rectTransform = containerObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
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
        slotBackground.color = new Color(0.74f, 0.74f, 0.74f, 1f);
        slotBackground.raycastTarget = true;

        Outline outline = slotObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);

        RectTransform iconRectTransform = iconObject.GetComponent<RectTransform>();
        iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
        iconRectTransform.anchoredPosition = Vector2.zero;
        iconRectTransform.sizeDelta = new Vector2(40f, 40f);

        Image itemIcon = iconObject.GetComponent<Image>();
        itemIcon.raycastTarget = false;

        GameObject textObject = new GameObject("QuantityText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(slotObject.transform, false);

        RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(1f, 0f);
        textRectTransform.anchorMax = new Vector2(1f, 0f);
        textRectTransform.pivot = new Vector2(1f, 0f);
        textRectTransform.anchoredPosition = new Vector2(-4f, 4f);
        textRectTransform.sizeDelta = new Vector2(28f, 20f);

        TextMeshProUGUI quantityText = textObject.GetComponent<TextMeshProUGUI>();
        quantityText.font = TMP_Settings.defaultFontAsset;
        quantityText.fontSize = 18f;
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.raycastTarget = false;

        GameObject labelObject = new GameObject("ItemLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(slotObject.transform, false);

        RectTransform labelRectTransform = labelObject.GetComponent<RectTransform>();
        labelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        labelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        labelRectTransform.pivot = new Vector2(0.5f, 0.5f);
        labelRectTransform.anchoredPosition = Vector2.zero;
        labelRectTransform.sizeDelta = new Vector2(52f, 22f);

        TextMeshProUGUI itemLabelText = labelObject.GetComponent<TextMeshProUGUI>();
        itemLabelText.font = TMP_Settings.defaultFontAsset;
        itemLabelText.fontSize = 18f;
        itemLabelText.alignment = TextAlignmentOptions.Center;
        itemLabelText.raycastTarget = false;
        itemLabelText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        itemLabelText.enabled = false;

        InventorySlotUI slotUI = slotObject.GetComponent<InventorySlotUI>();
        slotUI.ConfigureRuntimeReferences(slotBackground, itemIcon, quantityText, itemLabelText);
        return slotUI;
    }
}
