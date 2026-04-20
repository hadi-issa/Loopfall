using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPresentation : MonoBehaviour
{
    [SerializeField] private string startMenuSceneName = "StartMenu";

    private readonly string[] realityMessages =
    {
        "Roll through collapsing architecture. Gather memory fragments. Offer them at shrines. Hold onto what the loop keeps trying to take away.",
        "Every restart preserves knowledge, not comfort. The world decays faster, the path feels less certain, and memory becomes the only map that matters.",
    };

    private readonly string[] denialMessages =
    {
        "A calm, welcoming wandering experience. Follow the golden path, settle in gently, and let the day unfold without fear. Begin here, stay in the light, and ease into something lovely.",
        "Take the gentle route. Nothing urgent asks anything from you here. Just soft colors, kind spaces, and a bright little day that feels easy to trust.",
    };

    private TMP_Text titleText = null!;
    private TMP_Text subtitleText = null!;
    private TMP_Text realityText = null!;
    private TMP_Text comfortTitleText = null!;
    private TMP_Text comfortBodyText = null!;

    private RectTransform leftRoot = null!;
    private RectTransform rightRoot = null!;
    private RectTransform dividerRoot = null!;

    private Image leftPanel = null!;
    private Image rightPanel = null!;
    private Image dividerCore = null!;
    private Image redGhost = null!;
    private Image cyanGhost = null!;
    private RawImage dividerNoise = null!;
    private Image staticOverlay = null!;
    private Image blackoutOverlay = null!;
    private Image tearLeft = null!;
    private Image tearRight = null!;
    private Image dreadPulse = null!;
    private Image shadowVeil = null!;
    private Image rightContaminate = null!;
    private Image[] dividerSlices = null!;
    private float[] dividerSliceWidths = null!;
    private float[] dividerSliceCenters = null!;
    private float[] dividerSliceHeights = null!;
    private Image[] scanlines = null!;
    private RawImage crashNoise = null!;
    private Image crashBlackout = null!;
    private TMP_Text crashText = null!;
    private Image startTransitionBlackout = null!;
    private RawImage startTransitionNoise = null!;
    private TMP_Text startTransitionText = null!;
    private Texture2D crashNoiseTexture = null!;
    private Color32[] crashNoisePixels = null!;
    private bool crashRunning;
    private bool startTransitionRunning;
    private Image[] leftFlashSquares = null!;

    private float phaseDuration = 14f;
    private float glitchStart = 5.8f;
    private float glitchEnd = 7.6f;

    private Texture2D dividerNoiseTexture = null!;
    private Color32[] dividerNoisePixels = null!;
    private float nextNoiseRefreshTime;
    private const int DividerNoiseWidth = 72;
    private const int DividerNoiseHeight = 1024;
    private const int CrashNoiseWidth = 320;
    private const int CrashNoiseHeight = 180;

    private void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != startMenuSceneName)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        RectTransform menuRoot = GameObject.Find("StartMenu")?.GetComponent<RectTransform>();
        RectTransform background = GameObject.Find("Background")?.GetComponent<RectTransform>();
        RectTransform title = GameObject.Find("LoopfallTitle")?.GetComponent<RectTransform>();
        RectTransform buttonGroup = GameObject.Find("ButtonGroup")?.GetComponent<RectTransform>();

        if (canvas == null || menuRoot == null || background == null || title == null || buttonGroup == null)
        {
            return;
        }

        ConfigureCanvas(canvas);
        ConfigureCamera();
        ConfigureBackground(background);
        ConfigureLayout(menuRoot, title, buttonGroup);
        LoopfallAudio.EnsureExists();
        LoopfallMusic.EnsureExists().PlayMenuMusic(true);
    }

    private void Update()
    {
        if (dividerCore == null)
        {
            return;
        }

        float t = Mathf.Repeat(Time.unscaledTime, phaseDuration);
        bool inGlitch = t >= glitchStart && t < glitchEnd;
        bool postGlitch = t >= glitchEnd;
        int messageIndex = (int)(Time.unscaledTime / phaseDuration) % realityMessages.Length;

        realityText.text = realityMessages[messageIndex];
        comfortBodyText.text = inGlitch ? ScrambleText(denialMessages[messageIndex]) : denialMessages[messageIndex];

        float comfortAlpha = postGlitch ? 1f : inGlitch ? 0.68f : 0.88f;
        comfortTitleText.color = new Color(0.99f, 0.95f, 0.86f, Mathf.Lerp(0.52f, 0.95f, comfortAlpha));
        comfortBodyText.color = new Color(0.98f, 0.95f, 0.88f, Mathf.Lerp(0.42f, 0.92f, comfortAlpha));

        float dread = inGlitch ? 1f : Mathf.Lerp(0.18f, 0.42f, Mathf.PerlinNoise(Time.unscaledTime * 0.28f, 0.13f));
        titleText.color = new Color(0.93f, 0.95f, 0.99f, 0.9f + dread * 0.08f);
        subtitleText.color = new Color(0.74f, 0.78f, 0.88f, 0.74f + dread * 0.16f);
        realityText.color = new Color(0.64f, 0.68f, 0.8f, 0.72f + dread * 0.16f);

        if (startTransitionRunning)
        {
            LoopfallAudio.EnsureExists().SetMenuState(inGlitch, crashRunning);
            return;
        }

        AnimateDivider(inGlitch);
        AnimatePanelTearing(inGlitch);
        AnimateLeftFlashSquares(inGlitch);
        AnimateCrashOverlay();
        LoopfallAudio.EnsureExists().SetMenuState(inGlitch, crashRunning);
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigureCamera()
    {
        Camera menuCamera = Camera.main;
        if (menuCamera == null)
        {
            return;
        }

        menuCamera.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);
    }

    private void ConfigureBackground(RectTransform background)
    {
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        Image image = background.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.035f, 0.04f, 0.05f, 0.6f);
            image.raycastTarget = false;
        }
    }

    private void ConfigureLayout(RectTransform menuRoot, RectTransform titleRect, RectTransform buttonGroup)
    {
        menuRoot.anchorMin = Vector2.zero;
        menuRoot.anchorMax = Vector2.one;
        menuRoot.offsetMin = Vector2.zero;
        menuRoot.offsetMax = Vector2.zero;

        leftRoot = GetOrCreateRect(menuRoot, "GloomPanel");
        rightRoot = GetOrCreateRect(menuRoot, "JoyPanel");
        dividerRoot = GetOrCreateRect(menuRoot, "GlitchDivider");
        RectTransform overlayRoot = GetOrCreateRect(menuRoot, "HorrorOverlay");

        leftRoot.SetSiblingIndex(0);
        rightRoot.SetSiblingIndex(1);
        dividerRoot.SetSiblingIndex(2);
        overlayRoot.SetSiblingIndex(3);

        StretchHalf(leftRoot, 0f, 0.5f);
        StretchHalf(rightRoot, 0.5f, 1f);
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        leftPanel = GetOrAddImage(leftRoot.gameObject);
        rightPanel = GetOrAddImage(rightRoot.gameObject);
        leftPanel.color = new Color(0.008f, 0.008f, 0.014f, 0.985f);
        rightPanel.color = new Color(0.48f, 0.43f, 0.29f, 0.8f);
        leftPanel.raycastTarget = false;
        rightPanel.raycastTarget = false;

        ConfigureDivider(dividerRoot);
        ConfigureOverlays(overlayRoot);
        MoveTitle(titleRect, leftRoot);
        MoveButtons(buttonGroup, leftRoot);
        BuildNarrativeTexts(leftRoot);
        BuildComfortInvitePanel(rightRoot);
        BuildComfortTexts(rightRoot);
        ConfigureFakeButtons(rightRoot);
        ConfigureLeftFlashSquares(leftRoot);
    }

    private void MoveTitle(RectTransform titleRect, RectTransform root)
    {
        titleRect.SetParent(root, false);
        titleRect.SetAsLastSibling();
        titleRect.anchorMin = new Vector2(0.12f, 0.68f);
        titleRect.anchorMax = new Vector2(0.96f, 0.84f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        titleText = titleRect.GetComponent<TMP_Text>();
        titleText.font = ResolveMenuFont(titleText.font);
        titleText.text = "LOOPFALL";
        titleText.fontSize = 68f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 2f;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(0.93f, 0.95f, 0.99f, 0.95f);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.raycastTarget = false;

        Transform oldDecoration = titleText.transform.Find("Background");
        if (oldDecoration != null)
        {
            oldDecoration.gameObject.SetActive(false);
        }
    }

    private void MoveButtons(RectTransform buttonGroup, RectTransform root)
    {
        buttonGroup.SetParent(root, false);
        buttonGroup.SetAsLastSibling();
        buttonGroup.anchorMin = new Vector2(0.12f, 0.1f);
        buttonGroup.anchorMax = new Vector2(0.78f, 0.34f);
        buttonGroup.offsetMin = Vector2.zero;
        buttonGroup.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = buttonGroup.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 20f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        ContentSizeFitter fitter = buttonGroup.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        StyleButton("StartButton", "START", new Color(0.08f, 0.22f, 0.26f, 1f), new Color(0.14f, 0.34f, 0.39f, 1f));
        StyleButton("RebootButton", "RELAUNCH", new Color(0.16f, 0.08f, 0.07f, 1f), new Color(0.26f, 0.14f, 0.12f, 1f));
        StyleButton("ExitButton", "QUIT", new Color(0.05f, 0.06f, 0.08f, 1f), new Color(0.14f, 0.16f, 0.2f, 1f));
    }

    private void BuildNarrativeTexts(RectTransform root)
    {
        subtitleText = GetOrCreateText(root, "RealitySubtitle");
        subtitleText.font = ResolveMenuFont(subtitleText.font);
        subtitleText.text = "A narrative exploration prototype about memory loss, instability, and the violence of repetition.";
        subtitleText.fontSize = 21f;
        subtitleText.alignment = TextAlignmentOptions.TopLeft;
        subtitleText.color = new Color(0.7f, 0.75f, 0.86f, 0.82f);
        subtitleText.textWrappingMode = TextWrappingModes.Normal;
        subtitleText.raycastTarget = false;

        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.12f, 0.48f);
        subtitleRect.anchorMax = new Vector2(0.84f, 0.57f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;

        realityText = GetOrCreateText(root, "RealityBody");
        realityText.font = ResolveMenuFont(realityText.font);
        realityText.text = realityMessages[0];
        realityText.fontSize = 18f;
        realityText.alignment = TextAlignmentOptions.TopLeft;
        realityText.color = new Color(0.58f, 0.62f, 0.74f, 0.82f);
        realityText.textWrappingMode = TextWrappingModes.Normal;
        realityText.raycastTarget = false;

        RectTransform bodyRect = realityText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.12f, 0.2f);
        bodyRect.anchorMax = new Vector2(0.84f, 0.42f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
    }

    private void BuildComfortTexts(RectTransform root)
    {
        comfortTitleText = GetOrCreateText(root, "ComfortTitle");
        comfortTitleText.font = ResolveMenuFont(comfortTitleText.font);
        comfortTitleText.text = "A PERFECTLY LOVELY DAY";
        comfortTitleText.fontSize = 58f;
        comfortTitleText.fontStyle = FontStyles.Bold;
        comfortTitleText.characterSpacing = 1.8f;
        comfortTitleText.alignment = TextAlignmentOptions.BottomLeft;
        comfortTitleText.color = new Color(1f, 0.97f, 0.9f, 0.95f);
        comfortTitleText.raycastTarget = false;

        RectTransform titleRect = comfortTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.12f, 0.63f);
        titleRect.anchorMax = new Vector2(0.92f, 0.82f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        comfortBodyText = GetOrCreateText(root, "ComfortBody");
        comfortBodyText.font = ResolveMenuFont(comfortBodyText.font);
        comfortBodyText.text = denialMessages[0];
        comfortBodyText.fontSize = 24f;
        comfortBodyText.alignment = TextAlignmentOptions.TopLeft;
        comfortBodyText.color = new Color(1f, 0.98f, 0.92f, 0.92f);
        comfortBodyText.textWrappingMode = TextWrappingModes.Normal;
        comfortBodyText.raycastTarget = false;

        RectTransform bodyRect = comfortBodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.12f, 0.4f);
        bodyRect.anchorMax = new Vector2(0.9f, 0.58f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
    }

    private void BuildComfortInvitePanel(RectTransform root)
    {
        Image card = GetOrCreateOverlay(root, "ComfortCard", new Color(0.73f, 0.66f, 0.48f, 0.68f));
        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        cardRect.SetSiblingIndex(0);
        card.raycastTarget = false;

        Image glow = GetOrCreateOverlay(root, "ComfortGlow", new Color(1f, 0.96f, 0.74f, 0.12f));
        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;
        glowRect.SetSiblingIndex(1);
        glow.raycastTarget = false;

        TMP_Text kicker = GetOrCreateText(root, "ComfortKicker");
        kicker.font = ResolveMenuFont(kicker.font);
        kicker.text = "RECOMMENDED FIRST CHOICE";
        kicker.fontSize = 18f;
        kicker.fontStyle = FontStyles.Bold;
        kicker.characterSpacing = 3.2f;
        kicker.alignment = TextAlignmentOptions.BottomLeft;
        kicker.color = new Color(1f, 0.97f, 0.84f, 0.98f);
        kicker.raycastTarget = false;

        RectTransform kickerRect = kicker.rectTransform;
        kickerRect.anchorMin = new Vector2(0.12f, 0.8f);
        kickerRect.anchorMax = new Vector2(0.9f, 0.87f);
        kickerRect.offsetMin = Vector2.zero;
        kickerRect.offsetMax = Vector2.zero;

        TMP_Text prompt = GetOrCreateText(root, "ComfortPrompt");
        prompt.font = ResolveMenuFont(prompt.font);
        prompt.text = "Start somewhere gentle.";
        prompt.fontSize = 28f;
        prompt.fontStyle = FontStyles.Bold;
        prompt.alignment = TextAlignmentOptions.TopLeft;
        prompt.color = new Color(1f, 0.99f, 0.94f, 1f);
        prompt.raycastTarget = false;

        RectTransform promptRect = prompt.rectTransform;
        promptRect.anchorMin = new Vector2(0.12f, 0.56f);
        promptRect.anchorMax = new Vector2(0.9f, 0.64f);
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;
    }

    private void ConfigureDivider(RectTransform root)
    {
        root.anchorMin = new Vector2(0.4935f, 0f);
        root.anchorMax = new Vector2(0.5065f, 1f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        dividerCore = GetOrAddImage(root.gameObject);
        dividerCore.color = new Color(0.005f, 0.005f, 0.008f, 0.98f);
        dividerCore.raycastTarget = false;

        redGhost = GetOrCreateOverlay(root, "RedGhost", new Color(0.82f, 0.05f, 0.05f, 0.14f));
        cyanGhost = GetOrCreateOverlay(root, "CyanGhost", new Color(0.78f, 0.92f, 1f, 0.1f));
        dividerNoise = GetOrCreateNoiseLayer(root, "NoiseField");
        InitializeDividerNoiseTexture();

        dividerSlices = new Image[720];
        dividerSliceWidths = new float[dividerSlices.Length];
        dividerSliceCenters = new float[dividerSlices.Length];
        dividerSliceHeights = new float[dividerSlices.Length];
        for (int i = 0; i < dividerSlices.Length; i++)
        {
            RectTransform slice = GetOrCreateRect(root, $"Slice_{i}");
            float yMin = i / 720f;
            float height = (i % 9 == 0) ? 0.0035f : (i % 5 == 0 ? 0.0024f : 0.00145f);
            dividerSliceHeights[i] = height;
            dividerSliceWidths[i] = 0.04f + ((i * 37) % 100) / 100f * 0.24f;
            dividerSliceCenters[i] = 0.38f + ((i * 53) % 100) / 100f * 0.24f;

            slice.anchorMin = new Vector2(0f, yMin);
            slice.anchorMax = new Vector2(1f, Mathf.Min(1f, yMin + height));
            slice.offsetMin = Vector2.zero;
            slice.offsetMax = Vector2.zero;
            dividerSlices[i] = GetOrAddImage(slice.gameObject);
            dividerSlices[i].raycastTarget = false;
        }
    }

    private void ConfigureOverlays(RectTransform root)
    {
        staticOverlay = GetOrCreateOverlay(root, "StaticOverlay", new Color(1f, 1f, 1f, 0f));
        blackoutOverlay = GetOrCreateOverlay(root, "BlackoutOverlay", new Color(0f, 0f, 0f, 0f));
        tearLeft = GetOrCreateOverlay(root, "TearLeft", new Color(0.95f, 0.08f, 0.08f, 0f));
        tearRight = GetOrCreateOverlay(root, "TearRight", new Color(0.82f, 0.92f, 1f, 0f));
        dreadPulse = GetOrCreateOverlay(root, "DreadPulse", new Color(0.35f, 0.02f, 0.02f, 0f));
        shadowVeil = GetOrCreateOverlay(root, "ShadowVeil", new Color(0f, 0f, 0f, 0.22f));
        rightContaminate = GetOrCreateOverlay(root, "RightContaminate", new Color(0.18f, 0.02f, 0.02f, 0f));

        dreadPulse.rectTransform.anchorMin = new Vector2(0f, 0f);
        dreadPulse.rectTransform.anchorMax = new Vector2(0.62f, 1f);
        dreadPulse.rectTransform.offsetMin = Vector2.zero;
        dreadPulse.rectTransform.offsetMax = Vector2.zero;

        shadowVeil.rectTransform.anchorMin = new Vector2(0f, 0f);
        shadowVeil.rectTransform.anchorMax = new Vector2(1f, 1f);
        shadowVeil.rectTransform.offsetMin = Vector2.zero;
        shadowVeil.rectTransform.offsetMax = Vector2.zero;

        rightContaminate.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rightContaminate.rectTransform.anchorMax = new Vector2(1f, 1f);
        rightContaminate.rectTransform.offsetMin = Vector2.zero;
        rightContaminate.rectTransform.offsetMax = Vector2.zero;

        tearLeft.rectTransform.anchorMin = new Vector2(0.39f, 0f);
        tearLeft.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        tearLeft.rectTransform.offsetMin = Vector2.zero;
        tearLeft.rectTransform.offsetMax = Vector2.zero;

        tearRight.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        tearRight.rectTransform.anchorMax = new Vector2(0.61f, 1f);
        tearRight.rectTransform.offsetMin = Vector2.zero;
        tearRight.rectTransform.offsetMax = Vector2.zero;

        scanlines = new Image[22];
        for (int i = 0; i < scanlines.Length; i++)
        {
            RectTransform line = GetOrCreateRect(root, $"Scanline_{i}");
            line.anchorMin = new Vector2(0f, i / 22f);
            line.anchorMax = new Vector2(1f, i / 22f + 0.01f);
            line.offsetMin = Vector2.zero;
            line.offsetMax = Vector2.zero;
            scanlines[i] = GetOrAddImage(line.gameObject);
            scanlines[i].color = new Color(1f, 1f, 1f, 0f);
            scanlines[i].raycastTarget = false;
        }

        RectTransform crashRoot = GetOrCreateRect(root, "CrashOverlayRoot");
        crashRoot.anchorMin = Vector2.zero;
        crashRoot.anchorMax = Vector2.one;
        crashRoot.offsetMin = Vector2.zero;
        crashRoot.offsetMax = Vector2.zero;
        crashRoot.SetAsLastSibling();

        crashBlackout = GetOrCreateOverlay(crashRoot, "CrashBlackout", new Color(0f, 0f, 0f, 0f));
        crashNoise = GetOrCreateNoiseLayer(crashRoot, "CrashNoise");
        crashNoise.color = new Color(1f, 1f, 1f, 0f);
        crashText = GetOrCreateText(crashRoot, "CrashText");
        crashText.font = ResolveMenuFont(crashText.font);
        crashText.fontSize = 24f;
        crashText.alignment = TextAlignmentOptions.Center;
        crashText.color = new Color(1f, 1f, 1f, 0f);
        crashText.textWrappingMode = TextWrappingModes.Normal;
        crashText.raycastTarget = false;

        RectTransform crashTextRect = crashText.rectTransform;
        crashTextRect.anchorMin = new Vector2(0.18f, 0.38f);
        crashTextRect.anchorMax = new Vector2(0.82f, 0.62f);
        crashTextRect.offsetMin = Vector2.zero;
        crashTextRect.offsetMax = Vector2.zero;

        startTransitionBlackout = GetOrCreateOverlay(crashRoot, "StartTransitionBlackout", new Color(0f, 0f, 0f, 0f));
        startTransitionNoise = GetOrCreateNoiseLayer(crashRoot, "StartTransitionNoise");
        startTransitionNoise.color = new Color(1f, 1f, 1f, 0f);
        startTransitionText = GetOrCreateText(crashRoot, "StartTransitionText");
        startTransitionText.font = ResolveMenuFont(startTransitionText.font);
        startTransitionText.fontSize = 34f;
        startTransitionText.fontStyle = FontStyles.Bold;
        startTransitionText.alignment = TextAlignmentOptions.Center;
        startTransitionText.color = new Color(1f, 1f, 1f, 0f);
        startTransitionText.text = "ENTERING LOOP 01";
        startTransitionText.characterSpacing = 4f;
        startTransitionText.raycastTarget = false;

        RectTransform transitionTextRect = startTransitionText.rectTransform;
        transitionTextRect.anchorMin = new Vector2(0.2f, 0.43f);
        transitionTextRect.anchorMax = new Vector2(0.8f, 0.58f);
        transitionTextRect.offsetMin = Vector2.zero;
        transitionTextRect.offsetMax = Vector2.zero;

        crashNoiseTexture = new Texture2D(CrashNoiseWidth, CrashNoiseHeight, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = "CrashNoiseTexture",
        };
        crashNoisePixels = new Color32[CrashNoiseWidth * CrashNoiseHeight];
        crashNoise.texture = crashNoiseTexture;
    }

    private void AnimateDivider(bool inGlitch)
    {
        RefreshDividerNoise(inGlitch);

        float coreAlpha = inGlitch
            ? 0.9f + Mathf.PingPong(Time.unscaledTime * 14f, 0.1f)
            : 0.76f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.08f);
        dividerCore.color = new Color(0.006f, 0.006f, 0.009f, coreAlpha);

        float redOffset = inGlitch ? Mathf.Sin(Time.unscaledTime * 78f) * 18f : Mathf.Sin(Time.unscaledTime * 11f) * 2.8f;
        float cyanOffset = inGlitch ? Mathf.Cos(Time.unscaledTime * 74f) * -15f : Mathf.Cos(Time.unscaledTime * 10f) * -2.2f;
        redGhost.rectTransform.anchoredPosition = new Vector2(redOffset, 0f);
        cyanGhost.rectTransform.anchoredPosition = new Vector2(cyanOffset, 0f);
        redGhost.color = new Color(0.92f, 0.06f, 0.06f, inGlitch ? 0.35f : 0.1f);
        cyanGhost.color = new Color(0.82f, 0.93f, 1f, inGlitch ? 0.24f : 0.07f);

        for (int i = 0; i < dividerSlices.Length; i++)
        {
            float noise = Mathf.PerlinNoise(i * 0.31f, Time.unscaledTime * (inGlitch ? 140f : 28f));
            float flash = Mathf.PerlinNoise((i + 100) * 0.21f, Time.unscaledTime * (inGlitch ? 260f : 46f));
            float alpha = inGlitch ? Mathf.Lerp(0.05f, 1f, noise) : Mathf.Lerp(0.008f, 0.16f, noise);
            Color color;

            if (flash > 0.88f)
            {
                color = new Color(0.95f, 0.97f, 1f, Mathf.Min(1f, alpha + 0.35f));
            }
            else
            {
                int band = i % 7;
                if (band == 0)
                {
                    color = new Color(0.78f, 0.06f, 0.06f, alpha * 0.78f);
                }
                else if (band == 1)
                {
                    color = new Color(0.72f, 0.82f, 0.92f, alpha * 0.58f);
                }
                else if (band == 2)
                {
                    color = new Color(0.74f, 0.74f, 0.78f, alpha * 0.46f);
                }
                else
                {
                    color = new Color(0.015f, 0.015f, 0.02f, Mathf.Min(1f, alpha + 0.32f));
                }
            }

            dividerSlices[i].color = color;
            RectTransform rect = dividerSlices[i].rectTransform;
            float shift = inGlitch
                ? Mathf.Sin(Time.unscaledTime * (260f + i * 31f)) * (4f + (i % 7) * 0.9f)
                : Mathf.Sin(Time.unscaledTime * (30f + i * 3.8f)) * (0.8f + (i % 5) * 0.12f);

            float width = Mathf.Clamp01(dividerSliceWidths[i] + Mathf.Sin(Time.unscaledTime * (41f + i * 0.7f)) * (inGlitch ? 0.045f : 0.012f));
            float center = Mathf.Clamp01(dividerSliceCenters[i] + Mathf.Sin(Time.unscaledTime * (39f + i * 1.1f)) * (inGlitch ? 0.065f : 0.015f));
            float halfWidth = width * 0.5f;
            rect.anchorMin = new Vector2(Mathf.Clamp01(center - halfWidth), rect.anchorMin.y);
            rect.anchorMax = new Vector2(Mathf.Clamp01(center + halfWidth), rect.anchorMax.y);

            rect.anchoredPosition = new Vector2(shift, 0f);
            rect.localScale = new Vector3(1f, 1f + Mathf.Abs(shift) * 0.0014f, 1f);
        }
    }

    private void AnimatePanelTearing(bool inGlitch)
    {
        float staticAlpha = inGlitch
            ? Mathf.Lerp(0.04f, 0.22f, Mathf.PerlinNoise(Time.unscaledTime * 36f, 0.2f))
            : Mathf.Lerp(0.008f, 0.04f, Mathf.PerlinNoise(Time.unscaledTime * 11f, 0.7f));
        staticOverlay.color = new Color(1f, 1f, 1f, staticAlpha);

        float blackout = inGlitch && Mathf.PerlinNoise(Time.unscaledTime * 85f, 0.9f) > 0.72f ? 0.28f : 0f;
        blackoutOverlay.color = new Color(0f, 0f, 0f, blackout);

        float leftAlpha = inGlitch ? Mathf.Lerp(0.16f, 0.46f, Mathf.PerlinNoise(1.1f, Time.unscaledTime * 41f)) : 0.04f;
        float rightAlpha = inGlitch ? Mathf.Lerp(0.08f, 0.28f, Mathf.PerlinNoise(2.4f, Time.unscaledTime * 37f)) : 0.02f;
        tearLeft.color = new Color(0.95f, 0.08f, 0.08f, leftAlpha);
        tearRight.color = new Color(0.82f, 0.92f, 1f, rightAlpha);
        tearLeft.rectTransform.anchoredPosition = new Vector2(inGlitch ? Mathf.Sin(Time.unscaledTime * 51f) * 20f : 0f, 0f);
        tearRight.rectTransform.anchoredPosition = new Vector2(inGlitch ? Mathf.Cos(Time.unscaledTime * 47f) * -18f : 0f, 0f);

        float pulse = inGlitch
            ? Mathf.Lerp(0.1f, 0.26f, Mathf.PerlinNoise(0.3f, Time.unscaledTime * 18f))
            : Mathf.Lerp(0.02f, 0.08f, Mathf.PerlinNoise(0.8f, Time.unscaledTime * 1.9f));
        dreadPulse.color = new Color(0.45f, 0.02f, 0.02f, pulse);
        rightContaminate.color = new Color(0.15f, 0.01f, 0.01f, inGlitch ? 0.1f : 0.04f);
        shadowVeil.color = new Color(0f, 0f, 0f, inGlitch ? 0.28f : 0.2f);

        for (int i = 0; i < scanlines.Length; i++)
        {
            float lineNoise = Mathf.PerlinNoise(i * 0.31f, Time.unscaledTime * (inGlitch ? 44f : 8f));
            scanlines[i].color = new Color(1f, 1f, 1f, inGlitch ? lineNoise * 0.12f : lineNoise * 0.018f);
            RectTransform lineRect = scanlines[i].rectTransform;
            lineRect.anchoredPosition = new Vector2(inGlitch ? Mathf.Sin(Time.unscaledTime * (35f + i)) * 12f : 0f, 0f);
        }

        if (leftRoot != null)
        {
            leftRoot.anchoredPosition = new Vector2(inGlitch ? Mathf.Sin(Time.unscaledTime * 33f) * 4.5f : Mathf.Sin(Time.unscaledTime * 2.4f) * 0.35f, 0f);
        }

        if (rightRoot != null)
        {
            rightRoot.anchoredPosition = new Vector2(inGlitch ? Mathf.Cos(Time.unscaledTime * 29f) * -3.2f : Mathf.Cos(Time.unscaledTime * 1.8f) * -0.22f, 0f);
        }
    }

    private void ConfigureLeftFlashSquares(RectTransform root)
    {
        leftFlashSquares = new Image[6];
        Vector2[] mins =
        {
            new(0.04f, 0.72f),
            new(0.5f, 0.62f),
            new(0.08f, 0.42f),
            new(0.52f, 0.3f),
            new(0.14f, 0.14f),
            new(0.58f, 0.12f),
        };

        Vector2[] maxs =
        {
            new(0.3f, 0.92f),
            new(0.9f, 0.82f),
            new(0.36f, 0.62f),
            new(0.88f, 0.52f),
            new(0.42f, 0.34f),
            new(0.92f, 0.32f),
        };

        for (int i = 0; i < leftFlashSquares.Length; i++)
        {
            Image square = GetOrCreateOverlay(root, $"LeftFlashSquare_{i}", new Color(0f, 0f, 0f, 0f));
            RectTransform rect = square.rectTransform;
            rect.anchorMin = mins[i];
            rect.anchorMax = maxs[i];
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            square.color = new Color(0f, 0f, 0f, 0f);
            leftFlashSquares[i] = square;
        }
    }

    private void AnimateLeftFlashSquares(bool inGlitch)
    {
        if (leftFlashSquares == null)
        {
            return;
        }

        for (int i = 0; i < leftFlashSquares.Length; i++)
        {
            float noise = Mathf.PerlinNoise(i * 0.93f, Time.unscaledTime * (inGlitch ? 26f : 8f));
            float threshold = inGlitch ? 0.54f : 0.82f;
            float alpha = noise > threshold
                ? Mathf.Lerp(inGlitch ? 0.22f : 0.08f, inGlitch ? 0.56f : 0.22f, noise)
                : 0f;

            leftFlashSquares[i].color = new Color(0f, 0f, 0f, alpha);
        }
    }

    private void ConfigureFakeButtons(RectTransform root)
    {
        RectTransform fakeGroup = GetOrCreateRect(root, "FakeButtonGroup");
        fakeGroup.anchorMin = new Vector2(0.12f, 0.12f);
        fakeGroup.anchorMax = new Vector2(0.78f, 0.38f);
        fakeGroup.offsetMin = Vector2.zero;
        fakeGroup.offsetMax = Vector2.zero;
        fakeGroup.SetAsLastSibling();

        VerticalLayoutGroup layout = fakeGroup.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = fakeGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 20f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = fakeGroup.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = fakeGroup.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateFakeButton(fakeGroup, "FakeStartButton", "START");
        CreateFakeButton(fakeGroup, "FakeRelaunchButton", "RELAUNCH");
        CreateFakeButton(fakeGroup, "FakeQuitButton", "QUIT");
    }

    private void CreateFakeButton(RectTransform parent, string name, string label)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(440f, 82f);

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = 440f;
        layoutElement.preferredWidth = 440f;
        layoutElement.minHeight = 82f;
        layoutElement.preferredHeight = 82f;

        Image image = GetOrAddImage(rect.gameObject);
        image.color = new Color(0.98f, 0.88f, 0.62f, 0.98f);
        image.raycastTarget = true;
        image.type = Image.Type.Simple;

        Button button = rect.GetComponent<Button>();
        if (button == null)
        {
            button = rect.gameObject.AddComponent<Button>();
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.98f, 0.88f, 0.62f, 0.98f);
        colors.highlightedColor = new Color(1f, 0.94f, 0.74f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.8f, 0.66f, 0.38f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(BeginFakeCrash);

        TMP_Text labelText = EnsureButtonLabel(rect);
        labelText.font = ResolveMenuFont(labelText.font);
        labelText.text = label;
        labelText.fontSize = 24f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.26f, 0.16f, 0.04f, 1f);
        labelText.raycastTarget = false;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Overflow;

        MenuButtonAnimator animator = rect.GetComponent<MenuButtonAnimator>();
        if (animator == null)
        {
            animator = rect.gameObject.AddComponent<MenuButtonAnimator>();
        }

        animator.Configure(colors.normalColor, colors.highlightedColor);
    }

    private void BeginFakeCrash()
    {
        if (!crashRunning && !startTransitionRunning)
        {
            LoopfallAudio.EnsureExists().TriggerCrashSting();
            StartCoroutine(FakeCrashRoutine());
        }
    }

    public IEnumerator PlayLoopfallStartTransition()
    {
        if (startTransitionRunning)
        {
            yield break;
        }

        startTransitionRunning = true;
        SetAllButtonsInteractable(false);

        LoopfallAudio audio = LoopfallAudio.EnsureExists();
        float duration = 1.45f;
        float timer = 0f;
        audio.BeginLoopfallStartTransition(duration);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(timer / duration);
            float eased = normalized * normalized * (3f - 2f * normalized);
            float pulse = Mathf.PingPong(timer * 7.5f, 1f);

            startTransitionBlackout.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.08f, 0.96f, eased));
            startTransitionNoise.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.02f, 0.24f, pulse * eased));
            startTransitionText.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normalized - 0.12f) / 0.28f));
            startTransitionText.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(timer * 30f) * (1f + pulse * 9f), Mathf.Cos(timer * 21f) * 1.5f);

            leftRoot.anchoredPosition = new Vector2(-Mathf.Lerp(0f, 26f, eased), 0f);
            rightRoot.anchoredPosition = new Vector2(Mathf.Lerp(0f, 18f, eased), 0f);
            dividerRoot.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.8f, 1f, 1f), eased);
            dreadPulse.color = new Color(0.55f, 0.02f, 0.02f, Mathf.Lerp(0.08f, 0.38f, eased));
            shadowVeil.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.22f, 0.55f, eased));
            staticOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.02f, 0.12f, pulse * eased));
            blackoutOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.24f, eased));

            if (normalized > 0.38f && UnityEngine.Random.value > 0.86f)
            {
                audio.PlayUi(LoopfallCue.MenuGlitchTick, UnityEngine.Random.Range(0.05f, 0.11f), 0.02f);
            }

            yield return null;
        }

        audio.PlayUi(LoopfallCue.MenuGlitchBurst, 0.12f, 0.02f);
        startTransitionBlackout.color = new Color(0f, 0f, 0f, 1f);
        startTransitionNoise.color = new Color(1f, 1f, 1f, 0.16f);
        startTransitionText.alpha = 1f;
        startTransitionText.rectTransform.anchoredPosition = Vector2.zero;
        yield return new WaitForSecondsRealtime(0.18f);
    }

    private IEnumerator FakeCrashRoutine()
    {
        crashRunning = true;
        LoopfallAudio audio = LoopfallAudio.EnsureExists();

        string[] crashMessages =
        {
            "Loopfall.exe has stopped responding.",
            "Unhandled exception at 0x00000000.",
            "Graphics device removed unexpectedly.",
            "Fatal memory access violation.",
            "Display driver stopped responding.",
            "Read from protected memory failed.",
        };

        float buildup = Random.Range(0.18f, 0.52f);
        float freeze = Random.Range(0.85f, 1.85f);
        float blackout = Random.Range(0.45f, 0.95f);
        bool hardFlash = Random.value > 0.45f;
        crashText.text = crashMessages[Random.Range(0, crashMessages.Length)];
        audio.BeginCrashFlicker(buildup * 0.92f);

        while (buildup > 0f)
        {
            buildup -= Time.unscaledDeltaTime;
            staticOverlay.color = new Color(1f, 1f, 1f, Random.Range(0.22f, 0.58f));
            blackoutOverlay.color = new Color(0f, 0f, 0f, Random.Range(0.08f, 0.38f));
            tearLeft.color = new Color(1f, 0.08f, 0.08f, Random.Range(0.24f, 0.68f));
            tearRight.color = new Color(0.82f, 0.92f, 1f, Random.Range(0.18f, 0.5f));
            ApplyCrashShake(10f, 9f, 18f, 10f, 1f);

            if (hardFlash && Random.value > 0.76f)
            {
                crashBlackout.color = new Color(1f, 1f, 1f, Random.Range(0.08f, 0.22f));
            }
            yield return null;
        }

        audio.StopActiveMusicAbruptly();
        crashBlackout.color = new Color(0f, 0f, 0f, 0.96f);
        crashNoise.color = new Color(1f, 1f, 1f, 0.72f);
        crashText.color = new Color(1f, 1f, 1f, 0.95f);

        while (freeze > 0f)
        {
            freeze -= Time.unscaledDeltaTime;
            crashBlackout.color = new Color(0f, 0f, 0f, Random.Range(0.9f, 1f));
            crashNoise.color = new Color(1f, 1f, 1f, Random.Range(0.5f, 0.9f));
            crashText.alpha = Random.Range(0.35f, 1f);
            staticOverlay.color = new Color(1f, 1f, 1f, Random.Range(0.24f, 0.56f));
            blackoutOverlay.color = new Color(0f, 0f, 0f, Random.Range(0.3f, 0.62f));
            tearLeft.color = new Color(1f, 0.08f, 0.08f, Random.Range(0.12f, 0.4f));
            tearRight.color = new Color(0.82f, 0.92f, 1f, Random.Range(0.1f, 0.34f));
            ApplyCrashShake(12f, 11f, 12f, 7f, 1.15f);
            yield return null;
        }

        audio.PlayUi(LoopfallCue.MenuGlitchBurst, 0.09f, 0.03f);
        crashNoise.color = new Color(1f, 1f, 1f, 0.42f);
        crashBlackout.color = new Color(0f, 0f, 0f, 0.92f);
        yield return new WaitForSecondsRealtime(0.045f);

        crashBlackout.color = new Color(0f, 0f, 0f, 1f);
        crashNoise.color = new Color(1f, 1f, 1f, 0.96f);
        crashText.alpha = 1f;
        crashText.rectTransform.anchoredPosition = Vector2.zero;
        float blackoutTimer = blackout;
        while (blackoutTimer > 0f)
        {
            blackoutTimer -= Time.unscaledDeltaTime;
            ApplyCrashShake(13f, 12f, 10f, 6f, 1.2f);
            yield return null;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void AnimateCrashOverlay()
    {
        if (!crashRunning || crashNoiseTexture == null || crashNoisePixels == null)
        {
            return;
        }

        for (int y = 0; y < CrashNoiseHeight; y++)
        {
            for (int x = 0; x < CrashNoiseWidth; x++)
            {
                float n = Mathf.PerlinNoise((x + Time.unscaledTime * 500f) * 0.07f, (y + Time.unscaledTime * 300f) * 0.11f);
                byte v = (byte)Mathf.Lerp(16f, 255f, n);
                crashNoisePixels[y * CrashNoiseWidth + x] = new Color32(v, v, v, 255);
            }
        }

        crashNoiseTexture.SetPixels32(crashNoisePixels);
        crashNoiseTexture.Apply(false, false);
    }

    private void StyleButton(string objectName, string label, Color normalColor, Color highlightColor)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
        {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(340f, 76f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = 340f;
        layoutElement.preferredWidth = 340f;
        layoutElement.minHeight = 76f;
        layoutElement.preferredHeight = 76f;

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = normalColor;
            image.raycastTarget = true;
            image.type = Image.Type.Simple;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightColor;
            colors.selectedColor = highlightColor;
            colors.pressedColor = Color.Lerp(highlightColor, Color.black, 0.16f);
            colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.4f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
        }

        TMP_Text buttonLabel = EnsureButtonLabel(rect);
        buttonLabel.font = ResolveMenuFont(buttonLabel.font);
        buttonLabel.text = label;
        buttonLabel.fontSize = 21f;
        buttonLabel.fontStyle = FontStyles.Bold;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.color = new Color(0.95f, 0.97f, 1f, 1f);
        buttonLabel.raycastTarget = false;
        buttonLabel.textWrappingMode = TextWrappingModes.NoWrap;
        buttonLabel.overflowMode = TextOverflowModes.Overflow;

        MenuButtonAnimator animator = buttonObject.GetComponent<MenuButtonAnimator>();
        if (animator == null)
        {
            animator = buttonObject.AddComponent<MenuButtonAnimator>();
        }

        animator.Configure(normalColor, highlightColor);
    }

    private TMP_Text EnsureButtonLabel(RectTransform buttonRect)
    {
        Text legacyText = buttonRect.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.enabled = false;
        }

        TMP_Text label = buttonRect.Find("TMPLabel")?.GetComponent<TMP_Text>();
        if (label == null)
        {
            GameObject labelObject = new("TMPLabel", typeof(RectTransform));
            labelObject.transform.SetParent(buttonRect, false);
            label = labelObject.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return label;
    }

    private RectTransform GetOrCreateRect(RectTransform parent, string name)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect != null)
        {
            return rect;
        }

        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private TMP_Text GetOrCreateText(RectTransform parent, string name)
    {
        TMP_Text text = parent.Find(name)?.GetComponent<TMP_Text>();
        if (text != null)
        {
            return text;
        }

        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<TextMeshProUGUI>();
    }

    private Image GetOrAddImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        return image != null ? image : target.AddComponent<Image>();
    }

    private Image GetOrCreateOverlay(RectTransform parent, string name, Color color)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        Image image = GetOrAddImage(rect.gameObject);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private RawImage GetOrCreateNoiseLayer(RectTransform parent, string name)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        RawImage image = rect.GetComponent<RawImage>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<RawImage>();
        }

        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0.95f);
        return image;
    }

    private void StretchHalf(RectTransform rect, float minX, float maxX)
    {
        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(maxX, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private string ScrambleText(string source)
    {
        char[] chars = source.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ' ' || chars[i] == '.' || chars[i] == ',')
            {
                continue;
            }

            if (Random.value < 0.24f)
            {
                string glyphPair = RandomGlyphs();
                chars[i] = glyphPair[Random.Range(0, glyphPair.Length)];
            }
        }

        return new string(chars);
    }

    private void ApplyCrashShake(float leftMagnitude, float rightMagnitude, float textX, float textY, float dividerScaleBoost)
    {
        leftRoot.anchoredPosition = new Vector2(Random.Range(-leftMagnitude, leftMagnitude), Random.Range(-3f, 3f));
        rightRoot.anchoredPosition = new Vector2(Random.Range(-rightMagnitude, rightMagnitude), Random.Range(-3f, 3f));
        crashText.rectTransform.anchoredPosition = new Vector2(Random.Range(-textX, textX), Random.Range(-textY, textY));
        dividerRoot.localScale = new Vector3(Random.Range(1f, dividerScaleBoost), 1f, 1f);
    }

    private string RandomGlyphs()
    {
        const string glyphs = "#/:;[]|";
        return $"{glyphs[Random.Range(0, glyphs.Length)]}{glyphs[Random.Range(0, glyphs.Length)]}";
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            button.interactable = interactable;
        }
    }

    private void InitializeDividerNoiseTexture()
    {
        if (dividerNoise != null && dividerNoiseTexture == null)
        {
            dividerNoiseTexture = new Texture2D(DividerNoiseWidth, DividerNoiseHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                name = "DividerNoiseTexture",
            };

            dividerNoisePixels = new Color32[DividerNoiseWidth * DividerNoiseHeight];
            dividerNoise.texture = dividerNoiseTexture;
        }
    }

    private void RefreshDividerNoise(bool inGlitch)
    {
        if (dividerNoiseTexture == null || dividerNoisePixels == null)
        {
            return;
        }

        float refreshInterval = inGlitch ? 0.012f : 0.04f;
        if (Time.unscaledTime < nextNoiseRefreshTime)
        {
            return;
        }

        nextNoiseRefreshTime = Time.unscaledTime + refreshInterval;

        Color32 dark = new(5, 5, 8, 210);
        Color32 dim = new(18, 18, 24, 230);
        Color32 white = new(240, 242, 248, 255);
        Color32 red = new(160, 18, 18, 230);
        Color32 cyan = new(150, 200, 235, 210);

        for (int y = 0; y < DividerNoiseHeight; y++)
        {
            float rowNoise = Mathf.PerlinNoise(y * 0.027f, Time.unscaledTime * (inGlitch ? 28f : 6f));
            int rowShift = Mathf.RoundToInt((rowNoise - 0.5f) * (inGlitch ? 18f : 5f));
            int bandSeed = (y * 17) % 97;

            for (int x = 0; x < DividerNoiseWidth; x++)
            {
                int warpedX = Mathf.Clamp(x + rowShift, 0, DividerNoiseWidth - 1);
                float n = Mathf.PerlinNoise((warpedX + bandSeed) * 0.11f, y * 0.043f + Time.unscaledTime * (inGlitch ? 16f : 3.2f));
                float flash = Mathf.PerlinNoise((warpedX + 33) * 0.19f, y * 0.09f + Time.unscaledTime * (inGlitch ? 41f : 8f));
                int idx = y * DividerNoiseWidth + x;

                if (flash > (inGlitch ? 0.79f : 0.93f))
                {
                    dividerNoisePixels[idx] = white;
                }
                else if (n > 0.82f)
                {
                    dividerNoisePixels[idx] = red;
                }
                else if (n > 0.68f)
                {
                    dividerNoisePixels[idx] = cyan;
                }
                else if (n > 0.42f)
                {
                    dividerNoisePixels[idx] = dim;
                }
                else
                {
                    dividerNoisePixels[idx] = dark;
                }
            }
        }

        dividerNoiseTexture.SetPixels32(dividerNoisePixels);
        dividerNoiseTexture.Apply(false, false);
    }

    private TMP_FontAsset ResolveMenuFont(TMP_FontAsset fallback)
    {
        TMP_FontAsset tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return tmpFont != null ? tmpFont : fallback;
    }
}
