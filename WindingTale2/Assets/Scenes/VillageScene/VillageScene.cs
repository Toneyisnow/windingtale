using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    /// <summary>
    /// The GlobalVariables key the shop is entered on: the index of the spot the cursor
    /// stood on, which the shop turns into which picture to show.
    /// </summary>
    public const string ShopIndexVariableName = "ShopIndex";

    /// <summary>
    /// The GlobalVariables key the shop hands the village back on. Present only on a
    /// return from a shop, it carries the round the cursor was walking and the spot it
    /// went in on, so the village can rebuild that round and play the zoom in reverse.
    /// A normal entry from a battle never has it, so it fades in the ordinary way.
    /// </summary>
    public const string ShopReturnVariableName = "VillageShopReturn";

    /// <summary>
    /// What the shop hands the village back on ShopReturnVariableName. The spot list is
    /// world positions worked out against the resting camera, which is the same every
    /// load, so restoring them lands the cursor exactly where it left.
    /// </summary>
    public class ShopReturnInfo
    {
        public List<Vector3> Spots;
        public int SpotIndex;
        public Vector3 Spot;
    }

    /// <summary>Time the village takes to come up out of black, in seconds.</summary>
    public float fadeInDuration = 2.0f;

    /// <summary>
    /// Time the camera takes to pull into a shop, and to pull back out of one on the way
    /// home. The shop's own fade is quicker, so the black the camera lands on has fully
    /// arrived before the shop starts showing through it.
    /// </summary>
    public float shopTransitionDuration = 1.0f;

    /// <summary>
    /// How far the camera pulls in when entering a shop: 3 means the spot ends up three
    /// times its resting size, filling the middle of the screen.
    /// </summary>
    public float shopZoomFactor = 3.0f;

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
    /// How much further out than the cursor the background sits. The canvas fills the
    /// view whatever this is, so it does not change the resting picture -- but it does
    /// set how hard the background zooms when the enter-shop camera pulls in: the closer
    /// it is to the cursor plane, the more of the cursor's own zoom it shares, so keep it
    /// small to make the picture pull in nearly as much as the cursor. Only floor is
    /// leaving enough gap that the cursor stays clear in front of the picture.
    /// </summary>
    public float backgroundDistanceBehindCursor = 2.0f;

    /// <summary>
    /// Where a scattered spot may land, in viewport coordinates. Kept off all four
    /// edges so the figure standing on it is not half off the screen.
    /// </summary>
    private const float SpotMarginX = 0.15f;
    private const float SpotMarginBottom = 0.15f;
    private const float SpotMarginTop = 0.65f;

    /// <summary>The party that walked into the village, as the won battle left them.</summary>
    public GameRecord Record { get; private set; }

    private int villageId = 0;

    private Transform cursor = null;

    private readonly GameObject[] clips = new GameObject[ClipNames.Length];

    /// <summary>The round the cursor walks, in world space. Index 0 is the middle of the screen.</summary>
    private List<Vector3> spots = null;

    private int spotIndex = 0;

    private ScreenFader fader = null;

    /// <summary>True while the camera is pulling into a shop; input is shut off until the scene changes.</summary>
    private bool transitioning = false;

    void Start()
    {
        ShopReturnInfo returnInfo = GlobalVariables.Take<ShopReturnInfo>(ShopReturnVariableName);
        Init(GlobalVariables.Take<GameRecord>(RecordVariableName), returnInfo);
    }

    /// <summary>
    /// Builds the village around the record the last battle produced. Public and
    /// record-driven so the scene can be tested on its own: opened straight from the
    /// editor there is no record, and it falls back to the first village.
    /// </summary>
    public void Init(GameRecord record, ShopReturnInfo returnInfo = null)
    {
        if (record == null)
        {
            // Opened on its own rather than won into. Nothing to show a party from, but
            // the village itself still stands.
            Debug.LogWarning("No game record to enter the village with; showing the first village.");
            record = new GameRecord() { ChapterId = 1, Friends = new List<CreatureMapRecord>() };
        }

        this.Record = record;
        this.villageId = GetVillageId(record.ChapterId);

        ShowBackground(this.villageId);
        SetupCursor();

        if (returnInfo != null && returnInfo.Spots != null && returnInfo.Spots.Count > 0)
        {
            // Come back out of a shop: put the cursor back on the round it left and pull
            // the camera back out of the shop, the entering zoom run backwards.
            spots = returnInfo.Spots;
            spotIndex = Mathf.Clamp(returnInfo.SpotIndex, 0, spots.Count - 1);
            cursor.position = spots[spotIndex];
            StartCoroutine(ExitShopRoutine(returnInfo.Spot));
        }
        else
        {
            PlaceSpots();

            fader = ScreenFader.Create(1.0f);
            fader.FadeTo(0.0f, fadeInDuration);
        }
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
    /// Puts the village picture up on the scene's background canvas as a flat panel
    /// standing out in the world in front of the camera. A screen-space canvas would
    /// ride along with the camera and so never appear to move, but the enter-shop zoom
    /// pulls the camera into the picture, so the picture has to stand still in the world
    /// for the camera to close on it. The panel is sized and placed to fill the resting
    /// view exactly, so with the camera at rest it looks like an ordinary backdrop, and
    /// it sits further out than the cursor so the cursor stays in front of it.
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

        //// Fill the panel whatever the resolution turns out to be.
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Camera camera = Camera.main;
        Canvas canvas = image.canvas;
        if (canvas != null && camera != null)
        {
            float planeDistance = cursorDistance + backgroundDistanceBehindCursor;
            float height = 2.0f * planeDistance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * camera.aspect;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            canvasRect.localScale = Vector3.one;
            canvasRect.position = camera.transform.position + camera.transform.forward * planeDistance;
            canvasRect.rotation = camera.transform.rotation;
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
        if (spots == null || spots.Count == 0 || transitioning || fader == null || fader.IsFading)
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
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartCoroutine(EnterShopRoutine());
        }
    }

    private void MoveToSpot(int index)
    {
        spotIndex = (index + spots.Count) % spots.Count;
        cursor.position = spots[spotIndex];
    }

    /// <summary>
    /// Enters the shop the cursor is standing on: the whole picture goes to black over
    /// shopTransitionDuration while the camera pulls in to face the spot, and once it
    /// lands the shop is loaded on that black, entered on the spot's index.
    /// </summary>
    private IEnumerator EnterShopRoutine()
    {
        transitioning = true;

        Camera camera = Camera.main;
        Vector3 spot = spots[spotIndex];
        Vector3 from = camera.transform.position;
        Vector3 to = ZoomedCameraPosition(camera, spot);

        fader.FadeTo(1.0f, shopTransitionDuration);
        yield return LerpCameraPosition(camera, from, to, shopTransitionDuration);

        GlobalVariables.Set(RecordVariableName, Record);
        GlobalVariables.Set(ShopIndexVariableName, spotIndex);
        GlobalVariables.Set(ShopReturnVariableName, new ShopReturnInfo
        {
            Spots = spots,
            SpotIndex = spotIndex,
            Spot = spot,
        });

        SceneManager.LoadScene("ShoppingScene", LoadSceneMode.Single);
    }

    /// <summary>
    /// The entering zoom run backwards, for a return from a shop: the camera starts
    /// pulled in on the spot with the screen black, then backs out to its resting place
    /// as the black clears, leaving the whole village showing again.
    /// </summary>
    private IEnumerator ExitShopRoutine(Vector3 spot)
    {
        transitioning = true;

        Camera camera = Camera.main;
        Vector3 to = camera.transform.position;
        Vector3 from = ZoomedCameraPosition(camera, spot);
        camera.transform.position = from;

        fader = ScreenFader.Create(1.0f);
        fader.FadeTo(0.0f, shopTransitionDuration);
        yield return LerpCameraPosition(camera, from, to, shopTransitionDuration);

        transitioning = false;
    }

    /// <summary>
    /// Where the camera stands to face <paramref name="spot"/> head on, pulled in by
    /// shopZoomFactor. Facing it is a straight sideways slide to sit over it -- the
    /// camera never turns -- and pulling in closes the resting gap to the cursor plane
    /// down to a fraction of its length. Must be read with the camera at rest.
    /// </summary>
    private Vector3 ZoomedCameraPosition(Camera camera, Vector3 spot)
    {
        float restingZ = camera.transform.position.z;
        float zoomedZ = restingZ + cursorDistance * (1.0f - 1.0f / shopZoomFactor);
        return new Vector3(spot.x, spot.y, zoomedZ);
    }

    private IEnumerator LerpCameraPosition(Camera camera, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(elapsed / duration));
            camera.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        camera.transform.position = to;
    }
}
