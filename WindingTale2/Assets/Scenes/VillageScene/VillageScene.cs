using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.UI.Utils;

/// <summary>
/// The village between two chapters. It is handed over on black by the field scene's
/// quitting animation (GameMain.EnterVillage), so it opens by fading the village art up
/// out of that black, with the party's cursor resting on pos 1.
///
/// The cursor walks a fixed round of spots read from a per-village map, six by position
/// index: pos 0 is the way on to the next chapter; pos 1-5 are the shops, each matched to
/// the shop of the same index, and the cursor rests on pos 1 to begin with. Left and
/// right cycle pos 0 through pos 4; pos 5 waits on a special route not yet wired up.
/// Entering pos 0 (the next battlefield) is also still a placeholder -- see OnProceed.
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
    /// How many spots the left/right keys walk: pos 0 through pos 4. The last spot (pos 5)
    /// is left out of the round -- it is reached only by the secret sequence.
    /// </summary>
    private const int CyclableSpotCount = 5;

    /// <summary>The secret spot, pos 5, the left/right walk never lands on.</summary>
    private const int SecretSpotIndex = 5;

    /// <summary>
    /// Where the cursor may stand in each village, in world coordinates on the cursor
    /// plane. One entry per village (1-3), each holding six spots by position index: index
    /// 0 is the way on to the next chapter (see OnProceed), indices 1-5 are the shops,
    /// each matched to the shop of the same index. Index 5 is only reachable by a special
    /// route, not the left/right walk. Replaces the spots that used to be scattered at
    /// random.
    /// </summary>
    private static readonly Dictionary<int, Vector2[]> VillageSpotMaps = new Dictionary<int, Vector2[]>
    {
        {
            1, new[]
            {
                new Vector2(-6.4f, -5.2f), // pos 0 -- proceed to next chapter
                new Vector2(-8.2f, -1.8f), // pos 1 -- shop 1
                new Vector2(-8.9f, 3.3f),  // pos 2 -- shop 2
                new Vector2(3.2f, 1.8f),   // pos 3 -- shop 3
                new Vector2(1.2f, -3.6f),  // pos 4 -- shop 4
                new Vector2(-1.8f, 5.7f),  // pos 5 -- shop 5, special route
            }
        },
        // Villages 2 and 3 reuse village 1's layout as a placeholder; retune once their
        // own pictures are drawn.
        {
            2, new[]
            {
                new Vector2(-6.4f, -5.2f),
                new Vector2(-8.2f, -1.8f),
                new Vector2(-8.9f, 3.3f),
                new Vector2(3.2f, 1.8f),
                new Vector2(1.2f, -3.6f),
                new Vector2(-1.8f, 5.7f),
            }
        },
        {
            3, new[]
            {
                new Vector2(-6.4f, -5.2f),
                new Vector2(-8.2f, -1.8f),
                new Vector2(-8.9f, 3.3f),
                new Vector2(3.2f, 1.8f),
                new Vector2(1.2f, -3.6f),
                new Vector2(-1.8f, 5.7f),
            }
        },
    };

    /// <summary>
    /// How much further out than the cursor the background sits. The canvas fills the
    /// view whatever this is, so it does not change the resting picture -- but it does
    /// set how hard the background zooms when the enter-shop camera pulls in: the closer
    /// it is to the cursor plane, the more of the cursor's own zoom it shares, so keep it
    /// small to make the picture pull in nearly as much as the cursor. Only floor is
    /// leaving enough gap that the cursor stays clear in front of the picture.
    /// </summary>
    public float backgroundDistanceBehindCursor = 2.0f;

    /// <summary>The party that walked into the village, as the won battle left them.</summary>
    public GameRecord Record { get; private set; }

    private int villageId = 0;

    private Transform cursor = null;

    private readonly GameObject[] clips = new GameObject[ClipNames.Length];

    /// <summary>The round the cursor walks, in world space. Index 0 is the middle of the screen.</summary>
    private List<Vector3> spots = null;

    private int spotIndex = 0;

    /// <summary>
    /// This chapter's hidden route to pos 5, or null if the chapter has none. The cursor
    /// must be on its start spot and the left/right presses entered in order to walk it.
    /// </summary>
    private SecretSequenceDefinition secretSequence = null;

    /// <summary>How many of the secret sequence's presses have been entered correctly so far.</summary>
    private int secretProgress = 0;

    /// <summary>The panel the village picture is drawn on, kept so the picture can be swapped.</summary>
    private Image backgroundImage = null;

    /// <summary>The ordinary village picture, shown when no secret is under way.</summary>
    private Sprite normalBackgroundSprite = null;

    /// <summary>
    /// The alternate picture shown while the secret sequence is being entered (and while the
    /// cursor rests on the secret spot). One per chapter by chapterId % 4, or null if this
    /// village has no secret art -- in which case the ordinary picture stays up.
    /// </summary>
    private Sprite secretBackgroundSprite = null;

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
        this.secretSequence = DefinitionStore.Instance.GetSecretSequenceDefinition(record.ChapterId);

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

        // Coming back onto the secret spot should already show the secret picture rather
        // than flip to it a frame later.
        RefreshBackground();
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

        // Keep the panel and both pictures so the secret sequence can swap the picture out
        // and back without touching the rest of the layout.
        this.backgroundImage = image;
        this.normalBackgroundSprite = sprite;
        this.secretBackgroundSprite = LoadSecretBackground(villageId, this.Record.ChapterId);

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

    /// <summary>
    /// Loads the secret picture for this chapter: Village-NN-secret-1 through -4, chosen by
    /// chapterId % 4 (with 0 wrapping to 4, so chapter 4 takes -4 not a missing -0). Returns
    /// null if the village has no secret art, which just leaves the ordinary picture up.
    /// </summary>
    private static Sprite LoadSecretBackground(int villageId, int chapterId)
    {
        int secretNo = chapterId % 4;
        if (secretNo == 0)
        {
            secretNo = 4;
        }

        string spritePath = string.Format("Village/Village-{0:D2}-secret-{1}", villageId, secretNo);
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning("Cannot load village secret background: " + spritePath);
        }

        return sprite;
    }

    /// <summary>
    /// Shows the secret picture while the secret sequence is being entered (and while the
    /// cursor rests on the secret spot), the ordinary one otherwise. A village with no
    /// secret art keeps its ordinary picture throughout.
    /// </summary>
    private void RefreshBackground()
    {
        if (backgroundImage == null)
        {
            return;
        }

        bool showSecret = (secretProgress > 0 || spotIndex == SecretSpotIndex) && secretBackgroundSprite != null;
        Sprite wanted = showSecret ? secretBackgroundSprite : normalBackgroundSprite;

        if (wanted != null && backgroundImage.sprite != wanted)
        {
            backgroundImage.sprite = wanted;
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

    /// <summary>The spot the cursor rests on when the village is first entered: pos 1.</summary>
    private const int StartSpotIndex = 1;

    /// <summary>
    /// Lays out the round the cursor walks from this village's spot map: six fixed spots
    /// by position index, each a world point on the cursor plane. The cursor rests on
    /// pos 1 to begin with, one step in from pos 0 (the way on to the next chapter).
    /// </summary>
    private void PlaceSpots()
    {
        Camera camera = Camera.main;
        if (camera == null || cursor == null)
        {
            return;
        }

        if (!VillageSpotMaps.TryGetValue(villageId, out Vector2[] map))
        {
            Debug.LogWarning("No spot map for village " + villageId + "; using village 1's.");
            map = VillageSpotMaps[1];
        }

        //// The map is world x/y on the cursor plane; take that plane's depth from the
        //// camera so every spot sits the same distance out, whatever the resolution.
        float planeZ = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, cursorDistance)).z;

        spots = new List<Vector3>(map.Length);
        foreach (Vector2 point in map)
        {
            spots.Add(new Vector3(point.x, point.y, planeZ));
        }

        spotIndex = StartSpotIndex;
        cursor.position = spots[spotIndex];
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
            // The jump to pos 5 takes the whole press; otherwise it is an ordinary step.
            if (!AdvanceSecretSequence(SecretOperation.Right))
            {
                MoveToSpot(spotIndex + 1);
            }

            // A press may have started, advanced, broken or finished the sequence; put the
            // right picture up for wherever that left things.
            RefreshBackground();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (!AdvanceSecretSequence(SecretOperation.Left))
            {
                MoveToSpot(spotIndex - 1);
            }

            RefreshBackground();
        }
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (spotIndex == 0)
            {
                //// Pos 0 is the way on to the next chapter, not a shop.
                OnProceed();
            }
            else
            {
                StartCoroutine(EnterShopRoutine());
            }
        }
    }

    /// <summary>
    /// Left and right walk pos 0 through pos 4, wrapping at both ends. Pos 5 is left out
    /// of the round -- it is reached by a special route, not the walk.
    /// </summary>
    private void MoveToSpot(int index)
    {
        spotIndex = (index + CyclableSpotCount) % CyclableSpotCount;
        cursor.position = spots[spotIndex];
    }

    /// <summary>
    /// Feeds one left/right press into this chapter's secret sequence, the hidden route to
    /// pos 5. The sequence has to begin on its own start spot and be entered in order; a
    /// wrong key breaks the attempt, and getting the whole thing right lands the cursor on
    /// pos 5. Returns true only on that landing, so the caller skips the ordinary cursor
    /// step for the press -- while the sequence is still building, the cursor walks as
    /// usual, so the input looks like plain navigation until it pays off.
    /// </summary>
    private bool AdvanceSecretSequence(SecretOperation op)
    {
        if (secretSequence == null || secretSequence.Operations.Count == 0)
        {
            return false;
        }

        // Nothing entered yet and standing somewhere other than the start spot: this press
        // is just navigation, not the opening of the sequence.
        if (secretProgress == 0 && spotIndex != secretSequence.StartPosIndex)
        {
            return false;
        }

        if (op == secretSequence.Operations[secretProgress])
        {
            secretProgress++;
            if (secretProgress >= secretSequence.Operations.Count)
            {
                secretProgress = 0;
                ReachSecretSpot();
                return true;
            }

            return false;
        }

        // Wrong key: the attempt is off. Starting over means walking back to the start spot
        // and entering it again from the first press.
        secretProgress = 0;
        return false;
    }

    /// <summary>
    /// The secret sequence has been entered in full: drop the cursor on pos 5, the spot the
    /// left/right walk never lands on.
    /// </summary>
    private void ReachSecretSpot()
    {
        if (spots == null || spots.Count <= SecretSpotIndex)
        {
            return;
        }

        spotIndex = SecretSpotIndex;
        cursor.position = spots[spotIndex];
        Debug.Log("Village secret sequence entered; cursor moved to pos 5.");
    }

    /// <summary>
    /// Pos 0 is not a shop but the way on to the next chapter's battle. Loading that field
    /// scene is left for later; for now this only notes it was reached, so the spot is
    /// walkable and selectable while the route behind it is built out.
    /// </summary>
    private void OnProceed()
    {
        Debug.Log("Village proceed (pos 0) selected; the next battlefield is not wired up yet.");
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
