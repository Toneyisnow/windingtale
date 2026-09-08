using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 15 -- the lake and its bridges.
    ///
    /// Saikebangle (201) and his nine men (202..210) are cut off on the island in the
    /// middle of the lake, and fall back over the western bridge while the party comes
    /// up from the south-east corner. The enemy's forward companies (101..129) attack at
    /// once; the rear company with its captain (130..147, 199) waits at the north of
    /// the lake until turn 10. On turn 4 Saikebangle's men stop retreating and fight.
    /// On turn 7 four thieves (151..154) appear at the north edge, each carrying a
    /// treasure, and run for the north-west corner -- one that gets there is gone with
    /// its loot, but the first of them (151) has a star that is worth a scene if he is
    /// caught.
    ///
    /// Losing Sol (1) or Saikebangle ends the chapter. Kaili (10) has lines of her own
    /// when she is in the party, so both the opening and the ending pick their
    /// conversation by whether she is on the field. The chapter is won when the last
    /// enemy falls, and Saikebangle (17) joins the party where he stands.
    /// </summary>
    public class Chapter15 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 39, 46 },
            {  2, 38, 45 },
            {  3, 42, 45 },
            {  4, 43, 47 },
            {  5, 37, 46 },
            {  6, 39, 48 },
            {  7, 42, 49 },
            {  8, 40, 49 },
            {  9, 36, 49 },
            { 10, 37, 48 },
            { 11, 35, 47 },
            { 12, 44, 48 },
            { 13, 44, 46 },
            { 14, 41, 47 },
            { 15, 40, 44 },
            { 16, 36, 44 },
        };

        private const int FirstOptionalFriendId = 10;
        private const int KailiId = 10;

        /// <summary>The forward companies, as (id, definition, x, y, drop item): they attack at once.</summary>
        private static readonly int[,] ForwardEnemies = new int[,]
        {
            { 101, 51502, 42, 31,   0 },
            { 102, 51502, 44, 31,   0 },
            { 103, 51503, 40, 32,   0 },
            { 104, 51503, 46, 32,   0 },
            { 105, 51506, 39, 34,   0 },
            { 106, 51506, 41, 34,   0 },
            { 107, 51506, 45, 34,   0 },
            { 108, 51506, 47, 34, 902 },
            { 109, 51506, 35, 28,   0 },
            { 110, 51506, 37, 28,   0 },
            { 111, 51506, 35, 26,   0 },
            { 112, 51506, 37, 26,   0 },
            { 113, 51506, 34, 20,   0 },
            { 114, 51506, 36, 20,   0 },
            { 115, 51506, 34, 18,   0 },
            { 116, 51506, 36, 18,   0 },
            { 117, 51506, 38, 18, 103 },
            { 118, 51506, 40, 18,   0 },
            { 119, 51506, 38, 16,   0 },
            { 120, 51506, 40, 16,   0 },
            { 121, 51507, 42, 33,   0 },
            { 122, 51507, 44, 33,   0 },
            { 123, 51507, 36, 27,   0 },
            { 124, 51507, 35, 19,   0 },
            { 125, 51507, 39, 17, 902 },
            { 126, 51504, 38, 31,   0 },
            { 127, 51504, 49, 31,   0 },
            { 128, 51505, 40, 30, 103 },
            { 129, 51505, 47, 30,   0 },
        };

        /// <summary>The rear company, as (id, definition, x, y, drop item): it holds until turn 10.</summary>
        private static readonly int[,] RearEnemies = new int[,]
        {
            { 130, 51502, 29, 11,   0 },
            { 131, 51502, 35, 11, 105 },
            { 132, 51503, 31, 11, 103 },
            { 133, 51503, 33, 11,   0 },
            { 134, 51503, 30, 10,   0 },
            { 135, 51503, 34, 10,   0 },
            { 136, 51503, 31,  9,   0 },
            { 137, 51503, 33,  9,   0 },
            { 138, 51506, 32, 15, 901 },
            { 139, 51506, 31, 14,   0 },
            { 140, 51506, 33, 14,   0 },
            { 141, 51506, 30, 13,   0 },
            { 142, 51506, 32, 13, 103 },
            { 143, 51506, 34, 13,   0 },
            { 144, 51504, 29,  9, 803 },
            { 145, 51504, 35,  9,   0 },
            { 146, 51505, 30,  8,   0 },
            { 147, 51505, 34,  8,   0 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 51501;
        private const int BossDropItemId = 229;
        private static readonly FDPosition BossPost = FDPosition.At(32, 10);
        private const int RearAttackTurn = 10;

        /// <summary>Saikebangle, cut off on the island, and the friend he becomes.</summary>
        private const int SaikebangleNpcId = 201;
        private const int SaikebangleId = 17;
        private static readonly FDPosition SaikebanglePost = FDPosition.At(31, 22);

        /// <summary>His men, as (id, x, y). All of them fall back for the western bridge.</summary>
        private const int GuardDefinitionId = 51508;
        private static readonly int[,] Guards = new int[,]
        {
            { 202, 31, 20 },
            { 203, 30, 21 },
            { 204, 29, 22 },
            { 205, 30, 23 },
            { 206, 31, 24 },
            { 207, 29, 20 },
            { 208, 28, 21 },
            { 209, 28, 23 },
            { 210, 29, 24 },
        };
        private static readonly FDPosition GuardEscape = FDPosition.At(16, 22);
        private const int FirstGuardId = 201;
        private const int LastGuardId = 210;
        private const int GuardsFightTurn = 4;

        /// <summary>
        /// The thieves, as (id, x, y, item): each carries its item as well as dropping it,
        /// as the original had it, and all four run for the same corner.
        /// </summary>
        private const int ThiefDefinitionId = 51506;
        private static readonly int[,] Thieves = new int[,]
        {
            { 151, 34, 2, 118 },
            { 152, 35, 1, 809 },
            { 153, 34, 1, 113 },
            { 154, 33, 1, 806 },
        };
        private static readonly FDPosition ThiefEscape = FDPosition.At(5, 1);
        private const int StarThiefId = 151;
        private const int ThievesTurn = 7;

        public Chapter15(GameMain gameMain) : base(gameMain, 15)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, GuardsFightTurn, CreatureFaction.Npc, guardsFightBack);
            LoadTurnEvent(++eventId, ThievesTurn, CreatureFaction.Npc, thievesAppear);
            LoadTurnEvent(++eventId, RearAttackTurn, CreatureFaction.Npc, rearAttack);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, SaikebangleNpcId, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, StarThiefId, starThiefDead);
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                int thiefId = Thieves[i, 0];
                LoadReachPositionEvent(++eventId, thiefId, ThiefEscape,
                    (gameMain) => gameMain.gameMap.RemoveCreature(thiefId));
            }

            LoadDyingEvent(++eventId, BossId, bossDying);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < ForwardEnemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, ForwardEnemies[i, 0], ForwardEnemies[i, 1],
                    FDPosition.At(ForwardEnemies[i, 2], ForwardEnemies[i, 3]), ForwardEnemies[i, 4]);
            }

            for (int i = 0; i < RearEnemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, RearEnemies[i, 0], RearEnemies[i, 1],
                    FDPosition.At(RearEnemies[i, 2], RearEnemies[i, 3]), RearEnemies[i, 4], AITypes.AIType_StandBy);
            }
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost, BossDropItemId,
                AITypes.AIType_StandBy);

            AddCreatureToMap(gameMain, CreatureFaction.Npc, SaikebangleNpcId, SaikebangleId, SaikebanglePost);
            for (int i = 0; i < Guards.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Guards[i, 0], GuardDefinitionId,
                    FDPosition.At(Guards[i, 1], Guards[i, 2]));
            }
            for (int id = FirstGuardId; id <= LastGuardId; id++)
            {
                SetCreatureAiEscape(gameMain, id, GuardEscape);
            }

            // Talking
            if (gameMain.gameMap.Map.GetCreatureById(KailiId) != null)
            {
                PushConversationsActivities(gameMain, 15, 1, 1, 10);
            }
            else
            {
                PushConversationsActivities(gameMain, 15, 2, 1, 8);
            }

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 4: Saikebangle's men turn and fight.</summary>
        private Action<GameMain> guardsFightBack = (gameMain) =>
        {
            for (int id = FirstGuardId; id <= LastGuardId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }

            // Talking
            PushConversationsActivities(gameMain, 15, 2, 9, 9);
        };

        /// <summary>Turn 7: the thieves appear at the north edge and run for the corner.</summary>
        private Action<GameMain> thievesAppear = (gameMain) =>
        {
            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                int thiefId = Thieves[i, 0];
                FDCreature thief = AddCreatureToMap(gameMain, CreatureFaction.Enemy, thiefId, ThiefDefinitionId,
                    FDPosition.At(Thieves[i, 1], Thieves[i, 2]), Thieves[i, 3]);
                if (thief != null)
                {
                    thief.AddItem(Thieves[i, 3]);
                }
                SetCreatureAiEscape(gameMain, thiefId, ThiefEscape);
            }

            // Talking
            PushConversationsActivities(gameMain, 15, 3, 1, 8);
        };

        /// <summary>Turn 10: the rear company and its captain attack.</summary>
        private Action<GameMain> rearAttack = (gameMain) =>
        {
            for (int i = 0; i < RearEnemies.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, RearEnemies[i, 0], AITypes.AIType_Aggressive);
            }
            SetCreatureAiType(gameMain, BossId, AITypes.AIType_Aggressive);

            // Talking
            PushConversationsActivities(gameMain, 15, 2, 10, 10);
        };

        /// <summary>The thief with the star is caught.</summary>
        private Action<GameMain> starThiefDead = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 15, 4, 1, 5);
        };

        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 15, 5, 1, 2);
        };

        /// <summary>The last enemy falls: Saikebangle joins the party where he stands.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            if (gameMain.gameMap.Map.GetCreatureById(KailiId) != null)
            {
                PushConversationsActivities(gameMain, 15, 6, 1, 4);
            }
            else
            {
                PushConversationsActivities(gameMain, 15, 6, 5, 8);
            }
            PushConversationsActivities(gameMain, 15, 6, 9, 16);

            RecruitNpc(gameMain, SaikebangleNpcId, SaikebangleId, SaikebanglePost);

            gameMain.OnGameWin();
        };
    }
}
