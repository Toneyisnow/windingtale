namespace WindingTale.Core.Files
{
    /// <summary>
    /// The player's own preferences, persisted across runs by UserSettingsManager. This is
    /// the single bag every future setting goes into (audio volume, language, ...); add a
    /// property here and it is saved and loaded for free.
    ///
    /// Property initializers double as the defaults for a first run *and* for any field a
    /// previously-saved file predates: Newtonsoft runs this constructor before it overlays
    /// the JSON, so a field the file never had keeps the initializer value instead of a bare
    /// zero/null. Choose each default so it means "not chosen yet".
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// Chosen window/backbuffer size. Defaults to 1280x768 so the game comes up windowed
        /// at that size on first run; a 0x0 value (from an older saved file) still means
        /// "never chosen" and keeps the display's native resolution.
        /// </summary>
        public int ScreenWidth { get; set; } = 1280;
        public int ScreenHeight { get; set; } = 768;

        /// <summary>
        /// Fullscreen vs. windowed. Defaults to windowed, matching the fixed 1280x768 window
        /// the desktop build ships with.
        /// </summary>
        public bool IsFullScreen { get; set; } = false;
    }
}
