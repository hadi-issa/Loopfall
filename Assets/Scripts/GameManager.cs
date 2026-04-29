using System.Diagnostics;
using System.IO;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    private const float PauseToggleDebounce = 0.18f;

    [SerializeField] private KeyCode resetLoopKey = KeyCode.R;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode debugNextLoopKey = KeyCode.L;
    [SerializeField] private string loopfallSceneName = Config.MainSceneName;

    private bool pauseSlowed;
    private bool startSequenceRunning;
    private bool loopAdvanceSequenceRunning;
    private Player player = null!;
    private PauseMenuController pauseMenu = null!;
    private float nextPauseToggleTime;

    public static bool IsGameplayPaused { get; private set; }

    private void Awake()
    {
        MemoryManager.EnsureExists();
        LoopManager.EnsureExists();
        LoopfallAudio.EnsureExists();
        LoopfallMusic.EnsureExists().RouteForActiveScene(true);
        pauseMenu = PauseMenuController.EnsureExists();
        pauseMenu.Bind(this);
        SetPaused(false);
        ApplyCursorStateForActiveScene();
    }

    private void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        if (SceneManager.GetActiveScene().name != loopfallSceneName)
        {
            if (pauseSlowed)
            {
                SetPaused(false);
            }

            ReleaseCursorForMenus();
            return;
        }

        if (Input.GetKeyDown(resetLoopKey))
        {
            SetPaused(false);
            LoopManager.Instance.RestartCurrentLoop("Manual restart");
        }

        if (Input.GetKeyDown(debugNextLoopKey))
        {
            if (!loopAdvanceSequenceRunning)
            {
                StartCoroutine(DebugSwitchToNextLoopSequence());
            }

            return;
        }

        if (Input.GetKeyDown(pauseKey) && Time.unscaledTime >= nextPauseToggleTime)
        {
            SetPaused(!pauseSlowed);
        }

        if (!loopAdvanceSequenceRunning && MemoryManager.Instance.TotalFragments >= Config.FragmentsRequiredForLoopAdvance)
        {
            StartCoroutine(AdvanceLoopSequence());
            return;
        }

        if (player != null && player.transform.position.y < Config.LoopFallHeight)
        {
            SetPaused(false);
            LoopManager.Instance.RestartCurrentLoop("Fell from the world");
        }
    }

    public void StartGame()
    {
        if (startSequenceRunning)
        {
            return;
        }

        SetPaused(false);

        if (SceneManager.GetActiveScene().name == "StartMenu")
        {
            StartCoroutine(StartGameSequence());
            return;
        }

        SceneManager.LoadScene(loopfallSceneName);
    }

    private System.Collections.IEnumerator AdvanceLoopSequence()
    {
        int nextLoop = Mathf.Min(LoopManager.Instance.CurrentLoopIndex + 1, Config.MaxLoops);
        yield return RunLoopTransitionSequence(nextLoop, "All memory fragments collected");
    }

    private System.Collections.IEnumerator RunLoopTransitionSequence(int nextLoop, string reason)
    {
        loopAdvanceSequenceRunning = true;
        SetPaused(false);
        ClearGameplayInputBuffers();

        Canvas overlayCanvas = CreateLoopTransitionOverlay(
            out RectTransform leftPanel,
            out RectTransform rightPanel,
            out RectTransform divider,
            out Image dreadPulse,
            out Image shadowVeil,
            out Image staticOverlay,
            out Image blackout,
            out RawImage noise,
            out TMP_Text label);
        label.text = $"ENTERING LOOP {nextLoop:00}";

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

            blackout.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.08f, 0.96f, eased));
            noise.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.02f, 0.24f, pulse * eased));
            label.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normalized - 0.12f) / 0.28f));
            label.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(timer * 30f) * (1f + pulse * 9f), Mathf.Cos(timer * 21f) * 1.5f);

            leftPanel.anchoredPosition = new Vector2(-Mathf.Lerp(0f, 26f, eased), 0f);
            rightPanel.anchoredPosition = new Vector2(Mathf.Lerp(0f, 18f, eased), 0f);
            divider.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.8f, 1f, 1f), eased);
            dreadPulse.color = new Color(0.55f, 0.02f, 0.02f, Mathf.Lerp(0.08f, 0.38f, eased));
            shadowVeil.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.22f, 0.55f, eased));
            staticOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.02f, 0.12f, pulse * eased));

            if (normalized > 0.38f && Random.value > 0.86f)
            {
                audio.PlayUi(LoopfallCue.MenuGlitchTick, Random.Range(0.05f, 0.11f), 0.02f);
            }

            yield return null;
        }

        audio.PlayUi(LoopfallCue.MenuGlitchBurst, 0.12f, 0.02f);
        blackout.color = new Color(0f, 0f, 0f, 1f);
        noise.color = new Color(1f, 1f, 1f, 0.16f);
        label.alpha = 1f;
        label.rectTransform.anchoredPosition = Vector2.zero;
        yield return new WaitForSecondsRealtime(0.18f);

        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
        }

        MemoryManager.Instance.ResetProgress();
        LoopManager.Instance.SetLoopIndex(nextLoop, reason);
    }

    private Canvas CreateLoopTransitionOverlay(
        out RectTransform leftPanel,
        out RectTransform rightPanel,
        out RectTransform divider,
        out Image dreadPulse,
        out Image shadowVeil,
        out Image staticOverlay,
        out Image blackout,
        out RawImage noise,
        out TMP_Text label)
    {
        GameObject canvasObject = new("LoopAdvanceTransition");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        leftPanel = CreateOverlayPanel(canvas.transform, "LoopAdvanceGloomPanel", 0f, 0.5f, new Color(0.008f, 0.008f, 0.014f, 0.985f));
        rightPanel = CreateOverlayPanel(canvas.transform, "LoopAdvanceJoyPanel", 0.5f, 1f, new Color(0.48f, 0.43f, 0.29f, 0.8f));
        divider = CreateOverlayPanel(canvas.transform, "LoopAdvanceDivider", 0.493f, 0.507f, new Color(0.86f, 0.9f, 0.98f, 0.72f));
        dreadPulse = CreateOverlayImage(canvas.transform, "LoopAdvanceDreadPulse", new Color(0.55f, 0.02f, 0.02f, 0f));
        shadowVeil = CreateOverlayImage(canvas.transform, "LoopAdvanceShadowVeil", new Color(0f, 0f, 0f, 0.22f));
        staticOverlay = CreateOverlayImage(canvas.transform, "LoopAdvanceStaticOverlay", new Color(1f, 1f, 1f, 0f));
        blackout = CreateOverlayImage(canvas.transform, "LoopAdvanceBlackout", new Color(0f, 0f, 0f, 0f));
        noise = CreateOverlayNoise(canvas.transform);
        label = CreateOverlayText(canvas.transform);
        return canvas;
    }

    private static RectTransform CreateOverlayPanel(Transform parent, string name, float anchorMinX, float anchorMaxX, Color color)
    {
        GameObject panelObject = new(name, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorMinX, 0f);
        rect.anchorMax = new Vector2(anchorMaxX, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static Image CreateOverlayImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RawImage CreateOverlayNoise(Transform parent)
    {
        GameObject noiseObject = new("LoopAdvanceNoise", typeof(RectTransform));
        noiseObject.transform.SetParent(parent, false);
        RectTransform rect = noiseObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Texture2D texture = new(64, 64, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point,
            name = "LoopAdvanceNoiseTexture",
        };

        Color32[] pixels = new Color32[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte value = (byte)Random.Range(24, 255);
            pixels[i] = new Color32(value, value, value, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        RawImage noise = noiseObject.AddComponent<RawImage>();
        noise.texture = texture;
        noise.color = new Color(1f, 1f, 1f, 0f);
        noise.raycastTarget = false;
        return noise;
    }

    private static TMP_Text CreateOverlayText(Transform parent)
    {
        GameObject textObject = new("LoopAdvanceText", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.43f);
        rect.anchorMax = new Vector2(0.8f, 0.58f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 1f);
        text.alpha = 0f;
        text.characterSpacing = 4f;
        text.raycastTarget = false;
        return text;
    }

    private System.Collections.IEnumerator DebugSwitchToNextLoopSequence()
    {
        int currentLoop = LoopManager.Instance.CurrentLoopIndex;
        int nextLoop = currentLoop >= Config.MaxLoops ? 1 : currentLoop + 1;
        yield return RunLoopTransitionSequence(nextLoop, $"Debug key {debugNextLoopKey}");
    }

    private System.Collections.IEnumerator StartGameSequence()
    {
        startSequenceRunning = true;

        MenuPresentation presentation = FindFirstObjectByType<MenuPresentation>();
        if (presentation != null)
        {
            yield return StartCoroutine(presentation.PlayLoopfallStartTransition());
        }

        SceneManager.LoadScene(loopfallSceneName);
    }

    public void RelaunchGame()
    {
        SetPaused(false);
        LoopManager.EnsureExists().ResetProgress();
        MemoryManager.EnsureExists().ResetProgress();

#if UNITY_EDITOR
        EditorApplication.delayCall += RestartPlayMode;
        EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        string executablePath = ResolveExecutablePath();
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty,
            };

            Process.Start(startInfo);

            Application.Quit();
            return;
        }
#else
        SceneManager.LoadScene(loopfallSceneName);
#endif
    }

#if UNITY_EDITOR
    private static void RestartPlayMode()
    {
        EditorApplication.delayCall -= RestartPlayMode;
        EditorApplication.isPlaying = true;
    }
#endif

    private static string ResolveExecutablePath()
    {
        string processPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
        {
            return processPath;
        }

        string dataPath = Application.dataPath;
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            return string.Empty;
        }

#if UNITY_STANDALONE_WIN
        string windowsPath = Path.ChangeExtension(dataPath.Replace("_Data", string.Empty), ".exe");
        if (File.Exists(windowsPath))
        {
            return windowsPath;
        }
#elif UNITY_STANDALONE_OSX
        string appPath = Directory.GetParent(dataPath)?.Parent?.Parent?.FullName ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(appPath) && Directory.Exists(appPath))
        {
            return appPath;
        }
#elif UNITY_STANDALONE_LINUX
        string linuxPath = dataPath.Replace("_Data", string.Empty);
        if (File.Exists(linuxPath))
        {
            return linuxPath;
        }
#endif

        return string.Empty;
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void ReturnToStartMenu()
    {
        SetPaused(false);
        SceneManager.LoadScene("StartMenu");
    }

    private void SetPaused(bool paused)
    {
        pauseSlowed = paused;
        IsGameplayPaused = paused;
        nextPauseToggleTime = Time.unscaledTime + PauseToggleDebounce;
        Time.timeScale = paused ? Config.PauseTimeScale : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (pauseMenu == null)
        {
            pauseMenu = PauseMenuController.EnsureExists();
            pauseMenu.Bind(this);
        }

        pauseMenu.SetVisible(paused);
        ClearGameplayInputBuffers();
        ApplyCursorStateForActiveScene();
    }

    private void ClearGameplayInputBuffers()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        player?.ClearBufferedInput();
        Input.ResetInputAxes();
    }

    public void ExitGame()
    {
        SetPaused(false);
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ApplyCursorStateForActiveScene()
    {
        if (SceneManager.GetActiveScene().name != loopfallSceneName || pauseSlowed)
        {
            ReleaseCursorForMenus();
        }
    }

    private static void ReleaseCursorForMenus()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
