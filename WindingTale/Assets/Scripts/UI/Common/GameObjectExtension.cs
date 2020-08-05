using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Common
{
    public class GameObjectExtension
    {
        private static Material defaultMaterial = Resources.Load<Material>(@"common-mat");

        public static GameObject CreateFromObj(string resourcePath, Transform parent = null)
        {
            GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>(resourcePath));
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