using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class BundleObjectLoader : MonoBehaviour
{
    public string assetName = "Icons/AssetBundleTest/Icon_002_02";
    public string bundleName = "assetbundletest";

    // Start is called before the first frame update
    void Start()
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));

        if (bundle != null)
        {
            GameObject asset = bundle.LoadAsset<GameObject>("Icon_002_02");
        }


    }

}
