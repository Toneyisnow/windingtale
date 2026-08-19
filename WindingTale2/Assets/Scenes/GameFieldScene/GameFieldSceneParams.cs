public static class GameFiledSceneParams
{
    public static bool isContinue;

    /// <summary>
    /// The GlobalVariables key the chapter to play is handed over on: the title screen's
    /// New Game sets 1, and the village sets the chapter its party is up to. GameMain takes
    /// it on Start; nothing there (the scene opened on its own) means chapter 1.
    /// </summary>
    public const string ChapterIdVariableName = "ChapterId";
}