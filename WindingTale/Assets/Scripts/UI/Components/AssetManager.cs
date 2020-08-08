using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.UI.Components
{

    public class AssetManager
    {
        private static AssetManager instance = null;

        private static object mutex = new object();

        private AssetBundle iconAssetBundle = null;

        private Dictionary<int, AssetBundle> shapeAssetBundles = null;


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
        }

        private AssetBundle loadBundle(string bundleName)
        {
            return AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="animationId"></param>
        /// <param name="animationIndex"></param>
        /// <returns></returns>
        public GameObject LoadIconPrefab(int animationId, int animationIndex)
        {
            if (iconAssetBundle == null)
            {
                lock(mutex)
                {
                    iconAssetBundle = loadBundle("iconbundle");
                }
            }

            if (iconAssetBundle != null)
            {
                string iconName = string.Format(@"Icon_{0}_{1}", StringUtils.Digit3(animationId), StringUtils.Digit2(animationIndex));
                GameObject iconPrefab = iconAssetBundle.LoadAsset<GameObject>(iconName);
                return iconPrefab;
            }

            return null;
        }

        public GameObject LoadShapePrefab(int shapePanelIndex, int shapeIndex)
        {
            if (!shapeAssetBundles.ContainsKey(shapePanelIndex))
            {
                shapeAssetBundles[shapePanelIndex] = loadBundle("shapepanel" + StringUtils.Digit2(shapePanelIndex));
            }

            if (shapeAssetBundles[shapePanelIndex] != null)
            {
                string shapeName = string.Format(@"Shape_{0}_{1}", shapePanelIndex, shapeIndex);
                GameObject shapePrefab = shapeAssetBundles[shapePanelIndex].LoadAsset<GameObject>(shapeName);
                return shapePrefab;
            }

            return null;
        }
    }
}