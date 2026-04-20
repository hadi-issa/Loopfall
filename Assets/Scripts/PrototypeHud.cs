using UnityEngine;

public class PrototypeHud : MonoBehaviour
{
    public static void SetInteractionPrompt(string prompt)
    {
    }

    public static void PushMessage(string message)
    {
    }

    private void Awake()
    {
        Destroy(gameObject);
    }
}
