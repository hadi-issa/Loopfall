using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float groundedAcceleration = 32f;
    [SerializeField] private float airborneAcceleration = 12f;
    [SerializeField] private float maxGroundSpeed = 11f;
    [SerializeField] private float maxAirSpeed = 9f;
    [SerializeField] private float steeringGrip = 5.5f;
    [SerializeField] private float rollingResistance = 1.5f;

    [Header("Grounding")]
    [SerializeField] private float groundProbeDistance = 0.65f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody body = null!;
    private SphereCollider sphereCollider = null!;
    private Vector2 moveInput;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;
    private Vector3 supportVelocity;

    public bool IsGrounded => isGrounded;
    public Vector3 Velocity => body.linearVelocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        ConfigurePhysicsBody();
    }

    private void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        UpdateGrounding();
        ApplyMovementForces();
    }

    public void SetCameraTransform(Transform targetCamera)
    {
        cameraTransform = targetCamera;
    }

    private void ConfigurePhysicsBody()
    {
        body.useGravity = true;
        body.mass = 1.35f;
        body.linearDamping = 0.12f;
        body.angularDamping = 0.08f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.maxAngularVelocity = 35f;
    }

    private void UpdateGrounding()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float castRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) * 0.9f;

        if (Physics.SphereCast(origin, castRadius, Vector3.down, out RaycastHit hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            groundNormal = hit.normal;
            supportVelocity = hit.rigidbody != null ? hit.rigidbody.linearVelocity : Vector3.zero;
            return;
        }

        isGrounded = false;
        groundNormal = Vector3.up;
        supportVelocity = Vector3.zero;
    }

    private void ApplyMovementForces()
    {
        Vector3 inputDirection = ResolveMoveDirection();
        float inputStrength = Mathf.Clamp01(inputDirection.magnitude);
        if (inputStrength > 0f)
        {
            inputDirection.Normalize();
        }

        Vector3 slopeDirection = Vector3.ProjectOnPlane(inputDirection, groundNormal).normalized;
        if (!isGrounded || inputStrength <= 0f || slopeDirection == Vector3.zero)
        {
            slopeDirection = inputDirection;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        Vector3 supportPlanarVelocity = Vector3.ProjectOnPlane(supportVelocity, Vector3.up);
        Vector3 relativePlanarVelocity = planarVelocity - supportPlanarVelocity;

        float maxSpeed = isGrounded ? maxGroundSpeed : maxAirSpeed;
        float acceleration = isGrounded ? groundedAcceleration : airborneAcceleration;

        if (inputStrength > 0.01f)
        {
            Vector3 desiredVelocity = slopeDirection * (maxSpeed * inputStrength);
            Vector3 velocityDelta = desiredVelocity - relativePlanarVelocity;
            Vector3 driveForce = Vector3.ClampMagnitude(velocityDelta, acceleration);
            body.AddForce(driveForce, ForceMode.Acceleration);
        }

        if (isGrounded)
        {
            Vector3 lateralAxis = ResolveLateralAxis();
            Vector3 sidewaysVelocity = Vector3.Project(relativePlanarVelocity, lateralAxis);
            body.AddForce(-sidewaysVelocity * steeringGrip, ForceMode.Acceleration);

            if (inputStrength < 0.05f)
            {
                body.AddForce(-relativePlanarVelocity * rollingResistance, ForceMode.Acceleration);
            }
        }
    }

    private Vector3 ResolveMoveDirection()
    {
        Vector3 rawDirection = new Vector3(moveInput.x, 0f, moveInput.y);
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
        Vector3 right = Vector3.ProjectOnPlane(reference.right, Vector3.up).normalized;
        return right == Vector3.zero ? Vector3.right : right;
    }
}
