using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene
{

    public class GameRenderer : MonoBehaviour
    {
        public Material DefaultMaterial;

        public Material DefaultGreyMaterial;

        private static GameRenderer instance = null;


        public static GameRenderer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameRenderer>(); //// GameObject.Find("GameRoot").GetComponent<GameRenderer>();
                    if (instance == null)
                    {
                        throw new MissingComponentException("Cannot find component GameRenderer");
                    }
                }
                return instance;
            }
        }

        public void ApplyDefaultMaterial(GameObject gameObject)
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }

            renderer.materials = new Material[1] { DefaultMaterial };
        }

        private Shader _greyShader;
        private Shader GreyShader => _greyShader != null ? _greyShader : (_greyShader = Shader.Find("Custom/GreyoutShader"));

        public void ApplyDefaultGreyMaterial(GameObject gameObject)
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            Shader shader = GreyShader;
            if (shader == null)
            {
                Debug.LogError("[GameRenderer] Shader 'Custom/GreyoutShader' not found!");
                return;
            }

            Material[] originals = renderer.sharedMaterials;
            Material[] greyMats = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                if (originals[i] == null) { greyMats[i] = null; continue; }
                Material grey = new Material(shader);
                if (originals[i].HasProperty("_MainTex"))
                    grey.SetTexture("_MainTex", originals[i].GetTexture("_MainTex"));
                if (originals[i].HasProperty("_Color"))
                    grey.SetColor("_Color", originals[i].GetColor("_Color"));
                greyMats[i] = grey;
            }
            renderer.materials = greyMats;
        }

        private Shader _whiteFlashShader;
        private Shader WhiteFlashShader => _whiteFlashShader != null
            ? _whiteFlashShader
            : (_whiteFlashShader = Shader.Find("Custom/WhiteFlash"));

        /// <summary>
        /// Paints every submesh flat white. Used for the short "recovering" flash; the
        /// caller is responsible for restoring the saved materials afterwards.
        /// </summary>
        public void ApplyWhiteFlashMaterial(GameObject gameObject)
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            Shader shader = WhiteFlashShader;
            if (shader == null)
            {
                Debug.LogError("[GameRenderer] Shader 'Custom/WhiteFlash' not found!");
                return;
            }

            Material[] originals = renderer.sharedMaterials;
            Material[] whiteMats = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                whiteMats[i] = originals[i] == null ? null : new Material(shader);
            }
            renderer.materials = whiteMats;
        }
    }
}