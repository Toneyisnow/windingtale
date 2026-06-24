using System.Collections;
using UnityEngine;

public class BlockBlinkEffect : MonoBehaviour
{
    public MeshRenderer cursorRenderer;
    public float blinkSpeed = 2.0f;   // seconds for one fade (down or up)

    // The glassy indicator never fully disappears; it shimmers between these.
    public float minAlpha = 0.15f;
    public float maxAlpha = 0.6f;

    // Glassy blue-white tint. RGB only; alpha is driven by the blink.
    public Color tint = new Color(0.6f, 0.85f, 1.0f, 1.0f);

    private Material cursorMaterial;
    private bool isBlinking = false;

    void Start()
    {
        cursorRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();
        if (cursorRenderer != null)
        {
            // .material gives this indicator its own instance, so each block blinks
            // independently and we never mutate the shared source material.
            cursorMaterial = cursorRenderer.material;

            ApplyGlassMaterial();
            StartCoroutine(BlinkCursor());
        }
    }

    private void ApplyGlassMaterial()
    {
        Shader glass = Shader.Find("Custom/MoveIndicatorGlass");
        if (glass != null)
        {
            cursorMaterial.shader = glass;
        }

        Color c = tint;
        c.a = maxAlpha;
        cursorMaterial.color = c;

        if (cursorMaterial.HasProperty("_Glossiness")) cursorMaterial.SetFloat("_Glossiness", 0.95f);
        if (cursorMaterial.HasProperty("_Metallic")) cursorMaterial.SetFloat("_Metallic", 0.4f);
    }

    private IEnumerator BlinkCursor()
    {
        isBlinking = true;
        while (isBlinking)
        {
            // Fade down to minAlpha.
            float alpha = maxAlpha;
            while (alpha > minAlpha)
            {
                alpha -= (maxAlpha - minAlpha) * Time.deltaTime / blinkSpeed;
                SetCursorAlpha(alpha);
                yield return null;
            }

            // Fade back up to maxAlpha.
            alpha = minAlpha;
            while (alpha < maxAlpha)
            {
                alpha += (maxAlpha - minAlpha) * Time.deltaTime / blinkSpeed;
                SetCursorAlpha(alpha);
                yield return null;
            }
        }
    }

    private void SetCursorAlpha(float alpha)
    {
        if (cursorMaterial != null)
        {
            Color color = cursorMaterial.color;
            color.a = Mathf.Clamp(alpha, minAlpha, maxAlpha);
            cursorMaterial.color = color;
        }
    }
}
