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

        PrototypeHud.PushMessage($"The shrine accepts the {requestedFragment} fragment");
    }
}
