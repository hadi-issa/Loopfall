using UnityEngine;

public class FountainController : MonoBehaviour
{
    [SerializeField] private Transform fountainCore = null!;
    [SerializeField] private Renderer coreRenderer = null!;

    private Material coreMaterial = null!;
    private Vector3 coreBaseScale;
    private float coreBaseY;
    private bool initialized;

    public void Configure(Transform newFountainCore, Renderer newCoreRenderer)
    {
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

        AnimateCore();
    }

    private void TryInitialize()
    {
        if (initialized || fountainCore == null)
        {
            return;
        }

        transform.position = fountainCore.position;
        transform.rotation = Quaternion.identity;
        coreBaseScale = fountainCore.localScale;
        coreBaseY = fountainCore.localPosition.y;

        coreMaterial = coreRenderer != null ? coreRenderer.material : null;
        initialized = true;
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
