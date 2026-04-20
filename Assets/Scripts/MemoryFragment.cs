using Game;
using UnityEngine;

public class MemoryFragment : MonoBehaviour
{
    [SerializeField] private FragmentType fragmentType = FragmentType.Stabilizing;
    [SerializeField] private float spinSpeed = 70f;
    [SerializeField] private float bobAmount = 0.12f;
    [SerializeField] private float bobSpeed = 1.8f;

    private Vector3 startPosition;

    public void Configure(FragmentType type)
    {
        fragmentType = type;
    }

    private void Awake()
    {
        startPosition = transform.position;

        LoopfallAudioEmitter emitter = GetComponent<LoopfallAudioEmitter>();
        if (emitter == null)
        {
            emitter = gameObject.AddComponent<LoopfallAudioEmitter>();
        }

        emitter.Configure(LoopfallCue.FragmentLoop, fragmentType == FragmentType.Stabilizing ? 0.07f : 0.05f, fragmentType == FragmentType.Stabilizing ? 1.08f : 0.84f, 1f, 1f, 10f);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + startPosition.x) * bobAmount);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<Player>(out _))
        {
            return;
        }

        MemoryManager.Instance.AddFragment(fragmentType);
        LoopfallAudio.EnsureExists().PlayAt(
            fragmentType == FragmentType.Stabilizing ? LoopfallCue.FragmentPickupGood : LoopfallCue.FragmentPickupBad,
            transform.position,
            0.34f,
            fragmentType == FragmentType.Stabilizing ? 1.04f : 0.92f,
            0.65f,
            12f);
        PrototypeHud.PushMessage($"{fragmentType} fragment remembered");
        Destroy(gameObject);
    }
}
