using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeldItemUI : MonoBehaviour
{
    [SerializeField] private HeldItemSystem _heldItemSystem;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _rootRectTransform;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private TextMeshProUGUI _quantityText;
    [SerializeField] private Vector2 _cursorOffset = new Vector2(24f, -24f);

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        if (_rootRectTransform == null)
        {
            _rootRectTransform = transform as RectTransform;
        }

        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        if (_itemIcon != null)
        {
            _itemIcon.raycastTarget = false;
        }

        if (_quantityText != null)
        {
            _quantityText.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        if (_heldItemSystem != null)
        {
            _heldItemSystem.HeldItemChanged += RefreshUI;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (_heldItemSystem != null)
        {
            _heldItemSystem.HeldItemChanged -= RefreshUI;
        }
    }

    private void Update()
    {
        if (_heldItemSystem == null || !_heldItemSystem.HasItem)
        {
            return;
        }

        FollowCursor();
    }

    public void RefreshUI()
    {
        if (_heldItemSystem == null || !_heldItemSystem.HasItem)
        {
            SetVisualState(false, null, 0);
            return;
        }

        SetVisualState(true, _heldItemSystem.HeldItem.Icon, _heldItemSystem.HeldQuantity);
        FollowCursor();
    }

    private void FollowCursor()
    {
        if (_canvas == null || _rootRectTransform == null)
        {
            return;
        }

        RectTransform canvasRect = _canvas.transform as RectTransform;
        Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, eventCamera, out Vector2 localPoint))
        {
            _rootRectTransform.localPosition = new Vector3(localPoint.x + _cursorOffset.x, localPoint.y + _cursorOffset.y, 0f);
        }
    }

    private void SetVisualState(bool visible, Sprite iconSprite, int quantity)
    {
        if (_itemIcon != null)
        {
            _itemIcon.enabled = visible;
            _itemIcon.sprite = iconSprite;
        }

        if (_quantityText != null)
        {
            _quantityText.text = visible && quantity > 1 ? quantity.ToString() : string.Empty;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
