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
        /// Chosen window/backbuffer size. 0x0 means "never chosen" -- the game keeps the
        /// display's native resolution on first run rather than forcing a fixed size.
        /// </summary>
        public int ScreenWidth { get; set; } = 0;
        public int ScreenHeight { get; set; } = 0;

        /// <summary>
        /// Fullscreen vs. windowed. Defaults to fullscreen, the usual first-run expectation
        /// on both PC and tablet.
        /// </summary>
        public bool IsFullScreen { get; set; } = true;
    }
}
