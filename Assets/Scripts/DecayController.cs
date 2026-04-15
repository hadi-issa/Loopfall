using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DecayController : MonoBehaviour
{
    [SerializeField] private float fallDelay = 8f;
    [SerializeField] private float warningDuration = 1.5f;

    private Rigidbody body = null!;
    private float timer;
    private Quaternion initialRotation;
    private bool triggered;

    public void Configure(float delay, float warningTime)
    {
        fallDelay = delay;
        warningDuration = warningTime;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (triggered)
        {
            return;
        }

        timer += Time.deltaTime * LoopManager.Instance.DecayRate;

        float warningStart = Mathf.Max(0f, fallDelay - warningDuration);
        if (timer >= warningStart)
        {
            float wobbleTime = (timer - warningStart) * 9f;
            float wobble = Mathf.Sin(wobbleTime) * 3.5f;
            transform.rotation = initialRotation * Quaternion.Euler(wobble, 0f, wobble * 0.6f);
        }

        if (timer >= fallDelay)
        {
            TriggerCollapse();
        }
    }

    public void ApplyStabilization(float seconds)
    {
        if (triggered)
        {
            return;
        }

        timer = Mathf.Max(0f, timer - seconds);
    }

    public void AccelerateDecay(float seconds)
    {
        if (triggered)
        {
            return;
        }

        timer += seconds;
    }

    private void TriggerCollapse()
    {
        triggered = true;
        body.isKinematic = false;
        body.AddTorque(new Vector3(3f, 0.5f, 2f), ForceMode.Impulse);
        body.AddForce(Vector3.down * 1.5f, ForceMode.Impulse);
    }
}
