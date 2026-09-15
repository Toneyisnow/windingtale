using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.FightObjects;

/// <summary>
/// Renders the battle scene from its own camera with fight bodies shown as sprites and as
/// the 3D models from Resources/Fights3D (FightModel3D), without entering play mode, so a
/// conversion can be judged where it is actually seen.
///
/// Batch mode (no -nographics, the camera has to render):
///   Unity.exe -batchmode -projectPath WindingTale2 -executeMethod FightModelCapture.CaptureFromCommandLine
///             -friend 001 -enemy 701 -out D:/capture -quit
/// writes, for every idle and attack frame of each side (the other side holding its first
/// idle frame): friend_attack_07_2d.png / friend_attack_07_3d.png, enemy_idle_01_2d.png ...
/// Nothing is saved back to the scene.
/// </summary>
public static class FightModelCapture
{
    private const string ScenePath = "Assets/Scenes/GameBattleScene/GameBattleScene.unity";
    private const int Width = 1600;
    private const int Height = 900;

    [UnityEditor.MenuItem("Tools/Capture Fight Model/001 vs 701")]
    public static void Capture001()
    {
        CaptureInEditor(1, 701);
    }

    [UnityEditor.MenuItem("Tools/Capture Fight Model/002 vs 701")]
    public static void Capture002()
    {
        CaptureInEditor(2, 701);
    }

    [UnityEditor.MenuItem("Tools/Capture Fight Model/003 vs 701")]
    public static void Capture003()
    {
        CaptureInEditor(3, 701);
    }

    private static void CaptureInEditor(int friendId, int enemyId)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "FightCapture",
            StringUtils.Digit3(friendId) + "_vs_" + StringUtils.Digit3(enemyId)));
        try
        {
            Capture(friendId, enemyId, output);
        }
        finally
        {
            if (setup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
        UnityEditor.EditorUtility.RevealInFinder(output);
    }

    public static void CaptureFromCommandLine()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int friendId = int.Parse(ArgValue(args, "-friend", "1"));
        int enemyId = int.Parse(ArgValue(args, "-enemy", "701"));
        Capture(friendId, enemyId, ArgValue(args, "-out", Path.Combine(Application.dataPath, "..", "Temp", "FightCapture")));
    }

    public static void Capture(int friendId, int enemyId, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject friend = GameObject.Find("LocalBody");
        GameObject enemy = GameObject.Find("ForeignBody");
        Sprite[] friendSprites = LoadFrames(friendId);
        Sprite[] enemySprites = LoadFrames(enemyId);

        FightModel3D friendModel = FightModel3D.Attach(friend, friendId);
        FightModel3D enemyModel = FightModel3D.Attach(enemy, enemyId);
        Debug.Log("FightModelCapture: friend models " + (friendModel != null) + ", enemy models " + (enemyModel != null));

        Camera camera = Camera.main;
        RenderTexture target = new RenderTexture(Width, Height, 24);
        camera.targetTexture = target;
        Texture2D readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);

        Sprite friendRest = friendSprites.First(s => s.name.Contains("-1-01"));
        Sprite enemyRest = enemySprites.First(s => s.name.Contains("-1-01"));

        foreach (Sprite sprite in friendSprites)
        {
            Shoot(camera, target, readback, outputFolder, "friend", sprite, friend, sprite, enemy, enemyRest, friendModel, enemyModel);
        }
        foreach (Sprite sprite in enemySprites)
        {
            Shoot(camera, target, readback, outputFolder, "enemy", sprite, friend, friendRest, enemy, sprite, friendModel, enemyModel);
        }

        FightModel3D.Enabled = true;
        RenderTexture.active = null;
        camera.targetTexture = null;
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(readback);
        Debug.Log("FightModelCapture: wrote " + outputFolder);
    }

    private static Sprite[] LoadFrames(int animationId)
    {
        string digits = StringUtils.Digit3(animationId);
        return Resources.LoadAll<Sprite>(string.Format("Fights/{0}/Fight-{0}", digits))
            .Where(s => s.name.Contains("Fight-" + digits + "-1-") || s.name.Contains("Fight-" + digits + "-2-"))
            .OrderBy(s => s.name)
            .ToArray();
    }

    private static void Shoot(Camera camera, RenderTexture target, Texture2D readback, string outputFolder, string side,
        Sprite frame, GameObject friend, Sprite friendSprite, GameObject enemy, Sprite enemySprite,
        FightModel3D friendModel, FightModel3D enemyModel)
    {
        friend.GetComponent<SpriteRenderer>().sprite = friendSprite;
        enemy.GetComponent<SpriteRenderer>().sprite = enemySprite;

        string label = FrameLabel(frame.name);
        foreach (bool threeD in new[] { false, true })
        {
            FightModel3D.Enabled = threeD;
            if (friendModel != null) friendModel.Sync();
            if (enemyModel != null) enemyModel.Sync();

            RenderTexture.active = target;
            camera.Render();
            readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            readback.Apply();
            File.WriteAllBytes(Path.Combine(outputFolder, string.Format("{0}_{1}_{2}.png", side, label, threeD ? "3d" : "2d")),
                readback.EncodeToPNG());
        }
    }

    // "Fight-001-2-07.png" -> "attack_07"
    private static string FrameLabel(string spriteName)
    {
        string[] parts = Path.GetFileNameWithoutExtension(spriteName).Split('-');
        string animation = parts[2] == "1" ? "idle" : parts[2] == "2" ? "attack" : "spell";
        return animation + "_" + parts[3];
    }

    private static string ArgValue(string[] args, string name, string fallback)
    {
        int index = System.Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
