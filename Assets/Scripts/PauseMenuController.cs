using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; } = null!;

    private CanvasGroup canvasGroup = null!;
    private RectTransform panel = null!;
    private TMP_Text loopText = null!;
    private Button resumeButton = null!;
    private Button menuButton = null!;
    private Button quitButton = null!;
    private bool visible;

    public static PauseMenuController EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        PauseMenuController existing = FindFirstObjectByType<PauseMenuController>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject menuObject = new("LoopfallPauseMenu");
        Instance = menuObject.AddComponent<PauseMenuController>();
        DontDestroyOnLoad(menuObject);
        return Instance;
    }

    public void Bind(GameManager manager)
    {
        if (manager == null)
        {
            return;
        }

        resumeButton.onClick.RemoveAllListeners();
        menuButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();

        resumeButton.onClick.AddListener(manager.ResumeGame);
        menuButton.onClick.AddListener(manager.ReturnToStartMenu);
        quitButton.onClick.AddListener(manager.ExitGame);
    }

    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
        gameObject.SetActive(true);
        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
        panel.gameObject.SetActive(isVisible);

        if (!isVisible)
        {
            return;
        }

        UpdateLoopText();
        EnsureEventSystem();
        EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildMenu();
        SetVisible(false);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null!;
        }
    }

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        UpdateLoopText();
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.5f;
        panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one * (0.985f + pulse * 0.015f), 5f * Time.unscaledDeltaTime);
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        if (scene.name != Game.Config.MainSceneName)
        {
            SetVisible(false);
        }
    }

    private void BuildMenu()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image veil = CreateImage("SlowTimeVeil", transform, new Color(0.012f, 0.017f, 0.022f, 0.72f));
        Stretch(veil.rectTransform);

        Image leftBand = CreateImage("PauseLeftBand", transform, new Color(0.4f, 0.54f, 0.46f, 0.2f));
        leftBand.rectTransform.anchorMin = new Vector2(0f, 0f);
        leftBand.rectTransform.anchorMax = new Vector2(0.2f, 1f);
        leftBand.rectTransform.offsetMin = Vector2.zero;
        leftBand.rectTransform.offsetMax = Vector2.zero;
        leftBand.raycastTarget = false;

        Image panelImage = CreateImage("PausePanel", transform, new Color(0.055f, 0.07f, 0.075f, 0.94f));
        panel = panelImage.rectTransform;
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(620f, 620f);
        panel.anchoredPosition = Vector2.zero;

        TMP_FontAsset font = ResolveFont();
        TMP_Text title = CreateText("PauseTitle", panel, "PAUSED", 58f, FontStyles.Bold, new Color(0.92f, 0.96f, 0.9f, 1f), font);
        title.alignment = TextAlignmentOptions.BottomLeft;
        title.characterSpacing = 2.5f;
        title.rectTransform.anchorMin = new Vector2(0.11f, 0.76f);
        title.rectTransform.anchorMax = new Vector2(0.9f, 0.91f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        TMP_Text subtitle = CreateText("PauseSubtitle", panel, "The loop keeps breathing at slow speed.", 22f, FontStyles.Normal, new Color(0.68f, 0.77f, 0.72f, 0.92f), font);
        subtitle.alignment = TextAlignmentOptions.TopLeft;
        subtitle.rectTransform.anchorMin = new Vector2(0.11f, 0.67f);
        subtitle.rectTransform.anchorMax = new Vector2(0.88f, 0.75f);
        subtitle.rectTransform.offsetMin = Vector2.zero;
        subtitle.rectTransform.offsetMax = Vector2.zero;

        loopText = CreateText("PauseLoopState", panel, string.Empty, 20f, FontStyles.Bold, new Color(0.84f, 0.88f, 0.78f, 0.95f), font);
        loopText.alignment = TextAlignmentOptions.Left;
        loopText.characterSpacing = 1.2f;
        loopText.rectTransform.anchorMin = new Vector2(0.11f, 0.58f);
        loopText.rectTransform.anchorMax = new Vector2(0.88f, 0.64f);
        loopText.rectTransform.offsetMin = Vector2.zero;
        loopText.rectTransform.offsetMax = Vector2.zero;

        RectTransform buttonRoot = new GameObject("PauseButtons", typeof(RectTransform)).GetComponent<RectTransform>();
        buttonRoot.SetParent(panel, false);
        buttonRoot.anchorMin = new Vector2(0.11f, 0.12f);
        buttonRoot.anchorMax = new Vector2(0.89f, 0.55f);
        buttonRoot.offsetMin = Vector2.zero;
        buttonRoot.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = buttonRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 26f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        resumeButton = CreateButton(buttonRoot, "Resume", "RESUME");
        menuButton = CreateButton(buttonRoot, "ReturnToMenu", "RETURN TO MENU");
        quitButton = CreateButton(buttonRoot, "Quit", "QUIT");
    }

    private Button CreateButton(RectTransform parent, string name, string label)
    {
        Image image = CreateImage(name, parent, new Color(0.18f, 0.25f, 0.22f, 0.96f));
        RectTransform rect = image.rectTransform;
        rect.sizeDelta = new Vector2(0f, 84f);

        LayoutElement layoutElement = image.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 84f;
        layoutElement.preferredHeight = 84f;
        layoutElement.flexibleHeight = 0f;

        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.35f, 0.48f, 0.38f, 1f);
        colors.pressedColor = new Color(0.5f, 0.68f, 0.48f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        MenuButtonAnimator animator = image.gameObject.AddComponent<MenuButtonAnimator>();
        animator.Configure(colors.normalColor, colors.highlightedColor);

        TMP_Text text = CreateText("Label", rect, label, 25f, FontStyles.Bold, new Color(0.94f, 0.96f, 0.9f, 1f), ResolveFont());
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.characterSpacing = 2f;
        text.rectTransform.anchorMin = new Vector2(0.08f, 0f);
        text.rectTransform.anchorMax = new Vector2(0.96f, 1f);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        return button;
    }

    private void UpdateLoopText()
    {
        int loop = LoopManager.Instance != null ? LoopManager.Instance.CurrentLoopIndex : 1;
        loopText.text = $"LOOP LEVEL {loop:00} / {Game.Config.MaxLoops:00}    WORLD SPEED: {Game.Config.PauseTimeScale * 100f:0}%";
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, float size, FontStyles style, Color color, TMP_FontAsset font)
    {
        GameObject textObject = new(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_FontAsset ResolveFont()
    {
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
