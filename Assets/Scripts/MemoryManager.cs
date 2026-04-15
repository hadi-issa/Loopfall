using Game;
using UnityEngine;

public class MemoryManager : MonoBehaviour
{
    public static MemoryManager Instance { get; private set; } = null!;

    public int StabilizingFragments { get; private set; }
    public int CorruptedFragments { get; private set; }

    public static MemoryManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        MemoryManager existing = FindFirstObjectByType<MemoryManager>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject managerObject = new("MemoryManager");
        Instance = managerObject.AddComponent<MemoryManager>();
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

    public void AddFragment(FragmentType fragmentType)
    {
        if (fragmentType == FragmentType.Stabilizing)
        {
            StabilizingFragments++;
        }
        else
        {
            CorruptedFragments++;
        }
    }

    public bool ConsumeFragment(FragmentType fragmentType)
    {
        if (fragmentType == FragmentType.Stabilizing && StabilizingFragments > 0)
        {
            StabilizingFragments--;
            return true;
        }

        if (fragmentType == FragmentType.Corrupted && CorruptedFragments > 0)
        {
            CorruptedFragments--;
            return true;
        }

        return false;
    }

    public void ResetProgress()
    {
        StabilizingFragments = 0;
        CorruptedFragments = 0;
    }
}
