using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
{
    private RectTransform rect = null!;
    private Image image = null!;
    private Vector3 baseScale;
    private Color baseColor;
    private Color activeColor;
    private bool highlighted;
    private bool selectedFromKeyboard;

    public void Configure(Color normalColor, Color highlightColor)
    {
        baseColor = normalColor;
        activeColor = highlightColor;
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        baseScale = rect.localScale;

        if (image != null && baseColor == default)
        {
            baseColor = image.color;
            activeColor = Color.Lerp(image.color, Color.white, 0.18f);
        }
    }

    private void Update()
    {
        if (rect == null || image == null)
        {
            return;
        }

        Vector3 targetScale = highlighted ? baseScale * 1.035f : baseScale;
        Color targetColor = highlighted ? activeColor : baseColor;

        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, 8f * Time.unscaledDeltaTime);
        image.color = Color.Lerp(image.color, targetColor, 10f * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        LoopfallAudio.EnsureExists().PlayUi(LoopfallCue.ButtonHover, 0.22f, 0.08f);
        highlighted = true;
        selectedFromKeyboard = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        highlighted = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        highlighted = true;
        if (!selectedFromKeyboard)
        {
            LoopfallAudio.EnsureExists().PlayUi(LoopfallCue.ButtonHover, 0.18f, 0.08f);
            selectedFromKeyboard = true;
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        highlighted = false;
        selectedFromKeyboard = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        LoopfallAudio.EnsureExists().PlayUi(LoopfallCue.ButtonClick, 0.28f, 0.05f);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        LoopfallAudio.EnsureExists().PlayUi(LoopfallCue.ButtonClick, 0.28f, 0.05f);
    }
}
