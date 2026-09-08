using System;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 11 -- the woods along the river.
    ///
    /// The party, with Sophia (12) and Laiting (13) since the cave, is spread through
    /// the northern woods when a band of raiders (101..125) comes up from the south.
    /// Three of them are thieves who ignore the fight and go for the chests, then run
    /// for an edge of the map -- a thief that reaches its edge is gone for good, chest
    /// and all. After the opening talk Bailu (14) rides in from the north edge and
    /// joins the party for the battle and beyond.
    ///
    /// Losing Sol (1) ends the chapter; it is won when the last raider falls or flees.
    /// </summary>
    public class Chapter11 : ChapterEvents
    {
        /// <summary>
        /// Where the party stands, as (creature id, x, y). Kaili (10) and Rona (11)
        /// are only in the party if earlier chapters went that way, so everyone from 10
        /// on is settled only when the record carries them.
        /// </summary>
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 19, 11 },
            {  2,  7, 13 },
            {  3, 22,  9 },
            {  4, 14,  8 },
            {  5, 13, 11 },
            {  6,  8,  6 },
            {  7,  7, 10 },
            {  8,  9, 16 },
            {  9, 17, 14 },
            { 10, 24, 12 },
            { 11, 28, 15 },
            { 12,  3, 11 },
            { 13, 26, 17 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The raiders, as (id, definition, x, y), in the original's order.</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 51101, 16, 26 },
            { 102, 51101, 23, 26 },
            { 103, 51101,  8, 27 },
            { 104, 51101,  6, 28 },
            { 105, 51101, 18, 28 },
            { 106, 51101, 21, 29 },
            { 107, 51101, 12, 30 },
            { 108, 51101, 29, 31 },
            { 109, 51101,  5, 32 },
            { 110, 51101,  1, 34 },
            { 111, 51101, 23, 34 },
            { 112, 51101, 31, 35 },
            { 113, 51101,  5, 37 },
            { 114, 51101,  1, 39 },
            { 115, 51101, 35, 39 },
            { 116, 51101, 16, 41 },
            { 117, 51101, 30, 41 },
            { 118, 51101, 10, 42 },
            { 119, 51101,  7, 44 },
            { 120, 51101, 20, 45 },
            { 121, 51102, 30, 28 },
            { 122, 51102, 12, 39 },
            { 123, 51102, 32, 37 },
            { 124, 51102, 14, 44 },
            { 125, 51102, 28, 42 },
        };

        /// <summary>
        /// The thieves, as (id, chest x, chest y, escape x, escape y): each makes for one
        /// chest and then for its own edge of the map, and leaves the battle on reaching it.
        /// </summary>
        private static readonly int[,] Thieves = new int[,]
        {
            { 108, 28, 21, 35,  5 },
            { 119,  1, 32, 23, 45 },
            { 115, 19, 38, 10, 45 },
        };

        /// <summary>Bailu (14) rides in at the top of the map and stops in front of the party.</summary>
        private const int BailuId = 14;
        private static readonly FDPosition BailuEntry = FDPosition.At(18, 1);
        private static readonly FDPosition BailuStop = FDPosition.At(18, 6);

        public Chapter11(GameMain gameMain) : base(gameMain, 11)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);

            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                int thiefId = Thieves[i, 0];
                LoadReachPositionEvent(++eventId, thiefId, FDPosition.At(Thieves[i, 3], Thieves[i, 4]),
                    (gameMain) => gameMain.gameMap.RemoveCreature(thiefId));
            }

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]));
            }

            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                SetCreatureAiTreasure(gameMain, Thieves[i, 0],
                    FDPosition.At(Thieves[i, 1], Thieves[i, 2]),
                    FDPosition.At(Thieves[i, 3], Thieves[i, 4]));
            }

            // Talking
            PushConversationsActivities(gameMain, 11, 1, 1, 12);

            // Bailu appears only once the first half of the talk is over, and rides in
            // before the second half -- the original chained this as initialBattle_2.
            gameMain.PushActivity((gameMain) =>
            {
                AddCreatureToMap(gameMain, CreatureFaction.Friend, BailuId, BailuId, BailuEntry);

                gameMain.PushActivity(new SlideCursorActivity(BailuStop.X, BailuStop.Y));
                gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(BailuId,
                    FDMovePath.Create(BailuEntry, BailuStop)));

                // Talking
                PushConversationsActivities(gameMain, 11, 1, 13, 25);

                gameMain.PushActivity((gameMain) =>
                {
                    // Play background music
                    gameMain.PlayBackgroundMusic();
                });
            });
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 11, 2, 1, 12);

            gameMain.OnGameWin();
        };
    }
}
