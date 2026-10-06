using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.FightObjects;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.GameBattleScene
{

    public class BattleLoader : MonoBehaviour
    {
        public static float ANIMATION_INTERVAL = 0.5f;
        public static float ANIMATION_END = 1.8f;

        public GameObject localBody;
        public GameObject foreignBody;

        public GameObject localTai;

        public GameObject localHpBar;
        public GameObject localMpBar;
        public GameObject foreignHpBar;
        public GameObject foreignMpBar;

        /// <summary>
        /// The backdrop authored in the scene. It is both the shot designers frame the
        /// camera against in the editor and the fallback for chapters that have no
        /// backdrop of their own, so it stays in the scene rather than being loaded.
        /// </summary>
        public GameObject backdrop;

        /// <summary>
        /// The GlobalVariables key the field scene names the running chapter on, so the
        /// battle can pick that chapter's backdrop. GameMain sets it when it loads a
        /// chapter; nothing there means the authored backdrop is used.
        /// </summary>
        public const string ChapterIdVariableName = "BattleChapterId";


        /// <summary>
        /// Swaps in the running chapter's own backdrop, if one was built for it
        /// (Resources/BG/NN/BG_NN, from Tools/Vox_Generator/chapter_bg_to_vox.py).
        ///
        /// Every backdrop model is exported to the same envelope and with the same
        /// settings, so the authored object's transform is the placement for all of them
        /// and the swap is just "same spot, different model". Chapters without one keep
        /// the authored backdrop.
        ///
        /// The folder number is the chapter: BG/01 is chapter 1's island, BG/02 the
        /// church town, and so on.
        /// </summary>
        private void LoadBackdrop()
        {
            int chapterId = GlobalVariables.Get<int>(ChapterIdVariableName);
            if (chapterId <= 0 || backdrop == null) return;

            string backdropPath = string.Format("BG/{0:D2}/BG_{0:D2}", chapterId);
            GameObject backdropPrefab = Resources.Load<GameObject>(backdropPath);
            if (backdropPrefab == null) return;

            // The scene is authored with chapter 1's backdrop in place, so for that
            // chapter there is nothing to swap: loading it would only build a second
            // copy of a model the scene is already holding.
            if (backdropPrefab.name == backdrop.name) return;

            GameObject instance = Instantiate(backdropPrefab);
            // The battle scene is loaded additively, so the active scene is still the
            // field one: without this the backdrop would be built into that scene and
            // outlive the battle it belongs to.
            SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
            instance.name = backdropPrefab.name;
            instance.transform.SetPositionAndRotation(backdrop.transform.position,
                                                      backdrop.transform.rotation);
            instance.transform.localScale = backdrop.transform.localScale;

            backdrop.SetActive(false);

            ShiftBackdrop(instance, chapterId);
        }

        /// <summary>
        /// Per-chapter sideways nudge of the backdrop, as a fraction of the screen width
        /// (negative = to the left). Chapter 18: the whole scene sits 15% further left.
        /// </summary>
        private static readonly Dictionary<int, float> BackdropScreenShift = new Dictionary<int, float>
        {
            { 18, -0.15f },
        };

        /// <summary>
        /// Moves the backdrop along the camera's right axis by its chapter's share of the
        /// screen width. The backdrop has depth, so the shift is measured at the depth of
        /// its middle: nearer parts move a little more on screen, farther ones a little less.
        /// </summary>
        private void ShiftBackdrop(GameObject instance, int chapterId)
        {
            if (!BackdropScreenShift.TryGetValue(chapterId, out float fraction)) return;

            Camera camera = MagicEffect.FindSceneCamera(gameObject.scene);
            if (camera == null) return;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            float depth = Vector3.Dot(bounds.center - camera.transform.position, camera.transform.forward);
            float screenWidth = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * camera.aspect;
            instance.transform.position += camera.transform.right * (fraction * screenWidth);
        }

        private void LoadLocalTai()
        {
            int taiId = GlobalVariables.Get<int>("LocalTaiId");
            if (taiId <= 0 || localTai == null) return;

            string taiPath = string.Format("Tais/{0:D2}/Tai_{0:D2}", taiId);
            GameObject taiPrefab = Resources.Load<GameObject>(taiPath);
            if (taiPrefab == null) return;

            GameObject tai = Instantiate(taiPrefab, localTai.transform);

            // At the authored spot the Tai's top edge covers our fighter's feet. Drop it by
            // its own height. LocalTai is tipped towards the camera, so a world-space bound
            // would measure the slab's slanted depth too -- take the mesh's own thickness
            // and move along the Tai's own up axis instead.
            float height = 0f;
            foreach (MeshFilter filter in tai.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null)
                {
                    height = Mathf.Max(height, filter.sharedMesh.bounds.size.y * filter.transform.localScale.y);
                }
            }
            tai.transform.localPosition -= Vector3.up * height;

            ShiftLocalTai(tai);
        }

        /// <summary>
        /// Chapters whose Tai sits a twelfth of its width to the left and a twelfth of its
        /// width further into the scene, away from the camera.
        /// </summary>
        private static readonly HashSet<int> TaiNudgeBackLeftChapters = new HashSet<int> { 19 };

        private void ShiftLocalTai(GameObject tai)
        {
            int chapterId = GlobalVariables.Get<int>(ChapterIdVariableName);
            if (!TaiNudgeBackLeftChapters.Contains(chapterId)) return;

            Camera camera = MagicEffect.FindSceneCamera(gameObject.scene);
            if (camera == null) return;

            // The slab's width along its own x axis, in world units.
            float width = 0f;
            foreach (MeshFilter filter in tai.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null)
                {
                    width = Mathf.Max(width, filter.transform.TransformVector(Vector3.right * filter.sharedMesh.bounds.size.x).magnitude);
                }
            }

            // Left and "in" are taken level with the ground, so the Tai doesn't rise or sink.
            Vector3 left = Vector3.ProjectOnPlane(-camera.transform.right, Vector3.up).normalized;
            Vector3 inward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            tai.transform.position += (left + inward) * (width / 12f);
        }

        // Start is called before the first frame update
        void Start()
        {
            LoadBackdrop();
            LoadLocalTai();

            // Each result is consumed as it is read, so this battle runs the one result it
            // was loaded for and nothing else. Leaving them set made every later battle
            // re-run the previous magic on top of itself -- the stale MagicRunner won the
            // animators, so every fight kept showing the creature that last cast a spell.
            AttackResult attackResult = GlobalVariables.Take<AttackResult>("AttackResult");
            if (attackResult != null )
            {
                this.AddComponent<AttackRunner>().Initialize(this, attackResult);
            }
            // Load the Magic Result if it exists
            MagicResult magicResult = GlobalVariables.Take<MagicResult>("MagicResult");
            if (magicResult != null )
            {
                this.AddComponent<MagicRunner>().Initialize(this, magicResult);
            }
        }

    }

}