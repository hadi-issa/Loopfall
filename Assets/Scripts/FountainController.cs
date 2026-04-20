using UnityEngine;

public class FountainController : MonoBehaviour
{
    [SerializeField] private Transform waterSurface = null!;
    [SerializeField] private Renderer waterRenderer = null!;
    [SerializeField] private Transform fountainSpout = null!;
    [SerializeField] private Transform fountainCore = null!;
    [SerializeField] private Renderer coreRenderer = null!;

    private Material particleMaterial = null!;
    private Material waterMaterial = null!;
    private Material coreMaterial = null!;
    private Vector3 waterBaseScale;
    private Vector3 coreBaseScale;
    private float waterBaseY;
    private float coreBaseY;
    private bool initialized;

    public void Configure(Transform newWaterSurface, Renderer newWaterRenderer, Transform newFountainSpout, Transform newFountainCore, Renderer newCoreRenderer)
    {
        waterSurface = newWaterSurface;
        waterRenderer = newWaterRenderer;
        fountainSpout = newFountainSpout;
        fountainCore = newFountainCore;
        coreRenderer = newCoreRenderer;
        TryInitialize();
    }

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            if (!initialized)
            {
                return;
            }
        }

        AnimateWaterSurface();
        AnimateCore();
    }

    private void TryInitialize()
    {
        if (initialized || waterSurface == null || fountainSpout == null || fountainCore == null)
        {
            return;
        }

        transform.position = waterSurface.position;
        transform.rotation = Quaternion.identity;
        waterBaseScale = waterSurface.localScale;
        coreBaseScale = fountainCore.localScale;
        waterBaseY = waterSurface.localPosition.y;
        coreBaseY = fountainCore.localPosition.y;

        waterMaterial = waterRenderer != null ? waterRenderer.material : null;
        coreMaterial = coreRenderer != null ? coreRenderer.material : null;
        particleMaterial = CreateParticleMaterial();

        BuildMainJet();
        BuildMistCrown();
        BuildBasinSplash();
        initialized = true;
    }

    private void AnimateWaterSurface()
    {
        float t = Time.time;
        float bob = Mathf.Sin(t * 1.35f) * 0.016f + Mathf.Sin(t * 0.48f + 0.75f) * 0.008f;
        float pulse = 1f + Mathf.Sin(t * 1.1f) * 0.018f + Mathf.Sin(t * 0.62f) * 0.01f;
        float heightPulse = 1f + Mathf.Sin(t * 2.15f) * 0.05f;

        Vector3 waterPosition = waterSurface.localPosition;
        waterPosition.y = waterBaseY + bob;
        waterSurface.localPosition = waterPosition;
        waterSurface.localScale = new Vector3(waterBaseScale.x * pulse, waterBaseScale.y * heightPulse, waterBaseScale.z * pulse);

        if (waterMaterial == null)
        {
            return;
        }

        Color baseColor = new(0.36f, 0.62f, 0.78f, 0.92f);
        Color pulseColor = Color.Lerp(baseColor, new Color(0.62f, 0.82f, 0.92f, 1f), Mathf.InverseLerp(-1f, 1f, Mathf.Sin(t * 1.45f)));
        SetMaterialColor(waterMaterial, pulseColor);
        SetEmissionColor(waterMaterial, pulseColor * 0.22f);
    }

    private void AnimateCore()
    {
        float t = Time.time;
        Vector3 corePosition = fountainCore.localPosition;
        corePosition.y = coreBaseY + Mathf.Sin(t * 1.55f + 0.4f) * 0.045f;
        fountainCore.localPosition = corePosition;
        fountainCore.localScale = coreBaseScale * (1f + Mathf.Sin(t * 1.7f) * 0.035f);
        fountainCore.Rotate(0f, 24f * Time.deltaTime, 0f, Space.Self);

        if (coreMaterial == null)
        {
            return;
        }

        Color glow = Color.Lerp(new Color(0.72f, 0.9f, 0.95f), new Color(0.9f, 0.97f, 1f), Mathf.InverseLerp(-1f, 1f, Mathf.Sin(t * 1.95f)));
        SetMaterialColor(coreMaterial, glow);
        SetEmissionColor(coreMaterial, glow * 0.3f);
    }

    private void BuildMainJet()
    {
        Vector3 localPosition = transform.InverseTransformPoint(fountainSpout.position + Vector3.up * 0.18f);
        ParticleSystem jet = CreateParticleSystem("MainJet", localPosition, 2);
        ParticleSystem.MainModule main = jet.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.74f, 0.98f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.8f, 5.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.84f, 0.94f, 1f, 0.82f));
        main.gravityModifier = 0.44f;
        main.maxParticles = 320;

        ParticleSystem.EmissionModule emission = jet.emission;
        emission.enabled = true;
        emission.rateOverTime = 135f;

        ParticleSystem.ShapeModule shape = jet.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 3.5f;
        shape.radius = 0.04f;
        shape.length = 0.12f;

        ParticleSystem.NoiseModule noise = jet.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = 0.08f;
        noise.strengthY = 0.05f;
        noise.strengthZ = 0.08f;
        noise.frequency = 0.38f;
        noise.scrollSpeed = 0.35f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = jet.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.76f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.94f, 0.98f, 1f), 0.46f),
                new GradientColorKey(new Color(0.74f, 0.86f, 0.94f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.84f, 0.08f),
                new GradientAlphaKey(0.92f, 0.36f),
                new GradientAlphaKey(0.12f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        jet.Play();
    }

    private void BuildMistCrown()
    {
        Vector3 localPosition = transform.InverseTransformPoint(fountainCore.position + Vector3.up * 0.1f);
        ParticleSystem mist = CreateParticleSystem("MistCrown", localPosition, 1);
        ParticleSystem.MainModule main = mist.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.95f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.52f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.97f, 1f, 0.22f));
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = mist.emission;
        emission.enabled = true;
        emission.rateOverTime = 52f;

        ParticleSystem.ShapeModule shape = mist.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.16f;

        ParticleSystem.VelocityOverLifetimeModule velocity = mist.velocityOverLifetime;
        velocity.enabled = false;

        ParticleSystem.NoiseModule noise = mist.noise;
        noise.enabled = true;
        noise.strength = 0.24f;
        noise.frequency = 0.52f;
        noise.scrollSpeed = 0.4f;

        mist.Play();
    }

    private void BuildBasinSplash()
    {
        Vector3 localPosition = transform.InverseTransformPoint(waterSurface.position + Vector3.up * 0.04f);
        ParticleSystem splash = CreateParticleSystem("BasinSplash", localPosition, 0);
        ParticleSystem.MainModule main = splash.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.24f, 0.56f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.84f, 0.94f, 1f, 0.44f));
        main.gravityModifier = 0.2f;
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = splash.emission;
        emission.enabled = true;
        emission.rateOverTime = 58f;

        ParticleSystem.ShapeModule shape = splash.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.12f;
        shape.radiusThickness = 0.38f;
        shape.arc = 360f;
        shape.randomDirectionAmount = 0.12f;

        ParticleSystem.VelocityOverLifetimeModule velocity = splash.velocityOverLifetime;
        velocity.enabled = false;

        splash.Play();
    }

    private ParticleSystem CreateParticleSystem(string name, Vector3 localPosition, int sortingOrder)
    {
        GameObject particleObject = new(name);
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = localPosition;
        particleObject.transform.localRotation = Quaternion.identity;

        ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = particleMaterial;

        return particleSystem;
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
            name = "LoopfallFountainParticleMaterial",
        };

        SetMaterialColor(material, new Color(0.84f, 0.94f, 1f, 0.75f));
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
