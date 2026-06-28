using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene
{
    /// <summary>
    /// Installs the retro pixel-art sky as a real skybox at runtime via
    /// RenderSettings.skybox (shader "Skybox/RetroGradient"). A skybox renders behind
    /// every camera whose Clear Flags = Skybox, so it always shows in the Game view
    /// regardless of camera position (unlike a sphere object that the camera can end up
    /// outside of). Attach to the main camera; colours/bands are tunable in the Inspector.
    /// </summary>
    public class SkySphere : MonoBehaviour
    {
        public Color topColor = new Color(0.16f, 0.30f, 0.58f, 1f);
        public Color horizonColor = new Color(0.95f, 0.80f, 0.60f, 1f);
        public Color bottomColor = new Color(0.30f, 0.34f, 0.30f, 1f);

        [Range(-1f, 1f)] public float horizonHeight = 0.0f;
        [Range(0.2f, 4f)] public float gradientCurve = 1.0f;
        [Range(2, 32)] public int bands = 8;
        [Range(0f, 1f)] public float ditherStrength = 1f;
        [Range(1, 8)] public int pixelSize = 2;

        void Start()
        {
            Shader shader = Shader.Find("Skybox/RetroGradient");
            if (shader == null)
            {
                Debug.LogError("[SkySphere] Shader 'Skybox/RetroGradient' not found.");
                return;
            }

            Material mat = new Material(shader);
            mat.SetColor("_TopColor", topColor);
            mat.SetColor("_HorizonColor", horizonColor);
            mat.SetColor("_BottomColor", bottomColor);
            mat.SetFloat("_HorizonHeight", horizonHeight);
            mat.SetFloat("_HorizonSharpness", gradientCurve);
            mat.SetFloat("_Bands", bands);
            mat.SetFloat("_DitherStrength", ditherStrength);
            mat.SetFloat("_PixelSize", pixelSize);

            RenderSettings.skybox = mat;

            // Make sure the camera actually clears to the skybox.
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
            }
        }
    }
}
