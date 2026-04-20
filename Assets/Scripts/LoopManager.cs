using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoopManager : MonoBehaviour
{
    public static LoopManager Instance { get; private set; } = null!;

    public int CurrentLoopIndex { get; private set; } = 1;
    public float DecayRate => 1f + (CurrentLoopIndex - 1) * 0.18f;

    public static LoopManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LoopManager existing = FindFirstObjectByType<LoopManager>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject managerObject = new("LoopManager");
        Instance = managerObject.AddComponent<LoopManager>();
        DontDestroyOnLoad(managerObject);
        return Instance;
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
    }

    public void AdvanceLoop(string reason)
    {
        CurrentLoopIndex = Mathf.Min(CurrentLoopIndex + 1, Config.MaxLoops);
        Debug.Log($"Loop progression: {reason}. Entering loop {CurrentLoopIndex}.");
        ReloadActiveScene();
    }

    public void RestartCurrentLoop(string reason)
    {
        Debug.Log($"Loop restart: {reason}. Staying on loop {CurrentLoopIndex}.");
        ReloadActiveScene();
    }

    private static void ReloadActiveScene()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetProgress()
    {
        CurrentLoopIndex = 1;
    }
}
