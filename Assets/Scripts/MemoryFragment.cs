using Game;
using UnityEngine;

public class MemoryFragment : MonoBehaviour
{
    [SerializeField] private FragmentType fragmentType = FragmentType.Stabilizing;
    [SerializeField] private float spinSpeed = 70f;

    public void Configure(FragmentType type)
    {
        fragmentType = type;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<Player>(out _))
        {
            return;
        }

        MemoryManager.Instance.AddFragment(fragmentType);
        PrototypeHud.PushMessage($"{fragmentType} fragment remembered");
        Destroy(gameObject);
    }
}
