using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(AudioListener))]
public class AudioListenerCoordinator : MonoBehaviour
{
    private AudioListener localListener = null!;

    private void OnEnable()
    {
        localListener = GetComponent<AudioListener>();
        Refresh();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (localListener == null)
        {
            localListener = GetComponent<AudioListener>();
            if (localListener == null)
            {
                return;
            }
        }

        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length <= 1)
        {
            localListener.enabled = true;
            return;
        }

        AudioListener preferred = ResolvePreferredListener(listeners);
        foreach (AudioListener listener in listeners)
        {
            listener.enabled = listener == preferred;
        }
    }

    private AudioListener ResolvePreferredListener(AudioListener[] listeners)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            AudioListener mainListener = mainCamera.GetComponent<AudioListener>();
            if (mainListener != null)
            {
                return mainListener;
            }
        }

        if (localListener != null)
        {
            return localListener;
        }

        return listeners[0];
    }
}
