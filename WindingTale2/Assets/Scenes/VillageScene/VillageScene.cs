using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Files;
using WindingTale.UI.Utils;

/// <summary>
/// The village between two chapters. It is handed over on black by the field scene's
/// quitting animation (GameMain.EnterVillage), so it opens by fading the village art up
/// out of that black, with the party's cursor standing in the middle of it.
///
/// The cursor walks a fixed round of spots on the picture: the middle one it starts on,
/// plus a handful scattered over the rest of the screen, which left and right cycle
/// through. Where those spots eventually lead -- the shop, the inn, the next chapter --
/// is not wired up yet.
/// </summary>
public class VillageScene : MonoBehaviour
{
    /// <summary>
    /// The GlobalVariables key the field scene hands the finished battle over on. A
    /// one-shot handover: Init takes it, so a later village load cannot pick up a record
    /// from a battle two chapters ago.
    /// </summary>
    public const string RecordVariableName = "GameRecord";

    /// <summary>Time the village takes to come up out of black, in seconds.</summary>
    public float fadeInDuration = 2.0f;

    /// <summary>
    /// How far in front of the camera the cursor stands. The background canvas is put
    /// further out than this, so the cursor is always in front of the picture.
    /// </summary>
    public float cursorDistance = 12.0f;

    /// <summary>
    /// Size of the cursor relative to the battlefield icon it is built from, which is
    /// drawn for a map tile and so is too big for a village screen.
    /// </summary>
    public float cursorScale = 0.72f;

    /// <summary>How many spots to scatter, on top of the middle one the cursor starts on.</summary>
    public int randomSpotCount = 4;

    /// <summary>Creature 001's icon is what the cursor wears -- the hero, leading the party in.</summary>
    private const int CursorAnimationId = 1;

    /// <summary>
    /// Hundredths of a second each idle frame is held for. The same value
    /// CreatureIdleSynced runs the battlefield icons at, so the two stay in step.
    /// </summary>
    private const int IdleAnimationSpeed = 30;

    /// <summary>The three clip holders the scene carries, one per idle frame.</summary>
    private static readonly string[] ClipNames = { "icon01", "icon02", "icon03" };

    /// <summary>
    /// How much further out than the cursor the background sits. Only has to be enough
    /// to keep the picture behind the cursor; the canvas fills the view either way.
    /// </summary>
    private const float BackgroundDistanceBehindCursor = 20.0f;

    /// <summary>
    /// Where a scattered spot may land, in viewport coordinates. Kept off all four
    /// edges so the figure standing on it is not half off the screen.
    /// </summary>
    private const float SpotMarginX = 0.15f;
    private const float SpotMarginBottom = 0.15f;
    private const float SpotMarginTop = 0.65f;

    /// <summary>The party that walked into the village, as the won battle left them.</summary>
    public GameRecord Record { get; private set; }

    private Transform cursor = null;

    private readonly GameObject[] clips = new GameObject[ClipNames.Length];

    /// <summary>The round the cursor walks, in world space. Index 0 is the middle of the screen.</summary>
    private List<Vector3> spots = null;

    private int spotIndex = 0;

    private ScreenFader fader = null;

    void Start()
    {
        Init(GlobalVariables.Take<GameRecord>(RecordVariableName));
    }

    /// <summary>
    /// Builds the village around the record the last battle produced. Public and
    /// record-driven so the scene can be tested on its own: opened straight from the
    /// editor there is no record, and it falls back to the first village.
    /// </summary>
    public void Init(GameRecord record)
    {
        if (record == null)
        {
            // Opened on its own rather than won into. Nothing to show a party from, but
            // the village itself still stands.
            Debug.LogWarning("No game record to enter the village with; showing the first village.");
            record = new GameRecord() { ChapterId = 1, Friends = new List<CreatureMapRecord>() };
        }

        this.Record = record;

        ShowBackground(GetVillageId(record.ChapterId));
        SetupCursor();
        PlaceSpots();

        fader = ScreenFader.Create(1.0f);
        fader.FadeTo(0.0f, fadeInDuration);
    }

    void Update()
    {
        AnimateCursor();
        HandleInput();
    }

    /// <summary>
    /// Which of the three villages a chapter belongs to: one per stretch of ten
    /// chapters. Anything past the third stretch stays in the third village -- there is
    /// no fourth picture to show.
    /// </summary>
    private static int GetVillageId(int chapterId)
    {
        if (chapterId < 10)
        {
            return 1;
        }

        if (chapterId < 20)
        {
            return 2;
        }

        return 3;
    }

    /// <summary>
    /// Puts the village picture up on the scene's background canvas, and moves that
    /// canvas out of screen-space overlay: an overlay canvas draws over everything in
    /// the scene, the cursor included, so the picture has to be rendered through the
    /// camera to end up behind it.
    /// </summary>
    private void ShowBackground(int villageId)
    {
        Image image = FindBackgroundImage();
        if (image == null)
        {
            Debug.LogWarning("Village scene has no background image to draw the village on.");
            return;
        }

        string spritePath = string.Format("Village/Village-{0:D2}", villageId);
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Cannot load village background: " + spritePath);
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
            canvas.planeDistance = cursorDistance + BackgroundDistanceBehindCursor;
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

    /// <summary>
    /// Dresses the scene's VillageCursor in creature 001's battlefield icon: three
    /// voxel models, one per idle frame, the same ones GameMap.AddCreatureUI hangs off a
    /// creature. AnimateCursor is what cycles between them.
    /// </summary>
    private void SetupCursor()
    {
        GameObject cursorObject = GameObject.Find("VillageCursor");
        if (cursorObject == null)
        {
            cursorObject = new GameObject("VillageCursor");
        }

        cursor = cursorObject.transform;
        cursor.localScale = Vector3.one * cursorScale;

        //// The icons are modelled facing away from the battlefield camera, which looks
        //// down the map from behind the party. The village camera stands in front of the
        //// cursor instead, so the model has to be turned round to face the player.
        cursor.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        for (int i = 0; i < ClipNames.Length; i++)
        {
            clips[i] = FindOrAddChild(cursor, ClipNames[i]);
            AttachIcon(clips[i].transform, i + 1);
        }
    }

    private static GameObject FindOrAddChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject added = new GameObject(name);
        added.transform.SetParent(parent, false);
        return added;
    }

    /// <summary>
    /// Loads one idle frame's model under <paramref name="parent"/>. A clip holder that
    /// already carries a model is left alone, so a model wired up in the editor wins
    /// over the one loaded here.
    /// </summary>
    private static void AttachIcon(Transform parent, int frameNo)
    {
        if (parent.childCount > 0)
        {
            return;
        }

        string iconFilePath = string.Format("Icons/{0:D3}/Icon_{0:D3}_{1:D2}", CursorAnimationId, frameNo);
        GameObject prefab = Resources.Load<GameObject>(iconFilePath);
        if (prefab == null)
        {
            Debug.LogWarning("Cannot load village cursor icon: " + iconFilePath);
            return;
        }

        GameObject icon = Instantiate(prefab);
        icon.transform.SetParent(parent, false);
        icon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    /// <summary>
    /// Lays out the round the cursor walks: the middle of the screen, where it starts,
    /// and randomSpotCount more scattered over the picture. Each is a screen position
    /// turned into a world position at the cursor's distance from the camera, so the
    /// round comes out the same shape whatever the resolution.
    /// </summary>
    private void PlaceSpots()
    {
        Camera camera = Camera.main;
        if (camera == null || cursor == null)
        {
            return;
        }

        spots = new List<Vector3>();
        spots.Add(ViewportToCursorPoint(camera, 0.5f, 0.5f));

        for (int i = 0; i < randomSpotCount; i++)
        {
            spots.Add(ViewportToCursorPoint(
                camera,
                Random.Range(SpotMarginX, 1.0f - SpotMarginX),
                Random.Range(SpotMarginBottom, SpotMarginTop)));
        }

        spotIndex = 0;
        cursor.position = spots[spotIndex];
    }

    private Vector3 ViewportToCursorPoint(Camera camera, float viewportX, float viewportY)
    {
        return camera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, cursorDistance));
    }

    /// <summary>
    /// The idle animation, frame for frame what CreatureIdleSynced plays on the
    /// battlefield: clips 01, 02, 03, 02, each held for IdleAnimationSpeed hundredths of
    /// a second. Driven off Time.fixedTime rather than a timer of its own, which is what
    /// keeps every icon in the game breathing in step.
    /// </summary>
    private void AnimateCursor()
    {
        int timeDouble = (int)(Time.fixedTime * 100) / IdleAnimationSpeed;
        int frame = timeDouble % 4;

        SetClipVisible(0, frame == 0);
        SetClipVisible(1, frame == 1 || frame == 3);
        SetClipVisible(2, frame == 2);
    }

    private void SetClipVisible(int clipIndex, bool visible)
    {
        GameObject clip = clips[clipIndex];
        if (clip != null && clip.activeSelf != visible)
        {
            clip.SetActive(visible);
        }
    }

    /// <summary>
    /// Left and right walk the round, wrapping at both ends. Nothing is accepted until
    /// the village is fully up: a key pressed during the fade in is the tail of whatever
    /// ended the battle, not an answer to this screen.
    /// </summary>
    private void HandleInput()
    {
        if (spots == null || spots.Count == 0 || fader == null || fader.IsFading)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveToSpot(spotIndex + 1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveToSpot(spotIndex - 1);
        }
    }

    private void MoveToSpot(int index)
    {
        spotIndex = (index + spots.Count) % spots.Count;
        cursor.position = spots[spotIndex];
    }
}
