using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.Common;
using static WindingTale.UI.Common.Constants;

namespace WindingTale.UI.Components
{

    public class AssetManager
    {
        private static AssetManager instance = null;

        private static object mutex = new object();

        private Dictionary<string, AssetBundle> assetBundles = null;

        private AssetBundle iconAssetBundle = null;

        private Dictionary<int, AssetBundle> shapeAssetBundles = null;

        private static Material defaultMaterial = Resources.Load<Material>(@"common-mat");
        private static Material transparentMaterial = Resources.Load<Material>(@"common-transparent");


        public static AssetManager Instance()
        {
            if (instance == null)
            {
                lock(mutex)
                {
                    instance = new AssetManager();
                }
            }
            return instance;
        }

        private AssetManager()
        {
            shapeAssetBundles = new Dictionary<int, AssetBundle>();
            assetBundles = new Dictionary<string, AssetBundle>();
        }

        private AssetBundle LoadBundle(string bundleName)
        {
            if (!assetBundles.ContainsKey(bundleName))
            {
                assetBundles[bundleName] = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));
            }

            return assetBundles[bundleName];
        }

        private GameObject LoadPrefabResource(string resourcePath)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            return prefab;
        }

        private GameObject LoadPrefabResource(string bundleName, string resourcePath)
        {
            AssetBundle bundle = LoadBundle(bundleName);
            if (bundle != null)
            {
                GameObject prefab = bundle.LoadAsset<GameObject>(resourcePath);
                return prefab;
            }

            return null;
        }

        public GameObject InstantiateIconGO(Transform parent, int animationId, int animationIndex)
        {
            string iconName = string.Format(@"Icon_{0}_{1}", StringUtils.Digit3(animationId), StringUtils.Digit2(animationIndex));
            GameObject prefab = LoadPrefabResource("iconbundle", iconName);

            GameObject obj = GameObject.Instantiate(prefab);
            if (parent != null)
            {
                obj.transform.parent = parent;
            }

            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.transform.localRotation = new Quaternion(0, 0, 0, 0);

            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = defaultMaterial;
            var clr = renderer.sharedMaterial.color;
            renderer.sharedMaterial.color = new Color(clr.r, clr.g, clr.b, 1.0f);

            return obj;
        }

        public GameObject InstantiateCursorGO(Transform parent, CursorType type)
        {
            GameObject prefab = LoadPrefabResource("Others/Cursor");
            GameObject obj = GameObject.Instantiate(prefab);
            if (parent != null)
            {
                obj.transform.parent = parent;
            }

            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.transform.localRotation = new Quaternion(0, 0, 0, 0);

            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = defaultMaterial;
            var clr = renderer.sharedMaterial.color;
            renderer.sharedMaterial.color = new Color(clr.r, clr.g, clr.b, 1.0f);

            return obj;
        }

        public GameObject InstantiateShapeGO(Transform parent, int shapePanelIndex, int shapeIndex)
        {
            string bundleName = "shapepanel" + StringUtils.Digit2(shapePanelIndex);
            string shapeName = string.Format(@"Shape_{0}_{1}", shapePanelIndex, shapeIndex);
            GameObject prefab = LoadPrefabResource(bundleName, shapeName);
            GameObject obj = GameObject.Instantiate(prefab);

            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = defaultMaterial;

            obj.transform.parent = parent;
            obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            obj.transform.localRotation = new Quaternion(0f, 1.57f, 1.57f, 0f);

            return obj;
        }

        public GameObject InstantiateMenuItemGO(Transform parent, MenuItemId menuItem, int index)
        {
            string menuName = string.Format(@"Menu/Menu_{0}_{1}", menuItem.GetHashCode(), index);
            GameObject prefab = LoadPrefabResource(menuName);
            if (prefab == null)
            {
                return null;
            }

            GameObject obj = GameObject.Instantiate(prefab);
            obj.transform.parent = parent;

            obj.transform.localPosition = new Vector3(-1.2f, 0, -0.8f);
            obj.transform.localRotation = Quaternion.Euler(270, 90, 90);
            obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = defaultMaterial;
            var clr = renderer.sharedMaterial.color;
            renderer.sharedMaterial.color = new Color(clr.r, clr.g, clr.b, 1.0f);

            return obj;
        }

        public GameObject InstantiateIndicatorGO(Transform parent)
        {
            GameObject prefab = LoadPrefabResource(@"Others/BlockIndicator");
            if (prefab == null)
            {
                return null;
            }

            GameObject obj = GameObject.Instantiate(prefab);
            obj.transform.parent = parent;
            obj.transform.localPosition = new Vector3(0f, 0, 0f);
            //// obj.transform.localRotation = Quaternion.Euler(270, 90, 90);
            //// obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = transparentMaterial;
            var clr = renderer.sharedMaterial.color;
            renderer.sharedMaterial.color = new Color(clr.r, clr.g, clr.b, 1.0f);

            return obj;
        }
    }
}