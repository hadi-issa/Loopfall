using System.Diagnostics;
using System.IO;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    private const float PauseToggleDebounce = 0.18f;

    [SerializeField] private KeyCode resetLoopKey = KeyCode.R;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private string loopfallSceneName = Config.MainSceneName;

    private bool pauseSlowed;
    private bool startSequenceRunning;
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

            return;
        }

        if (Input.GetKeyDown(resetLoopKey))
        {
            SetPaused(false);
            LoopManager.Instance.RestartCurrentLoop("Manual restart");
        }

        if (Input.GetKeyDown(pauseKey) && Time.unscaledTime >= nextPauseToggleTime)
        {
            SetPaused(!pauseSlowed);
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
}
