using UnityEngine;

public class FountainController : MonoBehaviour
{
    [SerializeField] private Transform waterSurface = null!;
    [SerializeField] private Renderer waterRenderer = null!;
    [SerializeField] private Transform fountainSpout = null!;
    [SerializeField] private Transform fountainCore = null!;
    [SerializeField] private Renderer coreRenderer = null!;

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
