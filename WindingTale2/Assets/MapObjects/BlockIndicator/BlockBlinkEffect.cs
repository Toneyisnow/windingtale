using System.Collections;
using UnityEngine;

public class BlockBlinkEffect : MonoBehaviour
{
    public MeshRenderer cursorRenderer; // 光标的 Mesh Renderer 组件
    public float blinkSpeed = 2.0f; // 闪烁速度（秒）
    private Material cursorMaterial;
    private bool isBlinking = false;

    void Start()
    {
        cursorRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();
        if (cursorRenderer != null)
        {
            cursorMaterial = cursorRenderer.material;

            SetRenderingModeTransparent();
            StartCoroutine(BlinkCursor());
        }
    }

    private void SetRenderingModeFade()
    {
        cursorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        cursorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        cursorMaterial.SetInt("_ZWrite", 0);
        cursorMaterial.SetOverrideTag("RenderType", "Transparent");
        cursorMaterial.EnableKeyword("_ALPHABLEND_ON");
        cursorMaterial.DisableKeyword("_ALPHATEST_ON");
        cursorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");

    }

    private void SetRenderingModeTransparent()
    {
        cursorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        cursorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        cursorMaterial.SetInt("_ZWrite", 0);
        cursorMaterial.SetOverrideTag("RenderType", "Transparent");
        cursorMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        cursorMaterial.DisableKeyword("_ALPHABLEND_ON");
        cursorMaterial.DisableKeyword("_ALPHATEST_ON");

    }

    private IEnumerator BlinkCursor()
    {
        isBlinking = true;
        while (isBlinking)
        {
            // 逐渐淡出
            float alpha = 1f;
            while (alpha >= 0f)
            {
                alpha -= Time.deltaTime / blinkSpeed;
                SetCursorAlpha(alpha);
                yield return null;
            }

            alpha = 0f;
            while (alpha <= 1f)
            {
                alpha += Time.deltaTime / blinkSpeed;
                SetCursorAlpha(alpha);
                yield return null;
            }
        }
    }

    private void SetCursorAlpha(float alpha)
    {
        Debug.LogWarning("SetCursorAlpha: alpha: " + alpha);
        if (cursorMaterial != null)
        {
            Color color = cursorMaterial.color;
            color.a = alpha;
            cursorMaterial.color = color;
        }
    }
}
