using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 14 -- the forest road.
    ///
    /// The party, sixteen strong now, comes in at the south-east corner of a wood
    /// that is full of enemies lying in wait: the near half (101..128) breaks cover on
    /// turn 3, when their commanders are heard giving the order, and the far half
    /// (129..154) on turn 10. Losing Sol (1) ends the chapter; it is won when the last
    /// enemy falls.
    ///
    /// The original also carries a bossDyingMessage handler that plays chapter 10's
    /// lines -- a leftover that loadEvents never registers, so it is not ported.
    /// </summary>
    public class Chapter14 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 33, 34 },
            {  2, 35, 33 },
            {  3, 34, 31 },
            {  4, 37, 31 },
            {  5, 38, 34 },
            {  6, 36, 36 },
            {  7, 33, 36 },
            {  8, 30, 35 },
            {  9, 30, 38 },
            { 10, 35, 38 },
            { 11, 38, 37 },
            { 12, 40, 32 },
            { 13, 35, 29 },
            { 14, 28, 37 },
            { 15, 30, 33 },
            { 16, 31, 30 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>
        /// Everyone lying in wait, as (id, definition, x, y, drop item): the near half
        /// (101..128) first, then the far half (129..154).
        /// </summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 51404, 20, 20,   0 },
            { 102, 51404, 12, 23,   0 },
            { 103, 51404, 31, 13, 901 },
            { 104, 51401, 17, 21, 901 },
            { 105, 51401, 22, 18,   0 },
            { 106, 51401, 26, 13, 902 },
            { 107, 51401, 17, 10,   0 },
            { 108, 51402, 14, 31,   0 },
            { 109, 51402, 15, 27,   0 },
            { 110, 51402, 17, 24,   0 },
            { 111, 51402, 21, 22,   0 },
            { 112, 51402, 20, 16, 103 },
            { 113, 51402, 23, 14,   0 },
            { 114, 51402, 24, 10,   0 },
            { 115, 51402, 29,  6,   0 },
            { 116, 51402, 23,  7,   0 },
            { 117, 51402, 21, 11, 901 },
            { 118, 51402, 18, 13,   0 },
            { 119, 51402, 16, 16,   0 },
            { 120, 51402, 11, 20,   0 },
            { 121, 51402,  8, 24,   0 },
            { 122, 51403, 18, 26,   0 },
            { 123, 51403, 10, 26, 902 },
            { 124, 51403, 24, 18,   0 },
            { 125, 51403, 15, 14,   0 },
            { 126, 51405, 18, 18,   0 },
            { 127, 51405, 13, 17, 339 },
            { 128, 51405,  5, 22,   0 },

            { 129, 51404,  2,  3, 106 },
            { 130, 51404, 21,  3,   0 },
            { 131, 51404, 10,  9,   0 },
            { 132, 51401,  8, 20,   0 },
            { 133, 51401, 10, 12,   0 },
            { 134, 51401, 13,  8,   0 },
            { 135, 51401,  5,  5,   0 },
            { 136, 51402, 19,  8,   0 },
            { 137, 51402, 11, 15,   0 },
            { 138, 51402,  8, 17, 804 },
            { 139, 51402,  1, 17,   0 },
            { 140, 51402,  5, 15,   0 },
            { 141, 51402,  8, 13,   0 },
            { 142, 51402, 16,  7,   0 },
            { 143, 51402, 16,  2, 901 },
            { 144, 51402, 11,  6,   0 },
            { 145, 51402,  8,  7,   0 },
            { 146, 51402,  6, 10,   0 },
            { 147, 51402,  3,  8,   0 },
            { 148, 51403,  5, 19, 901 },
            { 149, 51403, 18,  5,   0 },
            { 150, 51403, 10,  2,   0 },
            { 151, 51403,  7,  3,   0 },
            { 152, 51405, 13,  3, 107 },
            { 153, 51405,  3, 13,   0 },
            { 154, 51405, 13, 11,   0 },
        };

        private const int FirstNearId = 101;
        private const int LastNearId = 128;
        private const int FirstFarId = 129;
        private const int LastFarId = 154;

        private const int NearAttackTurn = 3;
        private const int FarAttackTurn = 10;

        public Chapter14(GameMain gameMain) : base(gameMain, 14)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, NearAttackTurn, CreatureFaction.Npc, nearAttack);
            LoadTurnEvent(++eventId, FarAttackTurn, CreatureFaction.Npc, farAttack);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            // Every enemy holds still until its half is called out.
            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4], AITypes.AIType_StandBy);
            }

            // Talking
            PushConversationsActivities(gameMain, 14, 1, 1, 4);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 3: the commanders give the order and the near half attacks.</summary>
        private Action<GameMain> nearAttack = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 14, 2, 1, 4);

            for (int id = FirstNearId; id <= LastNearId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
        };

        /// <summary>Turn 10: the far half attacks.</summary>
        private Action<GameMain> farAttack = (gameMain) =>
        {
            for (int id = FirstFarId; id <= LastFarId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 14, 3, 1, 17);

            gameMain.OnGameWin();
        };
    }
}
