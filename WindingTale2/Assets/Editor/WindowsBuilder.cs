using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line entry point for producing the Windows (Win64) standalone build.
/// Invoke with: Unity.exe -batchmode -quit -executeMethod WindowsBuilder.BuildWindows
/// </summary>
public static class WindowsBuilder
{
    [UnityEditor.MenuItem("Build/Build Windows (1280x768 Windowed)")]
    public static void BuildWindows()
    {
        // Windowed mode, 1280x768, not full screen.
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 768;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultIsNativeResolution = false;

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        string exeName = PlayerSettings.productName + ".exe";

        string outputDir;
        if (Application.isBatchMode)
        {
            // No UI available in batch mode: fall back to the default folder.
            outputDir = "Build/Windows";
        }
        else
        {
            // Let the user choose where to build.
            outputDir = EditorUtility.SaveFolderPanel(
                "Choose Windows Build Folder", "", "");
            if (string.IsNullOrEmpty(outputDir))
            {
                Debug.Log("[WindowsBuilder] Build cancelled by user.");
                return;
            }
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = System.IO.Path.Combine(outputDir, exeName),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[WindowsBuilder] Build succeeded: {summary.totalSize} bytes at {summary.outputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[WindowsBuilder] Build failed: {summary.result}, {summary.totalErrors} errors");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
