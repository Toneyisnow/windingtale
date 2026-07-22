using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A full-screen black curtain that any scene can raise or lower without having to
/// carry a canvas for it. Everything is built in code on a top-most overlay canvas,
/// so the curtain covers the 3D view and every UI canvas underneath it, and it
/// swallows clicks meant for the UI while it is up.
///
/// SceneTransition is the prefab-driven version of the same idea, dropped into the
/// Title scene and wired up in the editor. This one is for the fades a scene did not
/// plan for and cannot size in advance -- the game-over quit above all.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    /// <summary>Overlay canvases draw in sorting order, so take the top of the range.</summary>
    private const int CurtainSortingOrder = 32767;

    /// <summary>True while a fade is running; the caller waits on this.</summary>
    public bool IsFading { get; private set; }

    private Image curtain = null;

    /// <summary>
    /// Builds a fader on a new object in the active scene, with the curtain already
    /// sitting at <paramref name="startAlpha"/> (0 fully clear, 1 fully black). The
    /// object belongs to the scene, so a single-mode scene load takes it with it.
    /// </summary>
    public static ScreenFader Create(float startAlpha)
    {
        GameObject faderObject = new GameObject("ScreenFader");

        Canvas canvas = faderObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CurtainSortingOrder;
        faderObject.AddComponent<GraphicRaycaster>();

        GameObject curtainObject = new GameObject("Curtain", typeof(Image));
        curtainObject.transform.SetParent(faderObject.transform, false);

        //// Stretch to all four corners, whatever the resolution turns out to be.
        RectTransform rect = curtainObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        ScreenFader fader = faderObject.AddComponent<ScreenFader>();
        fader.curtain = curtainObject.GetComponent<Image>();
        fader.SetAlpha(startAlpha);

        return fader;
    }

    /// <summary>
    /// Fades the curtain to <paramref name="targetAlpha"/> over the given number of
    /// seconds and calls <paramref name="onComplete"/> once it lands. A second call
    /// replaces whatever fade was running.
    /// </summary>
    public void FadeTo(float targetAlpha, float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(Fade(Mathf.Clamp01(targetAlpha), duration, onComplete));
    }

    private IEnumerator Fade(float targetAlpha, float duration, Action onComplete)
    {
        IsFading = true;

        float startAlpha = curtain.color.a;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(targetAlpha);
        IsFading = false;

        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        curtain.color = new Color(0.0f, 0.0f, 0.0f, Mathf.Clamp01(alpha));
    }
}
