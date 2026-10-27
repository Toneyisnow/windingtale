using UnityEngine;
using UnityEngine.EventSystems;
using WindingTale.UI.Audio;

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

    /// <summary>How far the keyboard-selected button floats up and down, in canvas units, and how fast (radians a second).</summary>
    public float floatHeight = 3f;
    public float floatSpeed = 4.8f;

    /// <summary>Told when the pointer comes onto the button, so the keyboard selection follows the mouse.</summary>
    public System.Action<TitleButton> Hovered;

    private Vector2 basePosition;
    private bool selected = false;
    private float floatTime = 0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = imageObj.GetComponent<UnityEngine.UI.Image>();
        originalScale = rectTransform.localScale;
        originalColor = image.color;
        basePosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (!selected)
        {
            return;
        }

        floatTime += Time.deltaTime;
        rectTransform.anchoredPosition = basePosition + Vector2.up * (Mathf.Sin(floatTime * floatSpeed) * floatHeight);
    }

    /// <summary>The keyboard selection: the selected button floats up and down; the others rest in place.</summary>
    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
        floatTime = 0f;
        if (!isSelected)
        {
            rectTransform.anchoredPosition = basePosition;
        }
    }

    /// <summary>
    /// A confirm key on the selected button: the same press as a click -- the sound, the
    /// button pressed in and let back out -- and then the button's own click.
    /// </summary>
    public void Press()
    {
        SoundEffects.Play(SoundEffect.DialogConfirm);
        StopAllCoroutines();
        StartCoroutine(PressAndRelease());
        UnityEngine.UI.Button button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.Invoke();
        }
    }

    private System.Collections.IEnumerator PressAndRelease()
    {
        StartCoroutine(ChangeColor(clickColor));
        yield return ScaleTo(originalScale * clickScaleFactor);
        StartCoroutine(ChangeColor(originalColor));
        yield return ScaleTo(originalScale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered?.Invoke(this);
        SoundEffects.Play(SoundEffect.DialogCursorMove);
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
        SoundEffects.Play(SoundEffect.DialogConfirm);
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
