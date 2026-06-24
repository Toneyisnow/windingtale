using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using WindingTale.Core.Objects;
using WindingTale.Core.Definitions;
using WindingTale.Core.Algorithms;
using WindingTale.Scenes.GameFieldScene;
using UnityEngine.EventSystems;

namespace WindingTale.MapObjects.CreatureIcon
{
    public class Creature : MonoBehaviour, IPointerClickHandler
    {
        private bool isInitialized = false;

        private int moveCount = 0;
        private bool isMoving = false;

        private FDMovePath path = null;

        private Material[] originalMaterials1 = null;
        private Material[] originalMaterials2 = null;
        private Material[] originalMaterials3 = null;

        public FDCreature creature
        {
            get; private set;
        }

        public void SetCreature(FDCreature creature)
        {
            this.creature = creature;
            isInitialized = true;
        }

        /// <summary>
        /// Reset to initial state when a new turn starts
        /// </summary>
        public void ResetTurnState()
        {
            this.SetActioned(false);
            this.creature.PrePosition = null;
        }

        public void SetGreyout(bool greyout)
        {
            GameObject obj1 = gameObject.transform.Find("Clip_01").GetChild(0).Find("default").gameObject;
            GameObject obj2 = gameObject.transform.Find("Clip_02").GetChild(0).Find("default").gameObject;
            GameObject obj3 = gameObject.transform.Find("Clip_03").GetChild(0).Find("default").gameObject;

            if (greyout)
            {
                MeshRenderer r1 = obj1.GetComponent<MeshRenderer>();
                MeshRenderer r2 = obj2.GetComponent<MeshRenderer>();
                MeshRenderer r3 = obj3.GetComponent<MeshRenderer>();
                // Save shared material references before greying out; guard against double-call overwriting originals
                if (originalMaterials1 == null && r1 != null) originalMaterials1 = r1.sharedMaterials;
                if (originalMaterials2 == null && r2 != null) originalMaterials2 = r2.sharedMaterials;
                if (originalMaterials3 == null && r3 != null) originalMaterials3 = r3.sharedMaterials;
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj1);
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj2);
                GameRenderer.Instance.ApplyDefaultGreyMaterial(obj3);
            }
            else
            {
                if (originalMaterials1 != null) { obj1.GetComponent<MeshRenderer>().sharedMaterials = originalMaterials1; originalMaterials1 = null; }
                if (originalMaterials2 != null) { obj2.GetComponent<MeshRenderer>().sharedMaterials = originalMaterials2; originalMaterials2 = null; }
                if (originalMaterials3 != null) { obj3.GetComponent<MeshRenderer>().sharedMaterials = originalMaterials3; originalMaterials3 = null; }
            }
        }

        private static readonly string[] ClipNames = { "Clip_01", "Clip_02", "Clip_03" };

        /// <summary>
        /// Fades the whole creature to the given alpha (e.g. while a menu covers its
        /// tile, so the menu reads clearly). Call ResetTransparency() to restore.
        /// </summary>
        public void SetTransparency(float alpha)
        {
            foreach (string clip in ClipNames)
            {
                MeshRenderer renderer = GetClipRenderer(clip);
                if (renderer == null)
                {
                    continue;
                }

                Material[] mats = renderer.materials; // per-instance copies
                foreach (Material m in mats)
                {
                    SetMaterialFade(m);
                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
                renderer.materials = mats;
            }
        }

        /// <summary>
        /// Restores the creature to full opacity after SetTransparency().
        /// </summary>
        public void ResetTransparency()
        {
            foreach (string clip in ClipNames)
            {
                MeshRenderer renderer = GetClipRenderer(clip);
                if (renderer == null)
                {
                    continue;
                }

                Material[] mats = renderer.materials;
                foreach (Material m in mats)
                {
                    Color c = m.color;
                    c.a = 1f;
                    m.color = c;
                    SetMaterialOpaque(m);
                }
                renderer.materials = mats;
            }
        }

        private MeshRenderer GetClipRenderer(string clipName)
        {
            Transform clip = transform.Find(clipName);
            if (clip == null || clip.childCount == 0)
            {
                return null;
            }
            Transform def = clip.GetChild(0).Find("default");
            return def != null ? def.GetComponent<MeshRenderer>() : null;
        }

        private static void SetMaterialFade(Material m)
        {
            m.SetFloat("_Mode", 2f); // Fade
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetMaterialOpaque(Material m)
        {
            m.SetFloat("_Mode", 0f); // Opaque
            m.SetInt("_SrcBlend", (int)BlendMode.One);
            m.SetInt("_DstBlend", (int)BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = -1; // use the shader's default queue (Geometry)
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            //if (isMoving)
            //{
            //    moveCount++;

            //    if (moveCount > 90)
            //    {
            //        isMoving = false;
            //        moveCount = 0;

            //        // update position
            //        creature.Position = path.Desitination;

            //        // remove component
            //        Destroy(gameObject.GetComponent<CreatureWalk>());
            //    }
            //}

        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("OnPointerClick " + this.creature.Position.ToString());

            PlayerInterface.getDefault().onSelectedPosition(this.creature.Position);
        }

        public void SetActioned(bool actioned)
        {
            creature.HasActioned = actioned;
            if (actioned)
            {
                SetGreyout(true);
            }
            else
            {
                SetGreyout(false);
            }
        }
    }
}
