using System.Collections;
using Game;
using UnityEngine;

public class HazardVolume : MonoBehaviour
{
    [SerializeField] private string resetReason = "Entered a hazard";
    [SerializeField] private float deathDuration = 0.72f;
    [SerializeField] private bool useSurfaceDiskCheck;
    [SerializeField] private Vector3 surfaceCenter;
    [SerializeField] private float surfaceRadius;
    [SerializeField] private float surfaceY;
    [SerializeField] private float surfaceContactTolerance = 0.006f;

    private bool triggered;

    public void Configure(string reason)
    {
        resetReason = reason;
    }

    public void ConfigureSurfaceDisk(string reason, Vector3 worldCenter, float worldRadius, float worldSurfaceY)
    {
        resetReason = reason;
        useSurfaceDiskCheck = true;
        surfaceCenter = worldCenter;
        surfaceRadius = worldRadius;
        surfaceY = worldSurfaceY;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerDeath(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTriggerDeath(other);
    }

    private void TryTriggerDeath(Collider other)
    {
        if (triggered || !other.TryGetComponent(out Player player))
        {
            return;
        }

        if (useSurfaceDiskCheck && !IsTouchingSurface(player))
        {
            return;
        }

        triggered = true;
        StartCoroutine(DeathRoutine(player));
    }

    private bool IsTouchingSurface(Player player)
    {
        float playerRadius = ResolvePlayerRadius(player);
        Vector3 playerPosition = player.transform.position;
        Vector2 center2D = new(surfaceCenter.x, surfaceCenter.z);
        Vector2 player2D = new(playerPosition.x, playerPosition.z);
        float horizontalReach = surfaceRadius + playerRadius;
        float playerBottomY = playerPosition.y - playerRadius;

        return Vector2.SqrMagnitude(player2D - center2D) <= horizontalReach * horizontalReach
            && playerBottomY <= surfaceY + surfaceContactTolerance;
    }

    private float ResolvePlayerRadius(Player player)
    {
        SphereCollider sphere = player.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            float largestScale = Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.y, player.transform.lossyScale.z);
            return sphere.radius * largestScale;
        }

        Collider playerCollider = player.GetComponent<Collider>();
        return playerCollider != null ? playerCollider.bounds.extents.magnitude * 0.5f : Config.SoccerBallRadius;
    }

    private IEnumerator DeathRoutine(Player player)
    {
        Vector3 startPosition = player.transform.position;
        Vector3 startScale = player.transform.localScale;
        Renderer renderer = player.GetComponentInChildren<Renderer>();
        Material material = renderer != null ? renderer.material : null;
        Rigidbody body = player.GetComponent<Rigidbody>();

        player.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        foreach (Collider collider in player.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        CreateDeathBurst(startPosition);
        LoopfallAudio.EnsureExists().PlayAt(LoopfallCue.CrashSting, startPosition, 0.22f, 1.18f, 1f, 18f);

        float timer = 0f;
        while (timer < deathDuration)
        {
            timer += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(timer / deathDuration);
            float eased = normalized * normalized * (3f - 2f * normalized);

            player.transform.position = startPosition + Vector3.down * (0.24f * eased);
            player.transform.localScale = Vector3.Lerp(startScale, startScale * 0.08f, eased);

            if (material != null)
            {
                Color color = Color.Lerp(new Color(0.74f, 0.82f, 0.92f, 1f), new Color(0.22f, 0.58f, 0.72f, 0.25f), eased);
                SetMaterialColor(material, color);
                SetEmissionColor(material, Color.Lerp(new Color(0.08f, 0.14f, 0.18f), new Color(0.02f, 0.24f, 0.36f), eased));
            }

            yield return null;
        }

        LoopManager.EnsureExists().RestartCurrentLoop(resetReason);
    }

    private void CreateDeathBurst(Vector3 position)
    {
        GameObject burstObject = new("WaterDeathBurst");
        burstObject.transform.position = position;

        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = 0.42f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.095f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.9f, 1f, 0.82f));
        main.gravityModifier = 0.08f;
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 54) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;
        shape.randomDirectionAmount = 0.55f;

        ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = CreateParticleMaterial();

        particles.Play();
        Destroy(burstObject, 1.4f);
    }

    private Material CreateParticleMaterial()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Legacy Shaders/Particles/Alpha Blended") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Standard");

        if (shader == null)
        {
            return null;
        }

        Material material = new(shader)
        {
            name = "WaterDeathBurstMaterial",
        };

        SetMaterialColor(material, new Color(0.62f, 0.9f, 1f, 0.78f));
        return material;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void SetEmissionColor(Material material, Color color)
    {
        if (material == null || !material.HasProperty("_EmissionColor"))
        {
            return;
        }

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color);
    }
}
