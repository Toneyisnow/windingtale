using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 19 -- the forest of red and blue pines.
    ///
    /// The party comes in at the bottom of a long wooded valley. The enemy's front
    /// company (101..116) is waiting just up the path and attacks at once; the middle
    /// companies (117..127, 144) hold until turn 4 and the rear ones (128..143) with
    /// the commander (199) until turn 8. Three thieves (151..153) stand in the middle
    /// of the map and, from turn 4, go for the three chests and then run for the edges
    /// -- one that reaches its edge is gone with its loot. On turn 6 Bana Long (22)
    /// appears at the south-west corner and joins the party. Losing Sol (1) ends the
    /// chapter; it is won when the last enemy falls or flees.
    ///
    /// Departures from the original: it numbered two different enemies 119, so the
    /// second (at (4, 20)) is 144 here and joins the turn-4 attack with the rest of
    /// its group; it also added Bana Long both at the start and again on turn 6, walking
    /// in from (4, 45), a tile five rows off the bottom of this 25 x 40 map -- here he
    /// appears once, on turn 6, at the tile the walk ended on. And friends 17..21,
    /// which the original never settled, take free tiles in the formation when the
    /// party carries them, since the save keeps only friends on the map.
    /// </summary>
    public class Chapter19 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 11, 37 },
            {  2, 13, 37 },
            {  3, 15, 37 },
            {  4, 12, 38 },
            {  5, 14, 38 },
            {  6, 16, 38 },
            {  7, 10, 38 },
            {  8, 11, 39 },
            {  9, 13, 39 },
            { 10, 15, 39 },
            { 11, 14, 40 },
            { 12, 12, 40 },
            { 13, 10, 40 },
            { 14, 16, 40 },
            { 15, 17, 39 },
            { 16,  9, 39 },
            { 17, 13, 38 },
            { 18, 12, 39 },
            { 19, 14, 39 },
            { 20, 13, 40 },
            { 21, 12, 37 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The front company, as (id, definition, x, y, drop item): it attacks at once.</summary>
        private static readonly int[,] FrontEnemies = new int[,]
        {
            { 101, 51902, 11, 28,   0 },
            { 102, 51902, 20, 29,   0 },
            { 103, 51908,  9, 28,   0 },
            { 104, 51908, 22, 29,   0 },
            { 105, 51903,  8, 29,   0 },
            { 106, 51903, 23, 30, 804 },
            { 107, 51904, 10, 31,   0 },
            { 108, 51904, 21, 32,   0 },
            { 109, 51905,  9, 30,   0 },
            { 110, 51905, 10, 29, 103 },
            { 111, 51905, 11, 30, 116 },
            { 112, 51905, 20, 31,   0 },
            { 113, 51905, 21, 30,   0 },
            { 114, 51905, 22, 31,   0 },
            { 115, 51907, 11, 29,   0 },
            { 116, 51907, 19, 30, 804 },
        };

        /// <summary>The middle companies, guarding their posts until turn 4. 144 was the original's second 119.</summary>
        private static readonly int[,] MiddleEnemies = new int[,]
        {
            { 117, 51902,  5, 21,   0 },
            { 118, 51902, 14, 15,   0 },
            { 119, 51908, 21, 22,   0 },
            { 144, 51904,  4, 20,   0 },
            { 120, 51904,  5, 19,   0 },
            { 121, 51904,  6, 20,   0 },
            { 122, 51904, 20, 21,   0 },
            { 123, 51904, 21, 20, 114 },
            { 124, 51904, 22, 21,   0 },
            { 125, 51905, 13, 14, 103 },
            { 126, 51905, 14, 13,   0 },
            { 127, 51905, 15, 14,   0 },
        };

        /// <summary>The rear companies, guarding their posts until turn 8.</summary>
        private static readonly int[,] RearEnemies = new int[,]
        {
            { 128, 51902, 19, 11,   0 },
            { 129, 51902, 17,  6, 111 },
            { 130, 51908,  6,  9,   0 },
            { 131, 51908, 24, 11,   0 },
            { 132, 51908, 24,  5,   0 },
            { 133, 51903, 20,  7,   0 },
            { 134, 51903, 22,  7,   0 },
            { 135, 51903, 20,  9,   0 },
            { 136, 51903, 22,  9,   0 },
            { 137, 51905,  5,  8, 903 },
            { 138, 51905,  6,  7,   0 },
            { 139, 51905,  7,  8,   0 },
            { 140, 51905, 21,  5,   0 },
            { 141, 51905, 21, 11,   0 },
            { 142, 51907, 18,  8, 103 },
            { 143, 51907, 24,  8,   0 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 51901;
        private static readonly FDPosition BossPost = FDPosition.At(21, 8);

        /// <summary>
        /// The thieves, as (id, x, y, drop item, chest x, chest y, escape x, escape y).
        /// They guard their posts until turn 4, then go for their chests.
        /// </summary>
        private const int ThiefDefinitionId = 51906;
        private static readonly int[,] Thieves = new int[,]
        {
            { 151, 10, 18, 103, 22, 24, 25, 5 },
            { 152, 11, 20,   0, 14,  2, 25, 4 },
            { 153, 12, 18,   0,  1, 12,  6, 2 },
        };

        private const int MiddleAttackTurn = 4;
        private const int BanaTurn = 6;
        private const int RearAttackTurn = 8;

        /// <summary>Bana Long (22), arriving at the south-west corner on turn 6.</summary>
        private const int BanaId = 22;
        private static readonly FDPosition BanaStop = FDPosition.At(4, 40);

        public Chapter19(GameMain gameMain) : base(gameMain, 19)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, MiddleAttackTurn, CreatureFaction.Npc, middleAttack);
            LoadTurnEvent(++eventId, BanaTurn, CreatureFaction.Npc, banaAppear);
            LoadTurnEvent(++eventId, RearAttackTurn, CreatureFaction.Npc, rearAttack);

            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                int thiefId = Thieves[i, 0];
                LoadReachPositionEvent(++eventId, thiefId, FDPosition.At(Thieves[i, 6], Thieves[i, 7]),
                    (gameMain) => gameMain.gameMap.RemoveCreature(thiefId));
            }

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, BossId, bossDying);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < FrontEnemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, FrontEnemies[i, 0], FrontEnemies[i, 1],
                    FDPosition.At(FrontEnemies[i, 2], FrontEnemies[i, 3]), FrontEnemies[i, 4]);
            }
            for (int i = 0; i < MiddleEnemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, MiddleEnemies[i, 0], MiddleEnemies[i, 1],
                    FDPosition.At(MiddleEnemies[i, 2], MiddleEnemies[i, 3]), MiddleEnemies[i, 4], AITypes.AIType_Guard);
            }
            for (int i = 0; i < RearEnemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, RearEnemies[i, 0], RearEnemies[i, 1],
                    FDPosition.At(RearEnemies[i, 2], RearEnemies[i, 3]), RearEnemies[i, 4], AITypes.AIType_Guard);
            }
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost, 0, AITypes.AIType_Guard);

            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Thieves[i, 0], ThiefDefinitionId,
                    FDPosition.At(Thieves[i, 1], Thieves[i, 2]), Thieves[i, 3], AITypes.AIType_Guard);
            }

            // Talking
            PushConversationsActivities(gameMain, 19, 1, 1, 8);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 4: the thieves go for the chests and the middle companies attack.</summary>
        private Action<GameMain> middleAttack = (gameMain) =>
        {
            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                SetCreatureAiTreasure(gameMain, Thieves[i, 0],
                    FDPosition.At(Thieves[i, 4], Thieves[i, 5]),
                    FDPosition.At(Thieves[i, 6], Thieves[i, 7]));
            }

            for (int i = 0; i < MiddleEnemies.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, MiddleEnemies[i, 0], AITypes.AIType_Aggressive);
            }
        };

        /// <summary>Turn 6: Bana Long appears at the south-west corner and joins the party.</summary>
        private Action<GameMain> banaAppear = (gameMain) =>
        {
            AddCreatureToMap(gameMain, CreatureFaction.Friend, BanaId, BanaId, BanaStop);

            // Talking
            PushConversationsActivities(gameMain, 19, 1, 9, 13);
        };

        /// <summary>Turn 8: the rear companies and the commander attack.</summary>
        private Action<GameMain> rearAttack = (gameMain) =>
        {
            for (int i = 0; i < RearEnemies.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, RearEnemies[i, 0], AITypes.AIType_Aggressive);
            }
            SetCreatureAiType(gameMain, BossId, AITypes.AIType_Aggressive);
        };

        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 19, 2, 1, 1);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 19, 3, 1, 10);

            gameMain.OnGameWin();
        };
    }
}
