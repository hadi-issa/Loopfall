using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target = null!;
    [SerializeField] private float lookHeight = 0.2f;

    [Header("Orbit")]
    [SerializeField] private float defaultDistance = 5.5f;
    [SerializeField] private float distance = 5.5f;
    [SerializeField] private float minDistance = 2.1f;
    [SerializeField] private float maxDistance = 12.2f;
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float defaultPitch = 43f;
    [SerializeField] private float pitch = 43f;
    [SerializeField] private float minPitch = -12f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float mouseSensitivity = 3.2f;
    [SerializeField] private float zoomSensitivity = 1.35f;
    [SerializeField] private float followSharpness = 13f;

    [Header("Collision")]
    [SerializeField] private LayerMask obstructionMask = ~0;
    [SerializeField] private float obstructionPadding = 0.16f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorDuringGameplay = true;

    private bool hasSnapped;

    public void Configure(Transform followTarget, float targetLookHeight)
    {
        bool shouldResetOrbit = target != followTarget || !hasSnapped;
        target = followTarget;
        lookHeight = targetLookHeight;

        if (shouldResetOrbit)
        {
            distance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
            pitch = Mathf.Clamp(defaultPitch, minPitch, maxPitch);
            yaw = 0f;
        }
        else
        {
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 focusPoint = GetFocusPoint();
        transform.position = ResolveCameraPosition(focusPoint);
        transform.rotation = ResolveCameraRotation(focusPoint, transform.position);
        hasSnapped = true;
    }

    private void OnDisable()
    {
        ReleaseCursor();
    }

    private void Update()
    {
        if (target == null || !CanCaptureGameplayCursor())
        {
            ReleaseCursor();
            return;
        }

        LockCursorForGameplay();
        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 focusPoint = GetFocusPoint();
        Vector3 targetPosition = ResolveCameraPosition(focusPoint);
        Quaternion targetRotation = ResolveCameraRotation(focusPoint, targetPosition);

        if (!hasSnapped)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            hasSnapped = true;
            return;
        }

        float blend = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
    }

    private void InitializeOrbitFromCurrentTransform()
    {
        if (target == null)
        {
            return;
        }

        Vector3 focusPoint = GetFocusPoint();
        Vector3 offset = transform.position - focusPoint;
        if (offset.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 planarOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        if (planarOffset.sqrMagnitude > 0.001f)
        {
            yaw = Mathf.Atan2(planarOffset.x, planarOffset.z) * Mathf.Rad2Deg + 180f;
        }

        float currentDistance = offset.magnitude;
        if (currentDistance > 0.001f)
        {
            distance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            pitch = Mathf.Clamp(Mathf.Asin(offset.y / currentDistance) * Mathf.Rad2Deg, minPitch, maxPitch);
        }
    }

    private Vector3 GetFocusPoint()
    {
        return target.position + Vector3.up * lookHeight;
    }

    private Vector3 ResolveCameraPosition(Vector3 focusPoint)
    {
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraDirection = orbitRotation * Vector3.back;
        float resolvedDistance = ResolveObstructedDistance(focusPoint, cameraDirection);
        return focusPoint + cameraDirection * resolvedDistance;
    }

    private Quaternion ResolveCameraRotation(Vector3 focusPoint, Vector3 cameraPosition)
    {
        Vector3 lookDirection = focusPoint - cameraPosition;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private float ResolveObstructedDistance(Vector3 focusPoint, Vector3 cameraDirection)
    {
        if (Physics.SphereCast(
                focusPoint,
                obstructionPadding,
                cameraDirection,
                out RaycastHit hit,
                distance,
                obstructionMask,
                QueryTriggerInteraction.Ignore))
        {
            return Mathf.Clamp(hit.distance - obstructionPadding, obstructionPadding, distance);
        }

        return distance;
    }

    private void LockCursorForGameplay()
    {
        if (!CanCaptureGameplayCursor())
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool CanCaptureGameplayCursor()
    {
        return lockCursorDuringGameplay
            && Application.isPlaying
            && target != null
            && !GameManager.IsGameplayPaused
            && SceneManager.GetActiveScene().name == Config.MainSceneName;
    }

    private static void ReleaseCursor()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
