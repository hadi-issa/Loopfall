using Game;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float groundedAcceleration = 24f;
    [SerializeField] private float airborneAcceleration = 7f;
    [SerializeField] private float maxGroundSpeed = 9.5f;
    [SerializeField] private float maxAirSpeed = 7.2f;
    [SerializeField] private float steeringGrip = 3.1f;
    [SerializeField] private float rollingResistance = 0.95f;
    [SerializeField] private float downhillSlideAcceleration = 5.6f;
    [SerializeField] private float visualRollTorque = 34f;

    [Header("Grounding")]
    [SerializeField] private float groundProbeDistance = 0.08f;
    [SerializeField] private float groundSnapAcceleration = 18f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Transform cameraTransform;

    [Header("Jumping")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float jumpImpulse = 3.08f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpCooldown = 0.14f;

    private Rigidbody body = null!;
    private SphereCollider sphereCollider = null!;
    private AudioSource rollingAudio = null!;
    private Vector2 moveInput;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 supportVelocity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastJumpPressedTime = float.NegativeInfinity;
    private float nextJumpTime;

    public bool IsGrounded => isGrounded;
    public Vector3 Velocity => body.linearVelocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }

        ConfigurePhysicsBody();
        ConfigureRollingAudio();
    }

    private void Update()
    {
        if (GameManager.IsGameplayPaused)
        {
            ClearBufferedInput();
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey))
        {
            lastJumpPressedTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        UpdateGrounding();
        if (GameManager.IsGameplayPaused)
        {
            UpdateRollingAudio();
            return;
        }

        TryJump();
        ApplyMovementForces();
        ApplyGroundSnap();
        ApplyVisualRoll();
        UpdateRollingAudio();
    }

    public void SetCameraTransform(Transform targetCamera)
    {
        cameraTransform = targetCamera;
    }

    public void ClearBufferedInput()
    {
        moveInput = Vector2.zero;
        lastJumpPressedTime = float.NegativeInfinity;
    }

    private void ConfigurePhysicsBody()
    {
        body.useGravity = true;
        body.mass = Config.SoccerBallMass;
        body.linearDamping = 0.12f;
        body.angularDamping = 0.03f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.maxAngularVelocity = 110f;
    }

    private void ConfigureRollingAudio()
    {
        rollingAudio = GetComponent<AudioSource>();
        if (rollingAudio == null)
        {
            rollingAudio = gameObject.AddComponent<AudioSource>();
        }

        LoopfallAudio audio = LoopfallAudio.EnsureExists();
        audio.ConfigureLoopSource(rollingAudio, LoopfallCue.RollLoop, 0f, 0.85f, 0f);
        rollingAudio.loop = true;
        rollingAudio.playOnAwake = false;
        if (!rollingAudio.isPlaying)
        {
            rollingAudio.Play();
        }
    }

    private void UpdateGrounding()
    {
        float worldRadius = GetWorldRadius();
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(0.02f, worldRadius * 0.25f);
        float castRadius = worldRadius * 0.92f;
        float castDistance = Mathf.Max(groundProbeDistance, worldRadius * 0.85f + groundProbeDistance);

        if (Physics.SphereCast(origin, castRadius, Vector3.down, out RaycastHit hit, castDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            isGrounded = Vector3.Angle(hit.normal, Vector3.up) <= 60f;
            groundNormal = hit.normal;
            supportVelocity = hit.rigidbody != null ? hit.rigidbody.linearVelocity : Vector3.zero;
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }
            return;
        }

        isGrounded = false;
        groundNormal = Vector3.up;
        supportVelocity = Vector3.zero;
    }

    private void TryJump()
    {
        if (Time.time < nextJumpTime)
        {
            return;
        }

        if (Time.time - lastJumpPressedTime > jumpBufferTime)
        {
            return;
        }

        if (Time.time - lastGroundedTime > coyoteTime)
        {
            return;
        }

        Vector3 jumpDirection = Vector3.up;
        float relativeUpwardSpeed = Vector3.Dot(body.linearVelocity - supportVelocity, jumpDirection);
        if (relativeUpwardSpeed < 0f)
        {
            body.linearVelocity -= jumpDirection * relativeUpwardSpeed;
        }

        body.AddForce(jumpDirection * jumpImpulse, ForceMode.Impulse);
        isGrounded = false;
        groundNormal = Vector3.up;
        supportVelocity = Vector3.zero;
        lastGroundedTime = float.NegativeInfinity;
        lastJumpPressedTime = float.NegativeInfinity;
        nextJumpTime = Time.time + jumpCooldown;
    }

    private void ApplyMovementForces()
    {
        Vector3 desiredDirection = ResolveMoveDirection();
        float inputStrength = Mathf.Clamp01(desiredDirection.magnitude);
        if (inputStrength > 0f)
        {
            desiredDirection.Normalize();
        }

        Vector3 movePlaneNormal = isGrounded ? groundNormal : Vector3.up;
        Vector3 slopeDirection = Vector3.ProjectOnPlane(desiredDirection, movePlaneNormal).normalized;
        if (slopeDirection == Vector3.zero)
        {
            slopeDirection = desiredDirection;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, movePlaneNormal);
        Vector3 supportPlanarVelocity = Vector3.ProjectOnPlane(supportVelocity, movePlaneNormal);
        Vector3 relativePlanarVelocity = planarVelocity - supportPlanarVelocity;

        float maxSpeed = isGrounded ? maxGroundSpeed : maxAirSpeed;
        float acceleration = isGrounded ? groundedAcceleration : airborneAcceleration;

        if (inputStrength > 0.01f)
        {
            Vector3 desiredVelocity = slopeDirection * (maxSpeed * inputStrength);
            Vector3 velocityDelta = desiredVelocity - relativePlanarVelocity;
            body.AddForce(Vector3.ClampMagnitude(velocityDelta, acceleration), ForceMode.Acceleration);
        }

        if (!isGrounded)
        {
            return;
        }

        Vector3 downhillDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
        if (downhillDirection.sqrMagnitude > 0.0001f)
        {
            body.AddForce(downhillDirection.normalized * downhillSlideAcceleration, ForceMode.Acceleration);
        }

        Vector3 lateralAxis = ResolveLateralAxis();
        Vector3 sidewaysVelocity = Vector3.Project(relativePlanarVelocity, lateralAxis);
        body.AddForce(-sidewaysVelocity * steeringGrip, ForceMode.Acceleration);

        if (inputStrength < 0.05f)
        {
            body.AddForce(-relativePlanarVelocity * rollingResistance, ForceMode.Acceleration);
        }
    }

    private void ApplyGroundSnap()
    {
        if (!isGrounded)
        {
            return;
        }

        body.AddForce(-groundNormal * groundSnapAcceleration, ForceMode.Acceleration);
    }

    private void ApplyVisualRoll()
    {
        Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        if (planarVelocity.sqrMagnitude < 0.05f)
        {
            return;
        }

        Vector3 torqueAxis = Vector3.Cross(Vector3.up, planarVelocity.normalized);
        body.AddTorque(torqueAxis * (visualRollTorque * planarVelocity.magnitude), ForceMode.Acceleration);
    }

    private void UpdateRollingAudio()
    {
        if (rollingAudio == null)
        {
            return;
        }

        float planarSpeed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
        float targetVolume = isGrounded ? Mathf.InverseLerp(0.35f, maxGroundSpeed * 0.8f, planarSpeed) * 0.3f : 0f;
        float targetPitch = Mathf.Lerp(0.72f, 1.35f, Mathf.InverseLerp(0.25f, maxGroundSpeed, planarSpeed));
        rollingAudio.volume = Mathf.Lerp(rollingAudio.volume, targetVolume, 8f * Time.fixedDeltaTime);
        rollingAudio.pitch = Mathf.Lerp(rollingAudio.pitch, targetPitch, 7f * Time.fixedDeltaTime);
    }

    private float GetWorldRadius()
    {
        float largestScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        largestScale = Mathf.Max(largestScale, transform.lossyScale.z);
        return sphereCollider.radius * largestScale;
    }

    private Vector3 ResolveMoveDirection()
    {
        Vector3 rawDirection = new(moveInput.x, 0f, moveInput.y);
        if (rawDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        Transform reference = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(reference.right, Vector3.up).normalized;

        if (forward == Vector3.zero)
        {
            forward = Vector3.forward;
        }

        if (right == Vector3.zero)
        {
            right = Vector3.right;
        }

        return forward * rawDirection.z + right * rawDirection.x;
    }

    private Vector3 ResolveLateralAxis()
    {
        Transform reference = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : transform;
        Vector3 right = Vector3.ProjectOnPlane(reference.right, groundNormal).normalized;
        return right == Vector3.zero ? Vector3.right : right;
    }
}
