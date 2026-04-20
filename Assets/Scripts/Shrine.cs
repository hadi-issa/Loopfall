using Game;
using UnityEngine;

public class Shrine : MonoBehaviour
{
    [SerializeField] private FragmentType requestedFragment = FragmentType.Stabilizing;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool playerInside;

    public void Configure(FragmentType fragmentType)
    {
        requestedFragment = fragmentType;
    }

    private void Update()
    {
        if (GameManager.IsGameplayPaused)
        {
            PrototypeHud.SetInteractionPrompt(string.Empty);
            return;
        }

        if (!playerInside)
        {
            return;
        }

        PrototypeHud.SetInteractionPrompt($"Press {interactKey} to offer a {requestedFragment} fragment");

        if (Input.GetKeyDown(interactKey))
        {
            TryOfferFragment();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            playerInside = false;
            PrototypeHud.SetInteractionPrompt(string.Empty);
        }
    }

    private void TryOfferFragment()
    {
        if (!MemoryManager.Instance.ConsumeFragment(requestedFragment))
        {
            LoopfallAudio.EnsureExists().PlayAt(LoopfallCue.ShrineReject, transform.position, 0.34f, 1f, 0.8f, 14f);
            PrototypeHud.PushMessage($"The shrine asks for a {requestedFragment} fragment");
            return;
        }

        foreach (DecayController decayController in FindObjectsByType<DecayController>(FindObjectsSortMode.None))
        {
            if (requestedFragment == FragmentType.Stabilizing)
            {
                decayController.ApplyStabilization(2.5f);
            }
            else
            {
                decayController.AccelerateDecay(1.5f);
            }
        }

        LoopfallAudio.EnsureExists().PlayAt(LoopfallCue.ShrineAccept, transform.position, 0.42f, requestedFragment == FragmentType.Stabilizing ? 1.02f : 0.9f, 0.8f, 16f);
        PrototypeHud.PushMessage($"The shrine accepts the {requestedFragment} fragment");
    }
}
