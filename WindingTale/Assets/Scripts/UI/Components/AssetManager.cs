using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WindingTale.UI.Components
{

    public class AssetManager
    {
        private static AssetManager instance = null;

        private static object mutex = new object();

        private AssetBundle iconAssetBundle = null;


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

        }

        private AssetBundle loadBundle(string bundleName)
        {
            return AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));

        }

        public GameObject LoadIconPrefab(string iconName)
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
                GameObject iconPrefab = iconAssetBundle.LoadAsset<GameObject>(iconName);
                return iconPrefab;
            }

            return null;
        }


    }
}