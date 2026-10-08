using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Common;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.UI.Audio;

namespace WindingTale.Scenes.GameFieldScene
{
    /// <summary>
    /// The battlefield's info box (the Record menu's "ShowInfo"): the original game's
    /// ChapterInfo picture in the middle of the screen, the chapter's name framed just above
    /// it as in the village (VillageTitleBar), with the chapter and turn, what wins
    /// and what loses the battle, how many enemies, friends and NPCs are still standing, and
    /// the party's money filled into it. Any key, or a click, puts it away.
    ///
    /// The picture already carries every label -- MAP, TURN, 勝利條件, 失敗條件, ENEMY,
    /// FRIEND, NPC and $ -- so only the values are drawn, each into the slot its label
    /// leaves. The slots below are measured off the bitmap (171 x 168, top-left origin).
    ///
    /// Built entirely in code on its own overlay canvas. While it stands the map takes no
    /// input (MapInput); it lets go one frame after it closes, so the key that closed it
    /// does not reach the field as well.
    /// </summary>
    public class ChapterInfoDialog : MonoBehaviour
    {
        private const string BackgroundResource = "Dialogs/ChapterInfo";
        private const string InputBlockReason = "ChapterInfoDialog";

        // Above the field UI and its dialogs, below the scene fade curtain.
        private const int SortingOrder = 200;

        private const float BitmapWidth = 171f;
        private const float BitmapHeight = 168f;

        // Canvas units per bitmap pixel (the canvas is scaled from 800 x 600): the box comes
        // to about 440 units tall.
        private const float PixelScale = 2.6f;

        // Canvas units between the box's top edge and the title plate above it.
        private const float TitleGap = 4f;

        private static readonly Color TextColor = new Color(0.86f, 0.90f, 0.98f, 1f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.8f);
        private const float ShadowOffset = 2f;

        private const float NumberFontSize = 20f;
        private const float ConditionFontSize = 22f;

        private RectTransform box = null;

        // The chapter's name just above the box, in the village's title plate.
        private RectTransform titlePlate = null;
        private bool closing = false;

        // The frame the box went up on: the key press that chose "ShowInfo" is still down
        // on it, and must not close the box again straight away.
        private int openedFrame = -1;

        /// <summary>Puts the box up for the map as it stands now.</summary>
        public static ChapterInfoDialog Show(FDMap map)
        {
            GameObject canvasObject = new GameObject("ChapterInfoDialog");

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 0.5f;

            ChapterInfoDialog dialog = canvasObject.AddComponent<ChapterInfoDialog>();
            dialog.openedFrame = Time.frameCount;
            dialog.Build(map);

            MapInput.SetBlocked(InputBlockReason, true);
            return dialog;
        }

        private void Build(FDMap map)
        {
            GameObject boxObject = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxObject.transform.SetParent(transform, false);

            box = boxObject.GetComponent<RectTransform>();
            box.anchorMin = new Vector2(0.5f, 0.5f);
            box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = new Vector2(BitmapWidth * PixelScale, BitmapHeight * PixelScale);

            Image background = boxObject.GetComponent<Image>();
            background.sprite = LoadBackground();
            background.raycastTarget = false;

            TMP_FontAsset font = ShoppingRecordDialog.GetRecordFont();
            int chapterId = map.ChapterId;

            // "第13关 ...", framed as in the village (VillageTitleBar), sitting just on top
            // of the box.
            titlePlate = VillageTitleBar.AddTitlePlate(transform,
                LocalizationManager.GetChapterTitleString(chapterId).GetLocalizedString());
            titlePlate.anchorMin = new Vector2(0.5f, 0.5f);
            titlePlate.anchorMax = new Vector2(0.5f, 0.5f);
            titlePlate.pivot = new Vector2(0.5f, 0f);
            titlePlate.anchoredPosition = new Vector2(0f, BitmapHeight * PixelScale / 2f + TitleGap);

            // MAP . nn   TURN . nn
            AddText(font, map.ChapterId.ToString(), 67, 3, 16, 12, NumberFontSize, TextAlignmentOptions.Left);
            AddText(font, map.TurnNo.ToString(), 112, 3, 22, 12, NumberFontSize, TextAlignmentOptions.Left);

            // Under 勝利條件 and 失敗條件.
            AddText(font, LocalizationManager.GetChapterWinConditionString(chapterId).GetLocalizedString(),
                6, 42, 159, 34, ConditionFontSize, TextAlignmentOptions.Center);
            AddText(font, LocalizationManager.GetChapterLoseConditionString(chapterId).GetLocalizedString(),
                6, 96, 159, 34, ConditionFontSize, TextAlignmentOptions.Center);

            // ENEMY . n   FRIEND . n   NPC . n  -- the ones still on the field.
            AddText(font, map.Enemies.Count.ToString(), 43, 139, 20, 11, NumberFontSize, TextAlignmentOptions.Left);
            AddText(font, map.Friends.Count.ToString(), 105, 139, 20, 11, NumberFontSize, TextAlignmentOptions.Left);
            AddText(font, map.Npcs.Count.ToString(), 152, 139, 15, 11, NumberFontSize, TextAlignmentOptions.Left);

            // $ nnnn
            AddText(font, map.TotalMoney.ToString(), 66, 156, 48, 10, NumberFontSize, TextAlignmentOptions.Left);
        }

        /// <summary>
        /// Writes a value into a slot of the picture, given in bitmap pixels from its top-left
        /// corner, with a dark copy under it for a shadow.
        /// </summary>
        private void AddText(TMP_FontAsset font, string value, float x, float y, float width, float height,
            float fontSize, TextAlignmentOptions alignment)
        {
            CreateLabel("Shadow", font, value, x, y, width, height, fontSize, alignment, ShadowColor, ShadowOffset);
            CreateLabel("Value", font, value, x, y, width, height, fontSize, alignment, TextColor, 0f);
        }

        private void CreateLabel(string objectName, TMP_FontAsset font, string value, float x, float y, float width, float height,
            float fontSize, TextAlignmentOptions alignment, Color color, float offset)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(box, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x * PixelScale + offset, -y * PixelScale - offset);
            rect.sizeDelta = new Vector2(width * PixelScale, height * PixelScale);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = color;
            text.raycastTarget = false;
        }

        void Update()
        {
            if (closing)
            {
                // The key that closed the box went down last frame; the map can listen again.
                MapInput.SetBlocked(InputBlockReason, false);
                Destroy(gameObject);
                return;
            }

            if (Input.anyKeyDown && Time.frameCount != openedFrame)
            {
                SoundEffects.Play(SoundEffect.DialogConfirm);

                // Hidden at once, released next frame: PlayerInterface reads the same key
                // press this frame, and must still find the map blocked.
                closing = true;
                if (box != null)
                {
                    box.gameObject.SetActive(false);
                }
                if (titlePlate != null)
                {
                    titlePlate.gameObject.SetActive(false);
                }
            }
        }

        void OnDestroy()
        {
            MapInput.SetBlocked(InputBlockReason, false);
        }

        /// <summary>
        /// The picture ships imported as a plain texture rather than a sprite, so it is
        /// wrapped in one here if need be.
        /// </summary>
        private static Sprite LoadBackground()
        {
            Sprite sprite = Resources.Load<Sprite>(BackgroundResource);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(BackgroundResource);
            if (texture == null)
            {
                Debug.LogWarning("ChapterInfoDialog: background not found: Resources/" + BackgroundResource);
                return null;
            }

            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
