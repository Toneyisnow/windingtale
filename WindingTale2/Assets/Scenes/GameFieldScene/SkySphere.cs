using UnityEngine;

namespace WindingTale.Scenes.GameFieldScene
{
    /// <summary>
    /// Installs the battlefield's sky as a real skybox at runtime (RenderSettings.skybox). A
    /// skybox renders behind every camera whose Clear Flags = Skybox, so it always shows
    /// regardless of camera position (unlike a sphere object that the camera can end up
    /// outside of). Attach to the main camera.
    ///
    /// Several skies exist, each a procedural skybox shader under Resources/Skies, and every
    /// chapter picks one by name with the "Sky" entry of its JSON (see ChapterDefinition.Sky),
    /// the way it picks its music. GameMain calls SetSky once the chapter is loaded; until
    /// then, and for a chapter that names none, the standard blue sky stands.
    ///
    ///   SKY_01  standard blue sky with white clouds
    ///   SKY_02  volcanic cave: dark rock ceiling, veins of magma and a sea of lava
    /// </summary>
    public class SkySphere : MonoBehaviour
    {
        public const string DefaultSky = "SKY_01";

        private Material skyMaterial = null;
        private string currentSky = null;

        void Start()
        {
            // A chapter that loaded before this ran has already asked for its sky.
            if (currentSky == null)
            {
                SetSky(DefaultSky);
            }
        }

        /// <summary>
        /// Puts the named sky up. An unknown or empty name falls back to the standard sky, so
        /// a typo in a chapter file costs the chapter its sky, not the whole picture.
        /// </summary>
        public void SetSky(string skyId)
        {
            string shaderName = GetShaderName(skyId);
            if (string.IsNullOrEmpty(skyId) || shaderName == null)
            {
                skyId = DefaultSky;
                shaderName = GetShaderName(skyId);
            }

            if (skyId == currentSky)
            {
                return;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[SkySphere] Shader '" + shaderName + "' not found.");
                return;
            }

            if (skyMaterial != null)
            {
                Destroy(skyMaterial);
            }

            skyMaterial = new Material(shader);
            RenderSettings.skybox = skyMaterial;
            currentSky = skyId;

            // Make sure the camera actually clears to the skybox.
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
            }
        }

        private static string GetShaderName(string skyId)
        {
            switch (skyId)
            {
                case "SKY_01":
                    return "Skybox/WT_Sky01";
                case "SKY_02":
                    return "Skybox/WT_Sky02";
                default:
                    return null;
            }
        }
    }
}
