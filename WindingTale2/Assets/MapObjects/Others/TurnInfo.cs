using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The turn-start banner. Expands from a tiny box in the middle of the screen
/// (X axis first, then Y), holds, then slides straight down out of the screen.
/// </summary>
public class TurnInfo : MonoBehaviour
{
    public GameObject turnValueObj = null;

    #region Animation Parameters

    /// <summary>Scale both axes start from, as a ratio of the final size.</summary>
    public float startRatio = 0.08f;

    /// <summary>Time to expand on one axis, in seconds. Each axis takes this long.</summary>
    public float expandDuration = 0.25f;

    /// <summary>
    /// Tension of the expand curve, as a multiple of the average speed: the curve
    /// creeps at both ends and peaks at this many times average speed in the middle.
    /// 1 is linear, 5 gives a hard snap.
    /// </summary>
    public float expandPeakSpeed = 3.0f;

    /// <summary>How long the fully expanded banner stays on screen, in seconds.</summary>
    public float holdDuration = 1.0f;

    /// <summary>Time for the banner to fly straight down out of the screen, in seconds.</summary>
    public float flyOutDuration = 0.5f;

    #endregion

    /// <summary>True while the animation is running; the caller waits on this.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Container holding the visible parts, so the whole banner can be scaled and
    /// moved as one without touching the Canvas transform (which the CanvasScaler drives).
    /// </summary>
    private RectTransform panel = null;

    void Awake()
    {
        BuildPanel();
    }

    /// <summary>
    /// Wraps the canvas children in a screen-sized, center-pivoted panel. Scaling that
    /// panel scales the banner around the middle of the screen.
    /// </summary>
    private void BuildPanel()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        // The CanvasScaler owns the canvas scale, and the prefab ships it at zero.
        canvas.transform.localScale = Vector3.one;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in canvas.transform)
        {
            children.Add(child);
        }

        GameObject panelObj = new GameObject("Panel", typeof(RectTransform));
        panelObj.layer = canvas.gameObject.layer;

        panel = panelObj.GetComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        children.ForEach(child => child.SetParent(panel, false));
    }

    /// <summary>
    /// Plays the full show/hide animation for the given turn number, then destroys
    /// this object. The number is rendered as two digits, e.g. turn 2 shows "02".
    /// </summary>
    public void Play(int turnNo)
    {
        SetTurnValue(turnNo);

        if (panel == null)
        {
            Destroy(gameObject);
            return;
        }

        IsPlaying = true;
        StartCoroutine(Animate());
    }

    private void SetTurnValue(int turnNo)
    {
        if (turnValueObj == null)
        {
            return;
        }

        string text = Mathf.Abs(turnNo % 100).ToString("D2");

        TextMeshProUGUI label = turnValueObj.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = text;
        }
    }

    private IEnumerator Animate()
    {
        panel.anchoredPosition = Vector2.zero;
        panel.localScale = new Vector3(startRatio, startRatio, 1.0f);

        //// 1. Expand on the X axis, then on the Y axis, both slow-fast-slow.
        yield return ExpandAxis(true);
        yield return ExpandAxis(false);

        //// 2. Stay fully open for a moment.
        yield return new WaitForSeconds(holdDuration);

        //// 3. Fly straight down until the whole banner is off the bottom of the screen.
        float flyDistance = panel.rect.height;
        float elapsed = 0.0f;
        while (elapsed < flyOutDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / flyOutDuration);
            panel.anchoredPosition = new Vector2(0.0f, -flyDistance * ratio);
            yield return null;
        }

        IsPlaying = false;
        Destroy(gameObject);
    }

    private IEnumerator ExpandAxis(bool isXAxis)
    {
        float elapsed = 0.0f;
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float value = Mathf.Lerp(startRatio, 1.0f, EaseInOut(Mathf.Clamp01(elapsed / expandDuration)));

            Vector3 scale = panel.localScale;
            if (isXAxis)
            {
                scale.x = value;
            }
            else
            {
                scale.y = value;
            }
            panel.localScale = scale;

            yield return null;
        }
    }

    /// <summary>
    /// Slow at both ends, accelerating hard through the middle. This is the sigmoid
    /// t^p / (t^p + (1-t)^p), whose slope at the midpoint is exactly p/2 times the
    /// average slope -- so p = 2 * expandPeakSpeed makes the parameter mean what it says.
    /// </summary>
    private float EaseInOut(float ratio)
    {
        float power = 2.0f * Mathf.Max(0.5f, expandPeakSpeed);

        float rise = Mathf.Pow(ratio, power);
        float fall = Mathf.Pow(1.0f - ratio, power);

        //// Both terms underflow to zero at extreme powers; fall back to the raw ratio.
        float total = rise + fall;
        return total > 0.0f ? rise / total : ratio;
    }
}
