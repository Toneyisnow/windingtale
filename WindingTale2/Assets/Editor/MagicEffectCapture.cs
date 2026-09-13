using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using WindingTale.Scenes.GameBattleScene;

/// <summary>
/// Renders a battle magic frame by frame from the battle scene's own camera, without
/// entering play mode, so a MagicEffectDefinition can be checked against its reference GIF.
///
/// Batch mode (no -nographics, the camera has to render):
///   Unity.exe -batchmode -projectPath WindingTale2 -executeMethod MagicEffectCapture.CaptureFromCommandLine
///             -magicId 101 -out D:/capture -quit
/// writes enemy_NNN.png (an enemy thief on the left takes it) and friend_NNN.png (a friend
/// on the right does), one per original frame. Nothing is saved back to the scene.
/// </summary>
public static class MagicEffectCapture
{
    private const string ScenePath = "Assets/Scenes/GameBattleScene/GameBattleScene.unity";
    private const int Width = 1600;
    private const int Height = 900;
    private const int ExtraFrames = 10;

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/101 火焰术")]
    public static void CaptureFireMagic()
    {
        CaptureInEditor(101);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/102 烈焰术")]
    public static void CaptureBlazeMagic()
    {
        CaptureInEditor(102);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/103 炎龙术")]
    public static void CaptureDragonMagic()
    {
        CaptureInEditor(103);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/104 天火术")]
    public static void CaptureSkyFireMagic()
    {
        CaptureInEditor(104);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/105 电击术")]
    public static void CaptureShockMagic()
    {
        CaptureInEditor(105);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/106 落雷术")]
    public static void CaptureThunderfallMagic()
    {
        CaptureInEditor(106);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/107 轰雷术")]
    public static void CaptureThunderstormMagic()
    {
        CaptureInEditor(107);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/108 神雷术")]
    public static void CaptureHolyThunderMagic()
    {
        CaptureInEditor(108);
    }

    [UnityEditor.MenuItem("Tools/Capture Magic Effect/109 圣光弹")]
    public static void CaptureHolyLightMagic()
    {
        CaptureInEditor(109);
    }

    private static void CaptureInEditor(int magicId)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        // The capture opens the battle scene over whatever is being edited; put that back after.
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "MagicCapture", magicId.ToString()));
        try
        {
            Capture(magicId, output);
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
        int magicId = int.Parse(ArgValue(args, "-magicId", "101"));
        Capture(magicId, ArgValue(args, "-out", Path.Combine(Application.dataPath, "..", "Temp", "MagicCapture")));
    }

    public static void Capture(int magicId, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);
        MagicEffectDefinition definition = MagicEffectDefinition.Get(magicId);

        CaptureSide(definition, "enemy", "ForeignBody", "Fights/701/Fight-701", false, outputFolder);
        CaptureSide(definition, "friend", "LocalBody", "Fights/101/Fight-101", true, outputFolder);
        Debug.Log("MagicEffectCapture: wrote " + outputFolder);
    }

    private static void CaptureSide(MagicEffectDefinition definition, string prefix, string bodyName, string sheet,
        bool targetIsFriend, string outputFolder)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject body = GameObject.Find(bodyName);
        GameObject other = GameObject.Find(targetIsFriend ? "ForeignBody" : "LocalBody");
        SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();
        bodyRenderer.sprite = Resources.LoadAll<Sprite>(sheet).First(s => s.name.EndsWith("-1-01"));
        other.GetComponent<SpriteRenderer>().enabled = false;

        Camera camera = Camera.main;
        RenderTexture target = new RenderTexture(Width, Height, 24);
        camera.targetTexture = target;
        Texture2D readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);

        MagicEffect effect = MagicEffect.Play(definition, body, body.transform.localPosition, targetIsFriend, true,
            percent => Debug.Log(prefix + " hit " + percent + "%"), () => Debug.Log(prefix + " complete"));
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();

        const int substeps = 6;
        int frames = definition.ScreenFlash.Length + definition.TotalFrames + ExtraFrames;
        for (int frame = 0; frame < frames; frame++)
        {
            RenderTexture.active = target;
            // Outside play mode nothing rebuilds the screen flash canvas before a render.
            Canvas.ForceUpdateCanvases();
            camera.Render();
            readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            readback.Apply();
            File.WriteAllBytes(Path.Combine(outputFolder, string.Format("{0}_{1:D3}.png", prefix, frame)), readback.EncodeToPNG());

            float dt = definition.FrameDuration / substeps;
            for (int i = 0; i < substeps; i++)
            {
                effect.Advance(dt);
                foreach (ParticleSystem system in particles)
                {
                    system.Simulate(dt, true, false, false);
                }
            }
        }

        RenderTexture.active = null;
        camera.targetTexture = null;
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(readback);
    }

    private static string ArgValue(string[] args, string name, string fallback)
    {
        int index = System.Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
