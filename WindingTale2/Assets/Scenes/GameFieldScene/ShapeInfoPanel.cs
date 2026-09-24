using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;

namespace WindingTale.Scenes.GameFieldScene
{
    /// <summary>
    /// The terrain readout in a bottom corner of the field screen (the one away from the cursor): a picture of
    /// the tile under the map cursor on the left (see TilePreview), and what standing
    /// on it does on the right -- "A+05" / "D+00", the percentage the tile adds to
    /// (or takes off) the attack and defense of whoever stands there
    /// (ShapeDefinition.AdjustedAp / AdjustedDp, the same numbers BattleHandler applies).
    ///
    /// Built entirely in code under the field canvas, so no scene edit is needed; add
    /// the component to the canvas object (GameCanvas does). It hides while a dialog
    /// is up -- the talk dialogs sit over this corner -- and while the cursor is
    /// hidden behind a menu.
    /// </summary>
    public class ShapeInfoPanel : MonoBehaviour
    {
        private const string FrameResource = "Dialogs/ShapeInfoBase";

        // Layout in canvas units (the canvas is scaled from 800 x 600). The frame
        // bitmap is 62 x 26 with a 3 px border; this keeps its proportions loosely, the
        // way the original screen stretched it, and lays the picture and text out
        // inside the blue area.
        private const float PanelWidth = 224f;
        private const float PanelHeight = 100f;
        private const float Margin = 8f;

        private const float PictureLeft = 13f;
        private const float PictureBottom = 12f;
        private const float PictureSize = 76f;

        private const float TextLeft = 100f;
        private const float TextWidth = 110f;
        private const float TextLineHeight = 38f;
        private const float TextBottom = 12f;
        private const float TextFontSize = 32f;

        // How often a moving picture (an idling creature, a flickering flame) is
        // taken again, in seconds -- the rate the creatures' idle frames turn at.
        private const float AnimatedRefreshInterval = 0.25f;

        private static readonly Color TextColor = new Color(0.86f, 0.90f, 0.98f, 1f);

        private RectTransform root = null;
        private RawImage picture = null;
        private TextMeshProUGUI apText = null;
        private TextMeshProUGUI dpText = null;

        private bool panelOnRight = false;

        private TilePreview preview = null;
        private GameCanvas gameCanvas = null;
        private GameMain gameMain = null;

        // What the picture currently shows, so it is only retaken when that changes.
        private bool hasPicture = false;
        private int shownKey = 0;
        private bool shownAnimated = false;
        private float nextAnimatedRefresh = 0f;

        void Awake()
        {
            gameCanvas = GetComponent<GameCanvas>();
            preview = gameObject.AddComponent<TilePreview>();
            Build();
        }

        private void Build()
        {
            GameObject rootObject = new GameObject("ShapeInfoPanel", typeof(RectTransform), typeof(Image));
            rootObject.transform.SetParent(this.transform, false);

            // First sibling: everything else on the canvas (dialogs, menus) draws over it.
            rootObject.transform.SetAsFirstSibling();

            root = rootObject.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            SetSide(false);

            Image frame = rootObject.GetComponent<Image>();
            frame.sprite = LoadFrame();
            frame.raycastTarget = false;

            GameObject pictureObject = new GameObject("Picture", typeof(RectTransform), typeof(RawImage));
            pictureObject.transform.SetParent(root, false);
            SetRect(pictureObject.GetComponent<RectTransform>(), PictureLeft, PictureBottom, PictureSize, PictureSize);

            picture = pictureObject.GetComponent<RawImage>();
            picture.texture = preview.Picture;
            picture.raycastTarget = false;
            picture.enabled = false;

            TMP_FontAsset font = ShoppingRecordDialog.GetRecordFont();
            apText = CreateText("Ap", font, TextBottom + TextLineHeight);
            dpText = CreateText("Dp", font, TextBottom);

            root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Puts the panel in the bottom-left corner, or the bottom-right one when
        /// <paramref name="right"/>. The contents are laid out from the panel's own
        /// bottom-left, so only the panel moves.
        /// </summary>
        private void SetSide(bool right)
        {
            Vector2 corner = new Vector2(right ? 1f : 0f, 0f);
            root.anchorMin = corner;
            root.anchorMax = corner;
            root.pivot = corner;
            root.anchoredPosition = new Vector2(right ? -Margin : Margin, Margin);
        }

        private TextMeshProUGUI CreateText(string objectName, TMP_FontAsset font, float bottom)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root, false);
            SetRect(textObject.GetComponent<RectTransform>(), TextLeft, bottom, TextWidth, TextLineHeight);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = TextFontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = TextColor;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, float left, float bottom, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// The frame bitmap (the original game's tile info box). Imported as a sprite;
        /// if the importer left it a plain texture, wrapped in one here.
        /// </summary>
        private static Sprite LoadFrame()
        {
            Sprite sprite = Resources.Load<Sprite>(FrameResource);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(FrameResource);
            if (texture == null)
            {
                Debug.LogWarning("ShapeInfoPanel: frame image not found: Resources/" + FrameResource);
                return null;
            }

            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        void LateUpdate()
        {
            // The panel belongs to the map screen: it goes with the map's input when a battle
            // animation (or anything else) takes the map over.
            GameMap map = GetGameMap();
            if (MapInput.IsBlocked || map == null || map.Map == null || map.Map.Field == null || !map.IsCursorVisible || IsDialogOpen())
            {
                SetShown(false);
                return;
            }

            FDPosition position = map.GetCursorPosition();
            if (position == null)
            {
                SetShown(false);
                return;
            }

            SetShown(true);

            // The corner away from the cursor: bottom-left while it is on the right half of
            // the screen, bottom-right while it is on the left half.
            bool onRight = !map.IsTileOnRightHalfOfScreen(position);
            if (onRight != panelOnRight)
            {
                SetSide(onRight);
                panelOnRight = onRight;
            }

            ShapeDefinition shape = map.Map.Field.GetShapeAt(position);
            apText.text = FormatModifier('A', shape != null ? shape.AdjustedAp : 0);
            dpText.text = FormatModifier('D', shape != null ? shape.AdjustedDp : 0);

            FDCreature creature = FindCreatureAt(map, position);
            TreasureInstance chest = null;
            if (creature == null)
            {
                ObjectsLayer objects = map.Objects;
                chest = objects != null ? objects.GetTreasureAt(position) : null;
            }

            int key = PictureKey(position, creature, chest);
            bool changed = !hasPicture || key != shownKey;
            bool animatedDue = shownAnimated && Time.unscaledTime >= nextAnimatedRefresh;
            if (!changed && !animatedDue)
            {
                return;
            }

            shownAnimated = preview.Render(map, position, creature);
            nextAnimatedRefresh = Time.unscaledTime + AnimatedRefreshInterval;
            shownKey = key;
            hasPicture = true;
            picture.enabled = true;
        }

        private GameMap GetGameMap()
        {
            if (gameMain == null)
            {
                gameMain = FindFirstObjectByType<GameMain>();
            }

            return gameMain != null && gameMain.mapObject != null ? gameMain.gameMap : null;
        }

        private bool IsDialogOpen()
        {
            return gameCanvas != null && gameCanvas.dialog != null && gameCanvas.dialog.activeSelf;
        }

        private void SetShown(bool shown)
        {
            if (root.gameObject.activeSelf != shown)
            {
                root.gameObject.SetActive(shown);
            }
        }

        private static FDCreature FindCreatureAt(GameMap map, FDPosition position)
        {
            if (map.Map.Creatures == null)
            {
                return null;
            }

            foreach (FDCreature creature in map.Map.Creatures)
            {
                if (creature != null && creature.Position != null && creature.Position.AreSame(position))
                {
                    return creature;
                }
            }

            return null;
        }

        /// <summary>
        /// Everything about the tile that changes what its picture looks like, folded
        /// into one number: which tile, who is on it (and whether they have acted, which
        /// greys them out), and whether a chest on it has been opened.
        /// </summary>
        private static int PictureKey(FDPosition position, FDCreature creature, TreasureInstance chest)
        {
            int creatureState = creature == null ? -1 : (creature.Id * 2 + (creature.HasActioned ? 1 : 0));
            int chestState = chest == null ? -1 : (chest.ShowingOpened ? 1 : 0);
            return System.HashCode.Combine(position.X, position.Y, creatureState, chestState);
        }

        /// <summary>
        /// "A+05", "D-05", "A+00": the letter, the sign and the size in two digits.
        /// </summary>
        public static string FormatModifier(char label, int percent)
        {
            char sign = percent < 0 ? '-' : '+';
            int size = Mathf.Min(99, Mathf.Abs(percent));
            return string.Format("{0}{1}{2:D2}", label, sign, size);
        }

        void OnDestroy()
        {
            if (root != null)
            {
                Destroy(root.gameObject);
            }
        }
    }
}
