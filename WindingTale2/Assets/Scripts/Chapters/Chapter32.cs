using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 32 -- the bad ending.
    ///
    /// Not a battle: without the Sky Key the party cannot follow Youni up to the Golden
    /// City, and she goes alone to take it away for good. Chapter 27 hands over here
    /// through GameMain.OnGameEnding when the last captain falls and nobody carries
    /// item 814. Youni (2) stands ahead of the others on the transfer platform, and
    /// the farewell is chapter 27's conversation 5, lines 1..15. There is no ending
    /// screen yet (the original went on to its GameWinScene), so the last line returns
    /// the game to the title -- see GameMain.OnGameFinished.
    /// </summary>
    public class Chapter32 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  2, 16, 10 },
            {  1, 16, 12 },
            {  3, 17, 12 },
            {  4, 18, 12 },
            {  5, 15, 12 },
            {  6, 14, 12 },
            {  7, 16, 13 },
            {  8, 17, 13 },
            {  9, 18, 13 },
            { 10, 15, 13 },
            { 11, 14, 13 },
            { 12, 16, 14 },
            { 13, 17, 14 },
            { 14, 18, 14 },
            { 15, 15, 14 },
            { 16, 14, 14 },
        };

        private const int FirstOptionalFriendId = 10;

        public Chapter32(GameMain gameMain) : base(gameMain, 32)
        {
            int eventId = 0;

            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            // Talking -- the farewell is written in chapter 27's strings.
            PushConversationsActivities(gameMain, 27, 5, 1, 15);

            gameMain.OnGameFinished();
        };
    }
}
