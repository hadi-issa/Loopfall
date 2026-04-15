using UnityEngine;

public class MenuBackdropDrift : MonoBehaviour
{
    private RectTransform rect = null!;
    private Vector2 origin;
    private float amplitude = 16f;
    private float speed = 0.1f;
    private float phase;

    public void Configure(Vector2 startOrigin, float movementAmplitude, float movementSpeed)
    {
        origin = startOrigin;
        amplitude = movementAmplitude;
        speed = movementSpeed;
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        origin = rect.anchoredPosition;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float t = Time.unscaledTime * speed + phase;
        rect.anchoredPosition = origin + new Vector2(Mathf.Sin(t) * amplitude, Mathf.Cos(t * 0.8f) * amplitude * 0.6f);
    }
}
