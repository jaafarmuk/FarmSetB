using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSourceDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ItemData _itemData;
    [SerializeField] private Image _sourceIcon;
    [SerializeField] private Canvas _dragCanvas;
    [SerializeField] private int _dragAmount = 1;

    private Image _dragVisualImage;
    private RectTransform _dragVisualRect;
    private int _draggedAmount;
    private bool _wasDroppedOnValidSlot;

    public ItemData SourceItem => _itemData;
    public int DraggedAmount => _draggedAmount;
    public bool WasDroppedOnValidSlot => _wasDroppedOnValidSlot;

    private void Awake()
    {
        if (_dragCanvas == null)
        {
            _dragCanvas = GetComponentInParent<Canvas>();
        }

        RefreshSourceIcon();
    }

    private void OnValidate()
    {
        RefreshSourceIcon();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_itemData == null || _itemData.Icon == null || _dragCanvas == null)
        {
            return;
        }

        _wasDroppedOnValidSlot = false;
        _draggedAmount = Mathf.Max(1, _dragAmount);
        CreateDragVisual();
        UpdateDragVisualPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragVisualRect == null)
        {
            return;
        }

        UpdateDragVisualPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyDragVisual();
        _draggedAmount = 0;
        _wasDroppedOnValidSlot = false;
    }

    public void RegisterValidDrop(int amountRemaining)
    {
        _wasDroppedOnValidSlot = true;
        _draggedAmount = Mathf.Max(0, amountRemaining);
    }

    private void CreateDragVisual()
    {
        DestroyDragVisual();

        GameObject dragVisualObject = new GameObject("ItemSourceDragVisual", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragVisualObject.transform.SetParent(_dragCanvas.transform, false);

        CanvasGroup canvasGroup = dragVisualObject.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        _dragVisualRect = dragVisualObject.GetComponent<RectTransform>();
        _dragVisualRect.anchorMin = new Vector2(0.5f, 0.5f);
        _dragVisualRect.anchorMax = new Vector2(0.5f, 0.5f);
        _dragVisualRect.pivot = new Vector2(0.5f, 0.5f);

        if (_sourceIcon != null)
        {
            _dragVisualRect.sizeDelta = _sourceIcon.rectTransform.rect.size;
        }

        _dragVisualImage = dragVisualObject.GetComponent<Image>();
        _dragVisualImage.sprite = _itemData.Icon;
        _dragVisualImage.preserveAspect = true;
        _dragVisualImage.raycastTarget = false;
    }

    private void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (_dragCanvas == null || _dragVisualRect == null)
        {
            return;
        }

        RectTransform canvasRect = _dragCanvas.transform as RectTransform;
        Camera eventCamera = _dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            _dragVisualRect.localPosition = localPoint;
        }
    }

    private void DestroyDragVisual()
    {
        if (_dragVisualImage == null)
        {
            return;
        }

        Destroy(_dragVisualImage.gameObject);
        _dragVisualImage = null;
        _dragVisualRect = null;
    }

    private void RefreshSourceIcon()
    {
        if (_sourceIcon == null || _itemData == null)
        {
            return;
        }

        _sourceIcon.sprite = _itemData.Icon;
        _sourceIcon.enabled = _itemData.Icon != null;
    }
}
