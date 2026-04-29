using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private StaminaSystem _staminaSystem;

    private Image _fillImage;
    private RectTransform _fillRectTransform;
    private float _maxFillWidth;
    private TextMeshProUGUI _staminaText;
    private TMP_FontAsset _fontAsset;

    private void Awake()
    {
        if (_staminaSystem == null)
        {
            _staminaSystem = GetComponent<StaminaSystem>();
        }

        _fontAsset = LoadFontAsset();
        EnsureEventSystem();
        CreateUiIfNeeded();
    }

    private void OnEnable()
    {
        if (_staminaSystem != null)
        {
            _staminaSystem.StaminaChanged += RefreshUi;
        }

        RefreshUi();
    }

    private void OnDisable()
    {
        if (_staminaSystem != null)
        {
            _staminaSystem.StaminaChanged -= RefreshUi;
        }
    }

    private void RefreshUi()
    {
        if (_staminaSystem == null)
        {
            return;
        }

        if (_fillImage != null)
        {
            float normalizedStamina = Mathf.Clamp01((float)_staminaSystem.CurrentStamina / _staminaSystem.MaxStamina);

            if (_fillRectTransform != null)
            {
                _fillRectTransform.sizeDelta = new Vector2(_maxFillWidth * normalizedStamina, _fillRectTransform.sizeDelta.y);
            }
        }

        if (_staminaText != null)
        {
            _staminaText.text = $"Stamina: {_staminaSystem.CurrentStamina} / {_staminaSystem.MaxStamina}";
        }
    }

    private void CreateUiIfNeeded()
    {
        if (transform.Find("StaminaCanvas") != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("StaminaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CreateStaminaPanel(canvasObject.transform);
    }

    private void CreateStaminaPanel(Transform canvasTransform)
    {
        GameObject panelObject = new GameObject("StaminaPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f);
        panelRect.sizeDelta = new Vector2(280f, 120f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject barBackgroundObject = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
        barBackgroundObject.transform.SetParent(panelObject.transform, false);

        RectTransform barBackgroundRect = barBackgroundObject.GetComponent<RectTransform>();
        barBackgroundRect.anchorMin = new Vector2(0f, 1f);
        barBackgroundRect.anchorMax = new Vector2(0f, 1f);
        barBackgroundRect.pivot = new Vector2(0f, 1f);
        barBackgroundRect.anchoredPosition = new Vector2(20f, -20f);
        barBackgroundRect.sizeDelta = new Vector2(240f, 24f);

        Image barBackgroundImage = barBackgroundObject.GetComponent<Image>();
        barBackgroundImage.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        GameObject barFillObject = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFillObject.transform.SetParent(barBackgroundObject.transform, false);

        RectTransform barFillRect = barFillObject.GetComponent<RectTransform>();
        barFillRect.anchorMin = new Vector2(0f, 0f);
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.pivot = new Vector2(0f, 0.5f);
        barFillRect.anchoredPosition = Vector2.zero;
        barFillRect.sizeDelta = new Vector2(barBackgroundRect.sizeDelta.x, 0f);

        _fillImage = barFillObject.GetComponent<Image>();
        _fillRectTransform = barFillRect;
        _maxFillWidth = barBackgroundRect.sizeDelta.x;
        _fillImage.color = new Color(0.31f, 0.77f, 0.34f, 1f);

        GameObject textObject = new GameObject("StaminaText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(20f, -52f);
        textRect.sizeDelta = new Vector2(240f, 24f);

        _staminaText = textObject.GetComponent<TextMeshProUGUI>();
        _staminaText.font = _fontAsset;
        _staminaText.fontSize = 18;
        _staminaText.color = Color.white;
        _staminaText.alignment = TextAlignmentOptions.Left;

        GameObject sleepButtonObject = new GameObject("SleepButton", typeof(RectTransform), typeof(Image), typeof(Button));
        sleepButtonObject.transform.SetParent(panelObject.transform, false);

        RectTransform sleepButtonRect = sleepButtonObject.GetComponent<RectTransform>();
        sleepButtonRect.anchorMin = new Vector2(0f, 1f);
        sleepButtonRect.anchorMax = new Vector2(0f, 1f);
        sleepButtonRect.pivot = new Vector2(0f, 1f);
        sleepButtonRect.anchoredPosition = new Vector2(20f, -82f);
        sleepButtonRect.sizeDelta = new Vector2(120f, 28f);

        Image sleepButtonImage = sleepButtonObject.GetComponent<Image>();
        sleepButtonImage.color = new Color(0.25f, 0.5f, 0.85f, 1f);

        Button sleepButton = sleepButtonObject.GetComponent<Button>();
        sleepButton.onClick.AddListener(HandleSleepClicked);

        GameObject sleepButtonTextObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        sleepButtonTextObject.transform.SetParent(sleepButtonObject.transform, false);

        RectTransform sleepButtonTextRect = sleepButtonTextObject.GetComponent<RectTransform>();
        sleepButtonTextRect.anchorMin = Vector2.zero;
        sleepButtonTextRect.anchorMax = Vector2.one;
        sleepButtonTextRect.offsetMin = Vector2.zero;
        sleepButtonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI sleepButtonText = sleepButtonTextObject.GetComponent<TextMeshProUGUI>();
        sleepButtonText.font = _fontAsset;
        sleepButtonText.fontSize = 18;
        sleepButtonText.color = Color.white;
        sleepButtonText.alignment = TextAlignmentOptions.Center;
        sleepButtonText.text = "Sleep";
    }

    private void HandleSleepClicked()
    {
        _staminaSystem?.RestoreToMax();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static TMP_FontAsset LoadFontAsset()
    {
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (fontAsset == null)
        {
            Debug.LogWarning("StaminaUI could not load the default TMP font asset. Make sure TextMesh Pro essentials are imported.");
        }

        return fontAsset;
    }
}
