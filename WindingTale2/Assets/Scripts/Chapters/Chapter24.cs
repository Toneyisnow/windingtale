using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 24 -- the island in the void.
    ///
    /// The party stands in a ring around the three stone pillars at the heart of a
    /// small island floating in blackness, with nothing but the void and four shafts
    /// of light around it. Four enemies (101..104) hang in the void at the four
    /// corners to begin with, and every few turns more come out of it: eight on turn
    /// 2 (105..112), twelve on turn 4 (113..124), sixteen on turn 7 (125..140) and
    /// twenty on turn 10 (141..160), the last wave led by the four strongest
    /// (52401). Everything drops something. Losing Sol (1) ends the chapter; it is
    /// won when the last enemy falls.
    ///
    /// Friends 17..29, which the original never settled, take free tiles inside and
    /// beside the ring when the party carries them, since the save keeps only friends
    /// on the map.
    /// </summary>
    public class Chapter24 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 21, 20 },
            {  2, 22, 20 },
            {  3, 20, 20 },
            {  4, 23, 19 },
            {  5, 23, 18 },
            {  6, 23, 17 },
            {  7, 22, 17 },
            {  8, 22, 16 },
            {  9, 21, 16 },
            { 10, 20, 16 },
            { 11, 20, 17 },
            { 12, 19, 17 },
            { 13, 19, 18 },
            { 14, 19, 19 },
            { 15, 20, 19 },
            { 16, 22, 19 },
            { 17, 21, 18 },
            { 18, 21, 19 },
            { 19, 18, 18 },
            { 20, 24, 18 },
            { 21, 19, 16 },
            { 22, 23, 16 },
            { 23, 21, 15 },
            { 24, 20, 15 },
            { 25, 22, 15 },
            { 26, 17, 19 },
            { 27, 25, 19 },
            { 28, 18, 19 },
            { 29, 24, 19 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The enemy, wave by wave, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] FirstEnemies = new int[,]
        {
            { 101, 52402,  8,  9, 103 },
            { 102, 52402, 33,  7, 104 },
            { 103, 52402,  8, 27, 103 },
            { 104, 52402, 33, 29, 104 },
        };

        private static readonly int[,] Turn2Enemies = new int[,]
        {
            { 105, 52403,  8, 10, 103 },
            { 106, 52403,  9,  9, 103 },
            { 107, 52403, 32,  7, 114 },
            { 108, 52403, 33,  8, 901 },
            { 109, 52403,  8, 26, 901 },
            { 110, 52403,  9, 27, 106 },
            { 111, 52403, 32, 29, 103 },
            { 112, 52403, 33, 28, 103 },
        };

        private static readonly int[,] Turn4Enemies = new int[,]
        {
            { 113, 52405,  8,  9, 901 },
            { 114, 52405,  6,  9, 901 },
            { 115, 52405,  8,  7, 241 },
            { 116, 52405, 33,  7, 103 },
            { 117, 52405, 35,  7, 902 },
            { 118, 52405, 33,  5, 113 },
            { 119, 52405,  8, 27, 325 },
            { 120, 52405,  6, 27, 106 },
            { 121, 52405,  8, 29, 116 },
            { 122, 52405, 33, 29, 901 },
            { 123, 52405, 35, 29, 332 },
            { 124, 52405, 33, 31, 901 },
        };

        private static readonly int[,] Turn7Enemies = new int[,]
        {
            { 125, 52405,  8,  9, 103 },
            { 126, 52405,  6,  9, 901 },
            { 127, 52405,  8,  7, 104 },
            { 128, 52404,  6,  7, 111 },
            { 129, 52405, 33,  7, 106 },
            { 130, 52405, 35,  7, 112 },
            { 131, 52405, 33,  5, 115 },
            { 132, 52404, 35,  5, 103 },
            { 133, 52405,  8, 27, 103 },
            { 134, 52405,  6, 27, 106 },
            { 135, 52405,  8, 29, 104 },
            { 136, 52404,  6, 29, 901 },
            { 137, 52405, 33, 29, 103 },
            { 138, 52405, 35, 29, 902 },
            { 139, 52405, 33, 31, 803 },
            { 140, 52404, 35, 31, 111 },
        };

        private static readonly int[,] Turn10Enemies = new int[,]
        {
            { 141, 52403,  8,  9, 103 },
            { 142, 52405,  6,  9, 107 },
            { 143, 52405,  8,  7, 104 },
            { 144, 52403,  6,  7, 103 },
            { 145, 52401,  7,  8, 901 },
            { 146, 52403, 33,  7, 803 },
            { 147, 52405, 35,  7, 112 },
            { 148, 52405, 33,  5, 902 },
            { 149, 52403, 35,  5, 106 },
            { 150, 52401, 34,  6, 103 },
            { 151, 52403,  8, 27, 903 },
            { 152, 52405,  6, 27, 209 },
            { 153, 52405,  8, 29, 108 },
            { 154, 52403,  6, 29, 804 },
            { 155, 52401,  7, 28, 104 },
            { 156, 52403, 33, 29, 107 },
            { 157, 52405, 35, 29, 103 },
            { 158, 52405, 33, 31, 111 },
            { 159, 52403, 35, 31, 103 },
            { 160, 52401, 34, 30, 106 },
        };

        public Chapter24(GameMain gameMain) : base(gameMain, 24)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the waves, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 2, CreatureFaction.Npc, (gameMain) => AddEnemies(gameMain, Turn2Enemies));
            LoadTurnEvent(++eventId, 4, CreatureFaction.Npc, (gameMain) => AddEnemies(gameMain, Turn4Enemies));
            LoadTurnEvent(++eventId, 7, CreatureFaction.Npc, (gameMain) => AddEnemies(gameMain, Turn7Enemies));
            LoadTurnEvent(++eventId, 10, CreatureFaction.Npc, (gameMain) => AddEnemies(gameMain, Turn10Enemies));

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private static void AddEnemies(GameMain gameMain, int[,] enemies)
        {
            for (int i = 0; i < enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, enemies[i, 0], enemies[i, 1],
                    FDPosition.At(enemies[i, 2], enemies[i, 3]), enemies[i, 4]);
            }
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);
            AddEnemies(gameMain, FirstEnemies);

            // Talking
            PushConversationsActivities(gameMain, 24, 1, 1, 14);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>The last enemy falls.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 24, 2, 1, 11);

            gameMain.OnGameWin();
        };
    }
}
