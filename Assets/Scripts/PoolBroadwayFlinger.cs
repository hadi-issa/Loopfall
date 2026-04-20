using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PoolBroadwayFlinger : MonoBehaviour
{
    [SerializeField] private float minFlingInterval = 2.3f;
    [SerializeField] private float maxFlingInterval = 5.2f;
    [SerializeField] private float horizontalImpulse = 14f;
    [SerializeField] private float upwardImpulse = 17f;
    [SerializeField] private float torqueImpulse = 32.5f;
    [SerializeField] private float leashDistance = 48f;
    [SerializeField] private float resetBelowY = -10f;

    private Rigidbody body = null!;
    private Vector3 homeLocalPosition;
    private Quaternion homeLocalRotation;
    private bool configured;
    private float nextFlingTime;

    public void Configure(Vector3 localPosition, Quaternion localRotation)
    {
        homeLocalPosition = localPosition;
        homeLocalRotation = localRotation;
        configured = true;

        if (body != null)
        {
            ResetRelic();
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.mass = 0.85f;
        body.useGravity = true;
        body.linearDamping = 0.04f;
        body.angularDamping = 0.03f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.maxAngularVelocity = 80f;

        if (!configured)
        {
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
            configured = true;
        }
    }

    private void Start()
    {
        ScheduleNextFling();
    }

    private void Update()
    {
        Vector3 homeWorldPosition = transform.parent != null ? transform.parent.TransformPoint(homeLocalPosition) : homeLocalPosition;
        if (transform.position.y < resetBelowY || Vector3.Distance(transform.position, homeWorldPosition) > leashDistance)
        {
            ResetRelic();
            return;
        }

        if (Time.time >= nextFlingTime)
        {
            Fling();
            ScheduleNextFling();
        }
    }

    private void Fling()
    {
        Vector2 flatDirection = Random.insideUnitCircle.normalized;
        if (flatDirection == Vector2.zero)
        {
            flatDirection = Vector2.right;
        }

        Vector3 impulse = new Vector3(flatDirection.x, 0f, flatDirection.y) * Random.Range(horizontalImpulse * 0.65f, horizontalImpulse * 1.35f);
        impulse += Vector3.up * Random.Range(upwardImpulse * 0.75f, upwardImpulse * 1.25f);

        body.WakeUp();
        body.AddForce(impulse, ForceMode.Impulse);
        body.AddTorque(Random.insideUnitSphere * torqueImpulse, ForceMode.Impulse);
        LoopfallAudio.EnsureExists().PlayAt(LoopfallCue.DecayWarning, transform.position, 0.12f, Random.Range(0.78f, 1.18f), 1f, 18f);
    }

    private void ResetRelic()
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.localPosition = homeLocalPosition;
        transform.localRotation = homeLocalRotation;
        body.Sleep();
        ScheduleNextFling();
    }

    private void ScheduleNextFling()
    {
        nextFlingTime = Time.time + Random.Range(minFlingInterval, maxFlingInterval);
    }
}
