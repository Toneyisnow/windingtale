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
    /// Chapter 22 -- the plateau of the six orbs.
    ///
    /// The party comes up the stairs at the south of a round stone plateau in the
    /// dark. The enemy (101..149) and its master (199) hold the platform above: the
    /// master at the foot of the monument, guards (52203, 52204, 52206, 52208) posted
    /// at the orbs, the statues and the stairs who hold their ground, and the rest
    /// coming down to meet the party. On turn 3 and again on turn 7 six more (201..212)
    /// climb out of the dark at the bottom corners of the map, and on turn 5 Sala (27)
    /// comes up from the south edge to join the party. Losing Sol (1) or Xiya (25)
    /// ends the chapter; it is won when the last enemy falls, and Xiya is given item
    /// 263 at the end.
    ///
    /// Departures from the original: it numbered two enemies 120, so the second --
    /// the 52205 at (15, 28) -- is 149 here and, unlike the first, takes no Guard AI
    /// (the original's setAiOfId only ever found the first); and its second wave
    /// numbered one arrival 200, which is 210 here. Friends 17..26, which it never
    /// settled, take free tiles behind the formation when the party carries them,
    /// since the save keeps only friends on the map.
    /// </summary>
    public class Chapter22 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 23, 28 },
            {  2, 22, 29 },
            {  3, 24, 29 },
            {  4, 23, 29 },
            {  5, 22, 30 },
            {  6, 24, 30 },
            {  7, 23, 30 },
            {  8, 22, 31 },
            {  9, 24, 31 },
            { 10, 23, 31 },
            { 11, 22, 32 },
            { 12, 24, 32 },
            { 13, 23, 32 },
            { 14, 21, 31 },
            { 15, 25, 31 },
            { 16, 23, 33 },
            { 17, 21, 29 },
            { 18, 25, 29 },
            { 19, 21, 30 },
            { 20, 25, 30 },
            { 21, 21, 32 },
            { 22, 25, 32 },
            { 23, 22, 33 },
            { 24, 24, 33 },
            { 25, 22, 34 },
            { 26, 24, 34 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The garrison of the plateau, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 52202, 12, 21,   0 },
            { 102, 52202, 34, 21, 106 },
            { 103, 52202, 16, 12,   0 },
            { 104, 52202, 30, 12,   0 },
            { 105, 52203, 21, 17,   0 },
            { 106, 52203, 25, 17, 106 },
            { 107, 52203, 18, 25,   0 },
            { 108, 52203, 28, 25,   0 },
            { 109, 52204,  9, 25, 112 },
            { 110, 52204,  9, 26,   0 },
            { 111, 52204,  9, 27, 103 },
            { 112, 52204, 37, 25,   0 },
            { 113, 52204, 37, 26,   0 },
            { 114, 52204, 37, 27, 902 },
            { 115, 52204, 22,  9,   0 },
            { 116, 52204, 23,  9,   0 },
            { 117, 52204, 24,  9,   0 },
            { 118, 52204, 22, 19,   0 },
            { 119, 52204, 23, 19, 902 },
            { 120, 52204, 24, 19,   0 },
            { 149, 52205, 15, 28,   0 },
            { 121, 52205, 31, 28,   0 },
            { 122, 52205, 22, 22,   0 },
            { 123, 52205, 24, 22,   0 },
            { 124, 52205, 20, 19,   0 },
            { 125, 52205, 26, 19, 903 },
            { 126, 52206, 14, 17,   0 },
            { 127, 52206, 14, 16, 349 },
            { 128, 52206, 15, 16,   0 },
            { 129, 52206, 32, 17,   0 },
            { 130, 52206, 32, 16,   0 },
            { 131, 52206, 31, 16, 103 },
            { 132, 52206, 22, 20,   0 },
            { 133, 52206, 23, 20,   0 },
            { 134, 52206, 24, 20, 903 },
            { 135, 52207, 13, 26,   0 },
            { 136, 52207, 14, 27,   0 },
            { 137, 52207, 15, 26,   0 },
            { 138, 52207, 16, 27,   0 },
            { 139, 52207, 30, 27,   0 },
            { 140, 52207, 31, 26,   0 },
            { 141, 52207, 32, 27, 102 },
            { 142, 52207, 33, 26,   0 },
            { 143, 52208, 18, 21,   0 },
            { 144, 52208, 28, 21,   0 },
            { 145, 52208, 12, 13, 103 },
            { 146, 52208, 35, 14,   0 },
            { 147, 52208, 21, 10,   0 },
            { 148, 52208, 25, 10,   0 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 52201;
        private const int BossDropItemId = 344;
        private static readonly FDPosition BossPost = FDPosition.At(23, 17);

        /// <summary>The ones who hold their posts: (first id, last id) ranges, plus the boss.</summary>
        private static readonly int[,] GuardRanges = new int[,]
        {
            { 105, 108 },
            { 118, 120 },
            { 126, 134 },
            { 143, 148 },
        };

        /// <summary>The two waves out of the dark at the bottom corners, as (id, x, y, drop item).</summary>
        private const int ReinforcementDefinitionId = 52209;
        private static readonly int[,] FirstWave = new int[,]
        {
            { 201,  2, 42,   0 },
            { 202,  3, 41,   0 },
            { 203,  4, 42,   0 },
            { 204, 42, 42, 106 },
            { 205, 43, 41,   0 },
            { 206, 44, 42,   0 },
        };
        private static readonly int[,] SecondWave = new int[,]
        {
            { 207,  2, 42, 804 },
            { 208,  3, 41,   0 },
            { 209,  4, 42,   0 },
            { 210, 42, 42,   0 },
            { 211, 43, 41, 114 },
            { 212, 44, 42,   0 },
        };

        /// <summary>Sala (27) comes up from the bottom edge to the foot of the stairs.</summary>
        private const int SalaId = 27;
        private static readonly FDPosition SalaEntry = FDPosition.At(22, 49);
        private static readonly FDPosition SalaStop = FDPosition.At(22, 37);

        private const int XiyaId = 25;
        private const int XiyaRewardItemId = 263;

        public Chapter22(GameMain gameMain) : base(gameMain, 22)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 3, CreatureFaction.Npc, reinforcement);
            LoadTurnEvent(++eventId, 5, CreatureFaction.Npc, salaAppear);
            LoadTurnEvent(++eventId, 7, CreatureFaction.Npc, reinforcement2);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, XiyaId, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, BossId, bossDying);
        }

        private static bool IsGuard(int creatureId)
        {
            for (int i = 0; i < GuardRanges.GetLength(0); i++)
            {
                if (creatureId >= GuardRanges[i, 0] && creatureId <= GuardRanges[i, 1])
                {
                    return true;
                }
            }
            return false;
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                int creatureId = Enemies[i, 0];
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, creatureId, Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4],
                    IsGuard(creatureId) ? AITypes.AIType_Guard : (AITypes?)null);
            }
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost,
                BossDropItemId, AITypes.AIType_Guard);

            // Talking
            PushConversationsActivities(gameMain, 22, 1, 1, 11);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        private static void AddWave(GameMain gameMain, int[,] wave)
        {
            for (int i = 0; i < wave.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, wave[i, 0], ReinforcementDefinitionId,
                    FDPosition.At(wave[i, 1], wave[i, 2]), wave[i, 3]);
            }
        }

        private Action<GameMain> reinforcement = (gameMain) =>
        {
            AddWave(gameMain, FirstWave);

            // Talking
            PushConversationsActivities(gameMain, 22, 2, 1, 3);
        };

        private Action<GameMain> reinforcement2 = (gameMain) =>
        {
            AddWave(gameMain, SecondWave);
        };

        /// <summary>Sala walks in from the bottom edge and joins the party.</summary>
        private Action<GameMain> salaAppear = (gameMain) =>
        {
            AddCreatureToMap(gameMain, CreatureFaction.Friend, SalaId, SalaId, SalaEntry);
            gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(SalaId,
                FDMovePath.Create(SalaEntry, SalaStop)));

            // Talking
            PushConversationsActivities(gameMain, 22, 3, 1, 9);
        };

        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 22, 4, 1, 1);
        };

        /// <summary>The last enemy falls; Xiya is handed the transfer gun.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            FDCreature xiya = gameMain.gameMap.Map.GetCreatureById(XiyaId);
            if (xiya != null)
            {
                xiya.AddItem(XiyaRewardItemId);
            }

            // Talking
            PushConversationsActivities(gameMain, 22, 4, 2, 14);

            gameMain.OnGameWin();
        };
    }
}
