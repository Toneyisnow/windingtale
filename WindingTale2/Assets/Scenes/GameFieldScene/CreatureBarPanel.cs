using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameBattleScene;

namespace WindingTale.Scenes.GameFieldScene
{
    /// <summary>
    /// The creature status box in a top corner of the field screen: name, level,
    /// and the HP / MP bars, shown while a friend's move range is up
    /// (ShowMoveRangeState) and gone as soon as the cursor moves off, a click lands
    /// anywhere else, or the state ends.
    ///
    /// It is not a look-alike of the battle bar but the battle bar itself: the prefab
    /// Resources/Others/CreatureBar (frame, HP / MP fills and labels, driven by
    /// BattleBarInfo) is put on a stage far from the board and photographed straight on
    /// into a texture, which a RawImage shows in the corner. That is the view the battle
    /// camera has of its bars, so what shows here is what shows there.
    ///
    /// Built in code under the field canvas, like ShapeInfoPanel; GameCanvas adds it the
    /// first time a bar is asked for.
    /// </summary>
    public class CreatureBarPanel : MonoBehaviour
    {
        private const string PrefabResource = "Others/CreatureBar";

        // Layout in canvas units (the canvas is scaled from 800 x 600); the height
        // follows from the aspect of the frame model.
        private const float PanelWidth = 360f;
        private const float Margin = 8f;

        // Width of the picture in pixels: about two per canvas unit at 800 x 600.
        private const int TextureWidth = 768;

        // Where the bar is put on stage: nowhere near the board, and far above it, so
        // neither the game camera nor its light shadows ever reach it.
        private static readonly Vector3 StageOrigin = new Vector3(0f, 3000f, 0f);

        // Distance from the camera to the bar, and the clip planes around it.
        private const float CameraDistance = 20f;
        private const float FarClip = 40f;

        // The battle camera faces the bar's +Y side with the bar's -Z edge up. The bar
        // is not mirrored, so this is also how it reads on screen: name at the left,
        // digits at the right.
        private static readonly Quaternion CameraRotation = Quaternion.LookRotation(Vector3.down, Vector3.back);

        private RectTransform root = null;
        private RawImage picture = null;

        private RenderTexture texture = null;
        private Camera previewCamera = null;
        private GameObject cameraObject = null;
        private GameObject barObject = null;
        private BattleBarInfo bar = null;

        private GameMain gameMain = null;

        // Where the cursor was when the box came up; it goes away when the cursor leaves.
        private bool shown = false;
        private FDPosition shownCursor = null;

        void Awake()
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabResource);
            if (prefab == null)
            {
                Debug.LogWarning("CreatureBarPanel: prefab not found: Resources/" + PrefabResource);
                return;
            }

            barObject = Instantiate(prefab, StageOrigin, Quaternion.identity);
            barObject.name = "CreatureBarStage";
            bar = barObject.GetComponent<BattleBarInfo>();

            Bounds frame = GetFrameBounds(barObject);
            float aspect = frame.size.x / Mathf.Max(0.01f, frame.size.z);
            int textureHeight = Mathf.Max(1, Mathf.RoundToInt(TextureWidth / aspect));

            texture = new RenderTexture(TextureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
            texture.name = "CreatureBarPanel";
            texture.filterMode = FilterMode.Bilinear;
            texture.Create();

            cameraObject = new GameObject("CreatureBarCamera");
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = frame.size.z * 0.5f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = FarClip;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.useOcclusionCulling = false;
            previewCamera.targetTexture = texture;
            previewCamera.transform.SetPositionAndRotation(
                frame.center + Vector3.up * CameraDistance, CameraRotation);

            Build(PanelWidth, PanelWidth / aspect);
        }

        /// <summary>
        /// The box the frame and fills fill, in world space on the stage. The labels are
        /// left out (they are text meshes, sized by their font) and so is nothing else:
        /// the frame is the outermost thing in the bar.
        /// </summary>
        private static Bounds GetFrameBounds(GameObject barObject)
        {
            Bounds bounds = new Bounds(barObject.transform.position, Vector3.zero);
            bool any = false;

            foreach (MeshRenderer renderer in barObject.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.GetComponent<TMP_Text>() != null)
                {
                    continue;
                }

                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private void Build(float width, float height)
        {
            GameObject rootObject = new GameObject("CreatureBarPanel", typeof(RectTransform), typeof(RawImage));
            rootObject.transform.SetParent(this.transform, false);

            // First sibling: everything else on the canvas (dialogs, menus) draws over it.
            rootObject.transform.SetAsFirstSibling();

            root = rootObject.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(width, height);
            SetSide(true);

            picture = rootObject.GetComponent<RawImage>();
            picture.texture = texture;
            picture.raycastTarget = false;

            root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Puts the box in the top-right corner, or the top-left one when
        /// <paramref name="right"/> is false.
        /// </summary>
        private void SetSide(bool right)
        {
            Vector2 corner = new Vector2(right ? 1f : 0f, 1f);
            root.anchorMin = corner;
            root.anchorMax = corner;
            root.pivot = corner;
            root.anchoredPosition = new Vector2(right ? -Margin : Margin, -Margin);
        }

        /// <summary>
        /// Shows the box for <paramref name="creature"/>. It stays until <see cref="Hide"/>
        /// or until the map cursor leaves the tile it was on when this was called.
        /// </summary>
        public void Show(FDCreature creature)
        {
            if (creature == null || bar == null || previewCamera == null)
            {
                return;
            }

            GameMap map = GetGameMap();
            shownCursor = map != null ? map.GetCursorPosition() : null;

            // The corner away from the cursor when it was clicked: top-right while it is on
            // the left half of the screen, top-left while it is on the right half.
            SetSide(map == null || !map.IsTileOnRightHalfOfScreen(shownCursor));

            bar.Bind(creature);

            // The text meshes are rebuilt lazily, after this frame's camera pass would
            // have run; the photograph is taken right now, so rebuild them first.
            foreach (TMP_Text text in barObject.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
            }

            previewCamera.Render();

            shown = true;
            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            shown = false;
            shownCursor = null;

            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (!shown)
            {
                return;
            }

            GameMap map = GetGameMap();
            if (map == null || map.Map == null || map.Map.Field == null)
            {
                Hide();
                return;
            }

            FDPosition cursor = map.GetCursorPosition();
            if (shownCursor == null || cursor == null || !cursor.AreSame(shownCursor))
            {
                Hide();
            }
        }

        private GameMap GetGameMap()
        {
            if (gameMain == null)
            {
                gameMain = FindFirstObjectByType<GameMain>();
            }

            return gameMain != null && gameMain.mapObject != null ? gameMain.gameMap : null;
        }

        void OnDestroy()
        {
            if (root != null)
            {
                Destroy(root.gameObject);
            }

            if (barObject != null)
            {
                Destroy(barObject);
            }

            if (cameraObject != null)
            {
                Destroy(cameraObject);
            }

            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
                texture = null;
            }
        }
    }
}
