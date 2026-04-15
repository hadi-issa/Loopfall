using Game;
using UnityEngine;

public class PrototypeHud : MonoBehaviour
{
    private static string interactionPrompt = string.Empty;
    private static string transientMessage = string.Empty;
    private static float messageUntil;

    private GUIStyle labelStyle = null!;

    public static void SetInteractionPrompt(string prompt)
    {
        interactionPrompt = prompt;
    }

    public static void PushMessage(string message)
    {
        transientMessage = message;
        messageUntil = Time.unscaledTime + 2.5f;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            richText = true,
        };
        labelStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (MemoryManager.Instance == null || LoopManager.Instance == null)
        {
            return;
        }

        Rect panelRect = new(20f, 20f, 460f, 150f);
        GUI.Box(panelRect, string.Empty);

        string hudText =
            $"Loop <b>{LoopManager.Instance.CurrentLoopIndex}</b> / {Config.MaxLoops}\n" +
            $"Stabilizing: <b>{MemoryManager.Instance.StabilizingFragments}</b>    Corrupted: <b>{MemoryManager.Instance.CorruptedFragments}</b>\n" +
            "Move: WASD / Arrow Keys    Offer: E    Reset Loop: R    Slow-Time Pause: Esc";

        GUI.Label(new Rect(32f, 30f, 430f, 90f), hudText, labelStyle);

        if (!string.IsNullOrEmpty(interactionPrompt))
        {
            GUI.Label(new Rect(32f, 108f, 430f, 30f), interactionPrompt, labelStyle);
        }

        if (!string.IsNullOrEmpty(transientMessage) && Time.unscaledTime <= messageUntil)
        {
            GUI.Label(new Rect(32f, 136f, 430f, 30f), transientMessage, labelStyle);
        }
    }
}
