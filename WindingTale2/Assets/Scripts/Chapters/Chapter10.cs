using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 10 -- the battle in the cave.
    ///
    /// The cave behind the waterfall where the kidnapped king, Kanaan III (901), is
    /// held, with the priestess Sophia (12) beside him -- both bound and unable to move
    /// for the whole battle, and out of the enemy's notice until near the end. Laiting
    /// (13) is back in the party from the start. The enemy is drawn up in three lines
    /// up the cave: the front line attacks at once, its flanks hold until turn 6, the
    /// middle line until turn 9, and the rear line with the boss (199) until turn 18 --
    /// when the boss orders the king killed and the two captives become fair game.
    ///
    /// Eight royal guards (201..208) arrive as allies on turn 5 where the party
    /// started. Losing Sol (1), Sophia or the king ends the chapter; the boss's dying
    /// words are conversation 3. When the last enemy falls Sophia is freed and joins
    /// the party where she stood, and the closing conversation with the king lets her
    /// and the princess leave with the party.
    /// </summary>
    public class Chapter10 : ChapterEvents
    {
        /// <summary>
        /// Where the party lines up, in creature id order 1..11. Kaili (10) is only in
        /// the party if she was saved in chapter 7 and Rona (11) only if chapter 8 was
        /// played, so those two are settled only when the record carries them -- the
        /// original's settleFriend did nothing for an empty slot.
        /// </summary>
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 18, 41 },
            {  2, 16, 43 },
            {  3, 14, 43 },
            {  4, 15, 42 },
            {  5, 17, 42 },
            {  6, 14, 41 },
            {  7, 18, 43 },
            {  8, 15, 44 },
            {  9, 17, 44 },
            { 10, 14, 45 },
            { 11, 18, 45 },
        };

        /// <summary>The first friend id that is optional: everyone from here on may be missing.</summary>
        private const int FirstOptionalFriendId = 10;

        /// <summary>Laiting, captain of the royal guard, with the party from the start of this chapter.</summary>
        private const int LaitingId = 13;
        private static readonly FDPosition LaitingEntry = FDPosition.At(16, 41);

        /// <summary>
        /// The enemy, as (id, definition, x, y, drop item), in the order the original
        /// added them: the front line across the lower cave (101..118), the middle line
        /// (119..126) and the rear line around the boss (127..138).
        /// </summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 51004, 14, 33,   0 },
            { 102, 51004, 18, 33,   0 },
            { 103, 51004,  8, 26,   0 },
            { 104, 51004, 24, 26,   0 },
            { 105, 51002, 15, 34, 102 },
            { 106, 51002, 17, 34,   0 },
            { 107, 51002,  9, 27,   0 },
            { 108, 51002, 23, 27, 111 },
            { 109, 51002,  5, 23,   0 },
            { 110, 51002,  7, 23,   0 },
            { 111, 51002, 25, 23, 102 },
            { 112, 51002, 27, 23,   0 },
            { 113, 51003, 13, 30,   0 },
            { 114, 51003, 19, 30,   0 },
            { 115, 51003,  6, 22,   0 },
            { 116, 51003, 26, 22, 102 },
            { 117, 51005, 14, 31,   0 },
            { 118, 51005, 18, 31,   0 },

            { 119, 51004, 13, 15,   0 },
            { 120, 51004, 19, 15, 102 },
            { 121, 51002, 15, 16,   0 },
            { 122, 51002, 17, 16,   0 },
            { 123, 51003, 14, 13,   0 },
            { 124, 51003, 18, 13,   0 },
            { 125, 51005, 13, 13,   0 },
            { 126, 51005, 19, 13, 103 },

            { 127, 51004, 15,  7, 903 },
            { 128, 51004, 17,  7, 259 },
            { 129, 51002,  9, 10,   0 },
            { 130, 51002, 23, 10,   0 },
            { 131, 51002, 13,  6,   0 },
            { 132, 51002, 19,  6,   0 },
            { 133, 51002, 11,  5,   0 },
            { 134, 51002, 21,  5,   0 },
            { 135, 51003, 13,  4,   0 },
            { 136, 51003, 19,  4,   0 },
            { 137, 51005, 14,  4,   0 },
            { 138, 51005, 18,  4, 103 },
        };

        /// <summary>The boss, standing at the top of the cave.</summary>
        private const int BossId = 199;
        private const int BossDefinitionId = 51001;
        private static readonly FDPosition BossPost = FDPosition.At(16, 5);

        /// <summary>The front line's flanks, which hold their ground until turn 6.</summary>
        private static readonly int[] FrontFlankIds = new int[] { 103, 104, 109, 110, 111, 112, 115, 116 };

        /// <summary>The middle line (119..126) attacks on turn 9, the rear line (127..138) with the boss on turn 18.</summary>
        private const int FirstMiddleLineId = 119;
        private const int LastMiddleLineId = 126;
        private const int FirstRearLineId = 127;
        private const int LastRearLineId = 138;

        private const int ReinforcementTurn = 5;
        private const int FrontFlankAttackTurn = 6;
        private const int MiddleLineAttackTurn = 9;
        private const int RearLineAttackTurn = 18;

        /// <summary>The captives: the king (901) and Sophia (12), held on either side of the upper cave.</summary>
        private const int KingId = 901;
        private const int KingDefinitionId = 901;
        private static readonly FDPosition KingPost = FDPosition.At(25, 9);
        private const int SophiaId = 12;
        private const int SophiaDefinitionId = 12;
        private static readonly FDPosition SophiaPost = FDPosition.At(7, 9);

        /// <summary>
        /// The royal guards who arrive on turn 5, as (id, x, y): eight of them, where the
        /// party stood at the start.
        /// </summary>
        private const int GuardDefinitionId = 51006;
        private static readonly int[,] Guards = new int[,]
        {
            { 201, 17, 41 },
            { 202, 15, 41 },
            { 203, 16, 42 },
            { 204, 14, 42 },
            { 205, 17, 43 },
            { 206, 15, 43 },
            { 207, 16, 44 },
            { 208, 14, 44 },
        };

        public Chapter10(GameMain gameMain) : base(gameMain, 10)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, ReinforcementTurn, CreatureFaction.Npc, reinforcement);
            LoadTurnEvent(++eventId, FrontFlankAttackTurn, CreatureFaction.Npc, frontFlankAttack);
            LoadTurnEvent(++eventId, MiddleLineAttackTurn, CreatureFaction.Npc, middleLineAttack);
            LoadTurnEvent(++eventId, RearLineAttackTurn, CreatureFaction.Npc, rearLineAttack);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, SophiaId, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, KingId, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, BossId, bossDying);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            for (int i = 0; i < PartyEntry.GetLength(0); i++)
            {
                int creatureId = PartyEntry[i, 0];
                if (creatureId >= FirstOptionalFriendId && !PartyCarries(gameMain, creatureId))
                {
                    continue;
                }

                AddCreatureToMap(gameMain, CreatureFaction.Friend, creatureId, creatureId,
                    FDPosition.At(PartyEntry[i, 1], PartyEntry[i, 2]));
            }

            // Laiting went ahead into the castle alone after chapter 9 and is not in the
            // record; the original added him fresh here, as does this.
            AddCreatureToMap(gameMain, CreatureFaction.Friend, LaitingId, LaitingId, LaitingEntry);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4]);
            }

            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost);

            // The middle and rear lines, the front line's flanks and the boss hold.
            for (int id = FirstMiddleLineId; id <= LastRearLineId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_StandBy);
            }
            foreach (int id in FrontFlankIds)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_StandBy);
            }
            SetCreatureAiType(gameMain, BossId, AITypes.AIType_StandBy);

            // The captives: bound, so they cannot move or act, and beneath the enemy's
            // notice. The original froze them for 255 turns -- for the whole battle --
            // and the Frozen effect here wears off only by magic, so it is the same.
            AddCaptive(gameMain, KingId, KingDefinitionId, KingPost);
            AddCaptive(gameMain, SophiaId, SophiaDefinitionId, SophiaPost);

            // Talking
            PushConversationsActivities(gameMain, 10, 1, 1, 12);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 5: the royal guard arrives at the cave mouth.</summary>
        private Action<GameMain> reinforcement = (gameMain) =>
        {
            for (int i = 0; i < Guards.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Guards[i, 0], GuardDefinitionId,
                    FDPosition.At(Guards[i, 1], Guards[i, 2]));
            }

            // Talking
            PushConversationsActivities(gameMain, 10, 2, 1, 3);
        };

        /// <summary>Turn 6: the front line's flanks join the attack.</summary>
        private Action<GameMain> frontFlankAttack = (gameMain) =>
        {
            foreach (int id in FrontFlankIds)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
        };

        /// <summary>Turn 9: the middle line attacks.</summary>
        private Action<GameMain> middleLineAttack = (gameMain) =>
        {
            for (int id = FirstMiddleLineId; id <= LastMiddleLineId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
        };

        /// <summary>
        /// Turn 18: the rear line and the boss attack, and the boss orders the king
        /// killed -- the captives stop being beneath notice, so the enemy can now go
        /// for them (and losing either ends the chapter). They stay frozen.
        /// </summary>
        private Action<GameMain> rearLineAttack = (gameMain) =>
        {
            for (int id = FirstRearLineId; id <= LastRearLineId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
            SetCreatureAiType(gameMain, BossId, AITypes.AIType_Aggressive);

            SetCreatureAiType(gameMain, KingId, AITypes.AIType_StandBy);
            SetCreatureAiType(gameMain, SophiaId, AITypes.AIType_StandBy);

            // Talking
            PushConversationsActivities(gameMain, 10, 5, 1, 1);
        };

        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 10, 3, 1, 5);
        };

        /// <summary>
        /// The last enemy falls: Sophia is freed and joins the party where she stood.
        /// Only Friend-faction creatures are carried on to the next chapter, and an NPC
        /// cannot change faction in place, so the captive is swapped for a friend.
        /// </summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            FDCreature captive = gameMain.gameMap.Map.GetCreatureById(SophiaId);
            FDPosition position = captive != null ? captive.Position : SophiaPost;

            gameMain.gameMap.RemoveCreature(SophiaId);
            AddCreatureToMap(gameMain, CreatureFaction.Friend, SophiaId, SophiaDefinitionId, position);

            // Talking
            PushConversationsActivities(gameMain, 10, 4, 1, 35);

            gameMain.OnGameWin();
        };

        /// <summary>
        /// Puts one of the captives on the map: an NPC the enemy takes no notice of,
        /// frozen so it neither moves nor acts.
        /// </summary>
        private static void AddCaptive(GameMain gameMain, int creatureId, int definitionId, FDPosition position)
        {
            FDCreature captive = AddCreatureToMap(gameMain, CreatureFaction.Npc, creatureId, definitionId,
                position, 0, AITypes.AIType_UnNoticable);
            if (captive != null)
            {
                captive.Effects.Add(CreatureEffects.Frozen);
            }
        }

    }
}
