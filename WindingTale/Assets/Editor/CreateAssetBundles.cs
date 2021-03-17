using UnityEngine;
using UnityEditor;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        /*
        string outputFolder = "Assets/StreamingAssets";

        //Check if __Bundles folder exist
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Debug.Log("Folder 'StreamingAssets' does not exist, creating new folder");

            AssetDatabase.CreateFolder("Assets", "StreamingAssets");
        }

        BuildPipeline.BuildAssetBundles(outputFolder, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
        BuildPipeline.BuildAssetBundles(outputFolder, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        */
    }
}