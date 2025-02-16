using UnityEngine;
using UnityEngine.EventSystems;

public class TitleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public GameObject imageObj = null;

    private Vector3 originalScale;
    private Color originalColor;
    private RectTransform rectTransform;
    private UnityEngine.UI.Image image;

    public float hoverScaleFactor = 1.1f;
    public float clickScaleFactor = 0.9f;
    public float transitionSpeed = 10f;
    public Color hoverColor = Color.yellow;
    public Color clickColor = Color.red;
    public float flashDuration = 0.03f;
    public int flashCount = 6;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = imageObj.GetComponent<UnityEngine.UI.Image>();
        originalScale = rectTransform.localScale;
        originalColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale * hoverScaleFactor));
        StartCoroutine(ChangeColor(hoverColor));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale));
        StartCoroutine(ChangeColor(originalColor));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale * clickScaleFactor));
        StartCoroutine(ChangeColor(clickColor));
        //// StartCoroutine(FlashEffect());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale * hoverScaleFactor));
        StartCoroutine(ChangeColor(hoverColor));
    }

    private System.Collections.IEnumerator ScaleTo(Vector3 targetScale)
    {
        while (Vector3.Distance(rectTransform.localScale, targetScale) > 0.01f)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * transitionSpeed);
            yield return null;
        }
        rectTransform.localScale = targetScale;
    }

    private System.Collections.IEnumerator ChangeColor(Color targetColor)
    {
        while (image.color != targetColor)
        {
            image.color = Color.Lerp(image.color, targetColor, Time.deltaTime * transitionSpeed);
            yield return null;
        }
        image.color = targetColor;
    }

    private System.Collections.IEnumerator FlashEffect()
    {
        for (int i = 0; i < flashCount; i++)
        {
            image.enabled = false;
            yield return new WaitForSeconds(flashDuration);
            image.enabled = true;
            yield return new WaitForSeconds(flashDuration);
        }
    }
}
