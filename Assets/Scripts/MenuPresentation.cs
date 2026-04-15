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
        "A gentle wandering game. Nothing breaks. Nothing slips away. Every room is bright, every memory is safe, and everyone finds their way home.",
        "Just keep smiling and follow the lovely path. There is no collapse, no repetition, no loss. Only a perfect day that never changes.",
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
    private Image[] dividerSlices = null!;
    private float[] dividerSliceWidths = null!;
    private float[] dividerSliceCenters = null!;
    private float[] dividerSliceHeights = null!;
    private Image[] scanlines = null!;
    private RawImage crashNoise = null!;
    private Image crashBlackout = null!;
    private TMP_Text crashText = null!;
    private Texture2D crashNoiseTexture = null!;
    private Color32[] crashNoisePixels = null!;
    private bool crashRunning;
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

        float comfortAlpha = postGlitch ? 1f : inGlitch ? 0.55f : 0.18f;
        comfortTitleText.color = new Color(0.84f, 0.8f, 0.72f, Mathf.Lerp(0.22f, 0.9f, comfortAlpha));
        comfortBodyText.color = new Color(0.84f, 0.8f, 0.72f, comfortAlpha);

        AnimateDivider(inGlitch);
        AnimatePanelTearing(inGlitch);
        AnimateLeftFlashSquares(inGlitch);
        AnimateCrashOverlay();
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
        leftPanel.color = new Color(0.015f, 0.02f, 0.03f, 0.9f);
        rightPanel.color = new Color(0.12f, 0.11f, 0.08f, 0.55f);
        leftPanel.raycastTarget = false;
        rightPanel.raycastTarget = false;

        ConfigureDivider(dividerRoot);
        ConfigureOverlays(overlayRoot);
        MoveTitle(titleRect, leftRoot);
        MoveButtons(buttonGroup, leftRoot);
        BuildNarrativeTexts(leftRoot);
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
        titleText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
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
        buttonGroup.anchorMax = new Vector2(0.9f, 0.29f);
        buttonGroup.offsetMin = Vector2.zero;
        buttonGroup.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = buttonGroup.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 18f;
            layout.childControlWidth = false;
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
        subtitleText.color = new Color(0.82f, 0.85f, 0.9f, 1f);
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
        realityText.color = new Color(0.68f, 0.73f, 0.8f, 1f);
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
        comfortTitleText.fontSize = 44f;
        comfortTitleText.fontStyle = FontStyles.Bold;
        comfortTitleText.characterSpacing = 2f;
        comfortTitleText.alignment = TextAlignmentOptions.BottomLeft;
        comfortTitleText.color = new Color(0.84f, 0.8f, 0.72f, 0.22f);
        comfortTitleText.raycastTarget = false;

        RectTransform titleRect = comfortTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.14f, 0.62f);
        titleRect.anchorMax = new Vector2(0.84f, 0.76f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        comfortBodyText = GetOrCreateText(root, "ComfortBody");
        comfortBodyText.font = ResolveMenuFont(comfortBodyText.font);
        comfortBodyText.text = denialMessages[0];
        comfortBodyText.fontSize = 20f;
        comfortBodyText.alignment = TextAlignmentOptions.TopLeft;
        comfortBodyText.color = new Color(0.84f, 0.8f, 0.72f, 0.18f);
        comfortBodyText.textWrappingMode = TextWrappingModes.Normal;
        comfortBodyText.raycastTarget = false;

        RectTransform bodyRect = comfortBodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.14f, 0.34f);
        bodyRect.anchorMax = new Vector2(0.8f, 0.56f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
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
            ? Mathf.Lerp(0.02f, 0.16f, Mathf.PerlinNoise(Time.unscaledTime * 36f, 0.2f))
            : Mathf.Lerp(0f, 0.025f, Mathf.PerlinNoise(Time.unscaledTime * 11f, 0.7f));
        staticOverlay.color = new Color(1f, 1f, 1f, staticAlpha);

        float blackout = inGlitch && Mathf.PerlinNoise(Time.unscaledTime * 85f, 0.9f) > 0.78f ? 0.22f : 0f;
        blackoutOverlay.color = new Color(0f, 0f, 0f, blackout);

        float leftAlpha = inGlitch ? Mathf.Lerp(0.08f, 0.3f, Mathf.PerlinNoise(1.1f, Time.unscaledTime * 41f)) : 0.015f;
        float rightAlpha = inGlitch ? Mathf.Lerp(0.06f, 0.22f, Mathf.PerlinNoise(2.4f, Time.unscaledTime * 37f)) : 0.01f;
        tearLeft.color = new Color(0.95f, 0.08f, 0.08f, leftAlpha);
        tearRight.color = new Color(0.82f, 0.92f, 1f, rightAlpha);
        tearLeft.rectTransform.anchoredPosition = new Vector2(inGlitch ? Mathf.Sin(Time.unscaledTime * 51f) * 20f : 0f, 0f);
        tearRight.rectTransform.anchoredPosition = new Vector2(inGlitch ? Mathf.Cos(Time.unscaledTime * 47f) * -18f : 0f, 0f);

        for (int i = 0; i < scanlines.Length; i++)
        {
            float lineNoise = Mathf.PerlinNoise(i * 0.31f, Time.unscaledTime * (inGlitch ? 44f : 8f));
            scanlines[i].color = new Color(1f, 1f, 1f, inGlitch ? lineNoise * 0.08f : lineNoise * 0.01f);
            RectTransform lineRect = scanlines[i].rectTransform;
            lineRect.anchoredPosition = new Vector2(inGlitch ? Mathf.Sin(Time.unscaledTime * (35f + i)) * 12f : 0f, 0f);
        }

        if (leftRoot != null)
        {
            leftRoot.anchoredPosition = new Vector2(inGlitch ? Mathf.Sin(Time.unscaledTime * 33f) * 3f : 0f, 0f);
        }

        if (rightRoot != null)
        {
            rightRoot.anchoredPosition = new Vector2(inGlitch ? Mathf.Cos(Time.unscaledTime * 29f) * -2.6f : 0f, 0f);
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
            float threshold = inGlitch ? 0.62f : 0.9f;
            float alpha = noise > threshold
                ? Mathf.Lerp(inGlitch ? 0.12f : 0.05f, inGlitch ? 0.38f : 0.14f, noise)
                : 0f;

            leftFlashSquares[i].color = new Color(0f, 0f, 0f, alpha);
        }
    }

    private void ConfigureFakeButtons(RectTransform root)
    {
        RectTransform fakeGroup = GetOrCreateRect(root, "FakeButtonGroup");
        fakeGroup.anchorMin = new Vector2(0.14f, 0.1f);
        fakeGroup.anchorMax = new Vector2(0.84f, 0.28f);
        fakeGroup.offsetMin = Vector2.zero;
        fakeGroup.offsetMax = Vector2.zero;
        fakeGroup.SetAsLastSibling();

        VerticalLayoutGroup layout = fakeGroup.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = fakeGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 14f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateFakeButton(fakeGroup, "FakeStartButton", "START");
        CreateFakeButton(fakeGroup, "FakeRelaunchButton", "RELAUNCH");
        CreateFakeButton(fakeGroup, "FakeQuitButton", "QUIT");
    }

    private void CreateFakeButton(RectTransform parent, string name, string label)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(320f, 60f);

        Image image = GetOrAddImage(rect.gameObject);
        image.color = new Color(0.34f, 0.31f, 0.24f, 0.72f);
        image.raycastTarget = true;

        Button button = rect.GetComponent<Button>();
        if (button == null)
        {
            button = rect.gameObject.AddComponent<Button>();
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.34f, 0.31f, 0.24f, 0.72f);
        colors.highlightedColor = new Color(0.48f, 0.43f, 0.31f, 0.82f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.24f, 0.2f, 0.14f, 0.92f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(BeginFakeCrash);

        TMP_Text labelText = EnsureButtonLabel(rect);
        labelText.font = ResolveMenuFont(labelText.font);
        labelText.text = label;
        labelText.fontSize = 22f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.98f, 0.96f, 0.9f, 0.88f);
        labelText.raycastTarget = false;
    }

    private void BeginFakeCrash()
    {
        if (!crashRunning)
        {
            StartCoroutine(FakeCrashRoutine());
        }
    }

    private IEnumerator FakeCrashRoutine()
    {
        crashRunning = true;

        string[] crashMessages =
        {
            "Loopfall.exe has stopped responding.",
            "Unhandled exception at 0x00000000.",
            "Graphics device removed unexpectedly.",
            "Fatal memory access violation.",
        };

        float buildup = Random.Range(0.55f, 1.2f);
        float freeze = Random.Range(0.45f, 1.1f);
        float blackout = Random.Range(0.2f, 0.45f);
        crashText.text = crashMessages[Random.Range(0, crashMessages.Length)];

        while (buildup > 0f)
        {
            buildup -= Time.unscaledDeltaTime;
            staticOverlay.color = new Color(1f, 1f, 1f, Random.Range(0.08f, 0.25f));
            blackoutOverlay.color = new Color(0f, 0f, 0f, Random.Range(0f, 0.18f));
            tearLeft.color = new Color(1f, 0.08f, 0.08f, Random.Range(0.1f, 0.35f));
            tearRight.color = new Color(0.82f, 0.92f, 1f, Random.Range(0.08f, 0.28f));
            yield return null;
        }

        crashBlackout.color = new Color(0f, 0f, 0f, 0.9f);
        crashNoise.color = new Color(1f, 1f, 1f, 0.4f);
        crashText.color = new Color(1f, 1f, 1f, 0.95f);

        while (freeze > 0f)
        {
            freeze -= Time.unscaledDeltaTime;
            crashBlackout.color = new Color(0f, 0f, 0f, Random.Range(0.82f, 0.98f));
            crashNoise.color = new Color(1f, 1f, 1f, Random.Range(0.22f, 0.52f));
            crashText.alpha = Random.Range(0.65f, 1f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(blackout);

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
        rect.sizeDelta = new Vector2(320f, 64f);

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = normalColor;
            image.raycastTarget = true;
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
        buttonLabel.fontSize = 22f;
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
                chars[i] = RandomGlyphs()[Random.Range(0, 4)];
            }
        }

        return new string(chars);
    }

    private string RandomGlyphs()
    {
        const string glyphs = "#/:;[]|";
        return $"{glyphs[Random.Range(0, glyphs.Length)]}{glyphs[Random.Range(0, glyphs.Length)]}";
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
