using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.UI.Components;

namespace WindingTale.UI.Common
{
    public class GameObjectExtension
    {
        private static Material defaultMaterial = Resources.Load<Material>(@"common-mat");

        public static GameObject CreateFromObj(string resourcePath, Transform parent = null)
        {
            GameObject iconPrefab = Resources.Load<GameObject>(resourcePath);
            if (iconPrefab == null)
            {
                int here = 1;
            }
            
            GameObject obj = GameObject.Instantiate(iconPrefab);
            
            if (parent != null)
            {
                obj.transform.parent = parent;
            }
            
            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.transform.localRotation = new Quaternion(0, 0, 0, 0);
            
            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = defaultMaterial;

            return obj;
        }

        public static GameObject LoadCreatureIcon(int animationId, int animationIndex, Transform parent = null)
        {
            GameObject iconPrefab = AssetManager.Instance().LoadIconPrefab(animationId, animationIndex);
            if (iconPrefab == null)
            {
                return null;
            }

            GameObject obj = GameObject.Instantiate(iconPrefab);
            if (parent != null)
            {
                obj.transform.parent = parent;
            }

            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.transform.localRotation = new Quaternion(0, 0, 0, 0);

            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = defaultMaterial;

            return obj;
        }
    }
}