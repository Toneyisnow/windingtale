using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 16 -- the frozen lakes.
    ///
    /// Midi (18) and her eight guards (201..208) are surrounded on the ice in the
    /// middle of the map. The enemy's northern companies (101..124) are on them at
    /// once; the southern ones (125..149) wait until turn 7. Two thieves (150, 151)
    /// go for the chests at the west and north and then run for the south-east corner,
    /// and are gone if they reach it. Losing Sol (1) or Midi ends the chapter.
    ///
    /// The ending depends on how the battle went. Midi joins the party only if Sol's
    /// HP has reached 320, at least five of her party are still standing, and the
    /// battle is over by turn 15; otherwise she takes her leave. The original numbered
    /// all eight guards 201; here they are 201..208.
    /// </summary>
    public class Chapter16 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 13, 8 },
            {  2, 17, 5 },
            {  3, 14, 5 },
            {  4, 13, 4 },
            {  5, 10, 6 },
            {  6, 11, 8 },
            {  7, 17, 7 },
            {  8, 16, 4 },
            {  9, 12, 2 },
            { 10,  9, 4 },
            { 11, 11, 4 },
            { 12, 12, 6 },
            { 13, 15, 7 },
            { 14, 16, 8 },
            { 15, 10, 2 },
            { 16, 14, 2 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The northern companies, as (id, definition, x, y, drop item): they attack at once.</summary>
        private static readonly int[,] NorthEnemies = new int[,]
        {
            { 101, 51601, 17, 17,   0 },
            { 102, 51601, 20, 14,   0 },
            { 103, 51601, 32, 12,   0 },
            { 104, 51603, 17, 15,   0 },
            { 105, 51603, 18, 14,   0 },
            { 106, 51603, 19, 13,   0 },
            { 107, 51603, 31, 11,   0 },
            { 108, 51603, 30, 12,   0 },
            { 109, 51603, 31, 13,   0 },
            { 110, 51603, 33, 13,   0 },
            { 111, 51602, 16, 16, 102 },
            { 112, 51602, 21, 13,   0 },
            { 113, 51604, 17, 13,   0 },
            { 114, 51604, 16, 14,   0 },
            { 115, 51604, 19, 17,   0 },
            { 116, 51604, 20, 16,   0 },
            { 117, 51604, 29, 11,   0 },
            { 118, 51604, 29, 13,   0 },
            { 119, 51604, 30, 14,   0 },
            { 120, 51604, 32, 14,   0 },
            { 121, 51605, 18, 16,   0 },
            { 122, 51605, 19, 15,   0 },
            { 123, 51605, 30, 10, 904 },
            { 124, 51605, 34, 14,   0 },
        };

        /// <summary>The southern companies, as (id, definition, x, y, drop item): they hold until turn 7.</summary>
        private static readonly int[,] SouthEnemies = new int[,]
        {
            { 125, 51601, 23, 32, 103 },
            { 126, 51601, 30, 35,   0 },
            { 127, 51601, 35, 35,   0 },
            { 128, 51606, 31, 37, 803 },
            { 129, 51606, 33, 34,   0 },
            { 130, 51603, 22, 33,   0 },
            { 131, 51603, 23, 34,   0 },
            { 132, 51603, 24, 33,   0 },
            { 133, 51602, 32, 35,   0 },
            { 134, 51602, 33, 36, 903 },
            { 135, 51604, 22, 31,   0 },
            { 136, 51604, 21, 32,   0 },
            { 137, 51604, 24, 31,   0 },
            { 138, 51604, 25, 32, 108 },
            { 139, 51604, 32, 31,   0 },
            { 140, 51604, 32, 32,   0 },
            { 141, 51604, 30, 32,   0 },
            { 142, 51604, 31, 33,   0 },
            { 143, 51604, 30, 34, 111 },
            { 144, 51604, 29, 34, 803 },
            { 145, 51605, 23, 30,   0 },
            { 146, 51605, 20, 33,   0 },
            { 147, 51605, 26, 33,   0 },
            { 148, 51605, 33, 33,   0 },
            { 149, 51605, 31, 36,   0 },
        };
        private const int SouthAttackTurn = 7;

        /// <summary>The thieves, as (id, x, y, chest x, chest y), and the corner they all run for.</summary>
        private const int ThiefDefinitionId = 51608;
        private static readonly int[,] Thieves = new int[,]
        {
            { 150,  5, 24, 18, 37 },
            { 151, 32,  6, 31, 29 },
        };
        private static readonly FDPosition ThiefEscape = FDPosition.At(40, 40);

        /// <summary>Midi and her guards, as (id, x, y) for the guards.</summary>
        private const int MidiId = 18;
        private static readonly FDPosition MidiPost = FDPosition.At(23, 21);
        private const int GuardDefinitionId = 51607;
        private static readonly int[,] Guards = new int[,]
        {
            { 201, 22, 21 },
            { 202, 24, 21 },
            { 203, 23, 20 },
            { 204, 23, 22 },
            { 205, 22, 20 },
            { 206, 24, 20 },
            { 207, 22, 22 },
            { 208, 24, 22 },
        };

        /// <summary>What it takes for Midi to join: Sol's HP, her party's survivors, and the turn.</summary>
        private const int RequiredSolHpMax = 320;
        private const int RequiredNpcCount = 5;
        private const int LastTurnToJoin = 15;

        public Chapter16(GameMain gameMain) : base(gameMain, 16)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:7", which fired once the last friend had acted in turn
            // 7, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, SouthAttackTurn, CreatureFaction.Npc, southAttack);

            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                int thiefId = Thieves[i, 0];
                LoadReachPositionEvent(++eventId, thiefId, ThiefEscape,
                    (gameMain) => gameMain.gameMap.RemoveCreature(thiefId));
            }

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, MidiId, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < NorthEnemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, NorthEnemies[i, 0], NorthEnemies[i, 1],
                    FDPosition.At(NorthEnemies[i, 2], NorthEnemies[i, 3]), NorthEnemies[i, 4]);
            }

            for (int i = 0; i < SouthEnemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, SouthEnemies[i, 0], SouthEnemies[i, 1],
                    FDPosition.At(SouthEnemies[i, 2], SouthEnemies[i, 3]), SouthEnemies[i, 4], AITypes.AIType_StandBy);
            }

            for (int i = 0; i < Thieves.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Thieves[i, 0], ThiefDefinitionId,
                    FDPosition.At(Thieves[i, 1], Thieves[i, 2]));
                SetCreatureAiTreasure(gameMain, Thieves[i, 0], FDPosition.At(Thieves[i, 3], Thieves[i, 4]), ThiefEscape);
            }

            AddCreatureToMap(gameMain, CreatureFaction.Npc, MidiId, MidiId, MidiPost);
            for (int i = 0; i < Guards.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Guards[i, 0], GuardDefinitionId,
                    FDPosition.At(Guards[i, 1], Guards[i, 2]));
            }

            // Talking
            PushConversationsActivities(gameMain, 16, 1, 1, 16);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 7: the southern companies attack.</summary>
        private Action<GameMain> southAttack = (gameMain) =>
        {
            for (int i = 0; i < SouthEnemies.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, SouthEnemies[i, 0], AITypes.AIType_Aggressive);
            }
        };

        /// <summary>
        /// The last enemy falls. Midi joins if the battle went well enough -- Sol strong
        /// enough, enough of her guards alive, and not too many turns taken -- and
        /// otherwise says goodbye.
        /// </summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            FDCreature sol = gameMain.gameMap.Map.GetCreatureById(1);
            bool joins = sol != null && sol.HpMax >= RequiredSolHpMax
                && gameMain.gameMap.Map.Npcs.Count >= RequiredNpcCount
                && gameMain.gameMap.Map.TurnNo <= LastTurnToJoin;

            if (joins)
            {
                // Talking
                PushConversationsActivities(gameMain, 16, 2, 1, 13);

                RecruitNpc(gameMain, MidiId, MidiId, MidiPost);
            }
            else
            {
                // Talking
                PushConversationsActivities(gameMain, 16, 3, 1, 8);
            }

            gameMain.OnGameWin();
        };
    }
}
