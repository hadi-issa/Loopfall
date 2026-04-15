using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private KeyCode resetLoopKey = KeyCode.R;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private string loopfallSceneName = Config.MainSceneName;

    private bool pauseSlowed;
    private Player player = null!;

    private void Awake()
    {
        MemoryManager.EnsureExists();
        LoopManager.EnsureExists();
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

        if (Input.GetKeyDown(resetLoopKey))
        {
            LoopManager.Instance.AdvanceLoop("Manual reset");
        }

        if (Input.GetKeyDown(pauseKey))
        {
            pauseSlowed = !pauseSlowed;
            Time.timeScale = pauseSlowed ? Config.PauseTimeScale : 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        if (player != null && player.transform.position.y < Config.LoopFallHeight)
        {
            LoopManager.Instance.AdvanceLoop("Fell from the world");
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(loopfallSceneName);
    }

    public void RelaunchGame()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        LoopManager.Instance.ResetProgress();
        MemoryManager.Instance.ResetProgress();

        SceneManager.LoadScene(loopfallSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
