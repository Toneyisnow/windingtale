using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace WindingTale.Core.Files
{
    /// <summary>
    /// Loads, persists and applies <see cref="UserSettings"/>. Mirrors the save-record
    /// managers next door: one JSON file, written to Application.dataPath in the editor so
    /// it is easy to inspect and to Application.persistentDataPath in a build.
    ///
    /// The loaded settings live in <see cref="Current"/> as a single shared instance, so a
    /// settings screen mutates that instance, calls the matching Apply* helper to preview
    /// the change live, and finally <see cref="Save"/> to keep it.
    /// </summary>
    public static class UserSettingsManager
    {
        private const string FileName = "UserSettings.json";

        private static UserSettings current;

        /// <summary>
        /// The active settings, loaded from disk on first access (defaults if there is no
        /// file yet). Never null.
        /// </summary>
        public static UserSettings Current
        {
            get
            {
                if (current == null)
                {
                    current = Load();
                }
                return current;
            }
        }

        /// <summary>
        /// Reads the settings file, falling back to defaults when it is missing or
        /// unreadable -- a corrupt or hand-edited file must never stop the game booting.
        /// </summary>
        public static UserSettings Load()
        {
            string path = GetFilePath();
            if (!File.Exists(path))
            {
                return new UserSettings();
            }

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<UserSettings>(json) ?? new UserSettings();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to read UserSettings, using defaults: " + e.Message);
                return new UserSettings();
            }
        }

        /// <summary>
        /// Writes <see cref="Current"/> back to disk.
        /// </summary>
        public static void Save()
        {
            string json = JsonConvert.SerializeObject(Current);
            File.WriteAllText(GetFilePath(), json);
        }

        /// <summary>
        /// Pushes the saved resolution to the engine. A 0x0 size means "never chosen", so we
        /// only switch fullscreen mode and leave the display at its native resolution rather
        /// than forcing a fixed size on first run.
        /// </summary>
        public static void ApplyResolution()
        {
            UserSettings settings = Current;
            FullScreenMode mode = settings.IsFullScreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            if (settings.ScreenWidth > 0 && settings.ScreenHeight > 0)
            {
                Screen.SetResolution(settings.ScreenWidth, settings.ScreenHeight, mode);
            }
            else
            {
                Screen.fullScreenMode = mode;
            }
        }

        /// <summary>
        /// The single call a settings screen needs: record a new resolution, apply it live,
        /// and persist it.
        /// </summary>
        public static void SetResolution(int width, int height, bool isFullScreen)
        {
            Current.ScreenWidth = width;
            Current.ScreenHeight = height;
            Current.IsFullScreen = isFullScreen;
            ApplyResolution();
            Save();
        }

        /// <summary>
        /// Applies the saved settings once, before the first scene loads, so every scene
        /// comes up at the player's chosen resolution without needing an object placed in it.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            ApplyResolution();
        }

        private static string GetFilePath()
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, FileName);
#else
            return Path.Combine(Application.persistentDataPath, FileName);
#endif
        }
    }
}
