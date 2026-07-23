using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WindingTale.Core.Files;
using WindingTale.UI.Utils;

/// <summary>
/// A single shop, reached from the village by walking the cursor onto its spot and
/// pressing space. The village pulls its camera in on that spot and hands the shop over
/// on black (VillageScene.EnterShopRoutine), so the shop opens by fading its picture up
/// out of that black, and closes the same way: on Esc it fades back to black and hands
/// the village back, which pulls its camera back out to where it started.
///
/// Which shop this is comes in as an index -- the village spot the cursor entered on --
/// and picks the picture to show. The party is carried through untouched so the village
/// has it again on the way back; buying and selling is not wired up yet.
/// </summary>
public class ShoppingScene : MonoBehaviour
{
    /// <summary>The GlobalVariables key the village hands the entered spot's index over on.</summary>
    public const string ShopIndexVariableName = "ShopIndex";

    /// <summary>The GlobalVariables key the party is carried through on, the village's own key.</summary>
    public const string RecordVariableName = "GameRecord";

    /// <summary>How many shop pictures exist to be shown, VillageShop-01-1 up to this.</summary>
    private const int ShopPictureCount = 5;

    /// <summary>Time the shop takes to fade its picture up, and back down on the way out.</summary>
    public float fadeDuration = 0.3f;

    private GameRecord record = null;

    private int shopIndex = 0;

    private ScreenFader fader = null;

    /// <summary>True once the shop is fading out to the village; input is shut off from there.</summary>
    private bool leaving = false;

    void Start()
    {
        shopIndex = GlobalVariables.Get<int>(ShopIndexVariableName);
        record = GlobalVariables.Get<GameRecord>(RecordVariableName);

        ShowBackground(shopIndex);

        fader = ScreenFader.Create(1.0f);
        fader.FadeTo(0.0f, fadeDuration);
    }

    void Update()
    {
        if (leaving || fader == null || fader.IsFading)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            LeaveShop();
        }
    }

    /// <summary>
    /// Leaves for the village: the picture fades to black and the village is loaded on
    /// it. The party and the return marker are handed back untouched -- the village put
    /// the marker up when it entered the shop -- so the village knows to back its camera
    /// out of this shop rather than fade in fresh.
    /// </summary>
    private void LeaveShop()
    {
        leaving = true;

        GlobalVariables.Set(RecordVariableName, record);

        fader.FadeTo(1.0f, fadeDuration, () =>
        {
            SceneManager.LoadScene("VillageScene", LoadSceneMode.Single);
        });
    }

    /// <summary>
    /// Puts the shop picture up full-screen, chosen by the spot the village entered on.
    /// Rendered through the camera rather than as an overlay so the fader's black
    /// curtain, which is an overlay, still lands on top of it.
    /// </summary>
    private void ShowBackground(int shopIndex)
    {
        Image image = FindBackgroundImage();
        if (image == null)
        {
            Debug.LogWarning("Shopping scene has no background image to draw the shop on.");
            return;
        }

        //// Spots are numbered from zero, the pictures from one, so the middle spot shows
        //// VillageShop-01-1. Clamped to the pictures that exist.
        int pictureNo = Mathf.Clamp(shopIndex + 1, 1, ShopPictureCount);
        string spritePath = string.Format("Shops/VillageShop-01-{0}", pictureNo);
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Cannot load shop background: " + spritePath);
            return;
        }

        image.sprite = sprite;

        //// Fill the screen whatever the resolution turns out to be.
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Canvas canvas = image.canvas;
        if (canvas != null && Camera.main != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 32.0f;
        }
    }

    private Image FindBackgroundImage()
    {
        GameObject background = GameObject.Find("Background");
        if (background != null)
        {
            Image image = background.GetComponentInChildren<Image>(true);
            if (image != null)
            {
                return image;
            }
        }

        return FindFirstObjectByType<Image>();
    }
}
