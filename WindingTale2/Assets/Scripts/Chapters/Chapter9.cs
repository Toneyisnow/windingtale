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
    /// Chapter 9 -- the knight's choice.
    ///
    /// The road to the capital, lined with stone pillars and a notice board. The royal
    /// guard under captain Laiting (199) is drawn up across it: the king has ordered the
    /// princess's "bandits" arrested, and Laiting will carry the order out even against
    /// Xilia. The guard's centre holds on AIType_StandBy until turn 7, then everything
    /// attacks.
    ///
    /// When Laiting falls he is not lost: on the blow that kills him he learns from the
    /// princess that the order came from the minister Grey, not the king, and joins the
    /// party as friend 13 with 1 HP where he stood -- and Grey's own men (201..213)
    /// arrive from the edges of the map to arrest him. The chapter ends when the last
    /// enemy falls; a guard runs in with news that Grey has been murdered, and Laiting
    /// goes ahead into the castle alone, so he leaves the party after the battle.
    /// </summary>
    public class Chapter9 : ChapterEvents
    {
        /// <summary>
        /// Where the party lines up, in creature id order 1..11. Kaili (10) is only in
        /// the party if she was saved in chapter 7 and Rona (11) only if chapter 8 was
        /// played, so those two are settled only when the record carries them -- the
        /// original's settleFriend did nothing for an empty slot.
        /// </summary>
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 10, 34 },
            {  2, 12, 34 },
            {  3, 14, 34 },
            {  4, 16, 34 },
            {  5, 15, 35 },
            {  6, 13, 35 },
            {  7, 11, 35 },
            {  8, 10, 36 },
            {  9, 12, 36 },
            { 10, 14, 36 },
            { 11, 16, 36 },
        };

        /// <summary>The first friend id that is optional: everyone from here on may be missing.</summary>
        private const int FirstOptionalFriendId = 10;

        /// <summary>
        /// The royal guard, as (id, definition, x, y, drop item), in the order the original
        /// added them. Four pairs share a tile (101/112, 102/111, 103/113, 104/114): the
        /// original stacked them the same way.
        /// </summary>
        private static readonly int[,] Guard = new int[,]
        {
            { 101, 50908, 14, 22,   0 },
            { 102, 50908, 12, 22,   0 },
            { 103, 50908,  8, 19, 116 },
            { 104, 50908, 18, 19,   0 },

            { 105, 50903, 12, 19,   0 },
            { 106, 50903, 14, 19,   0 },
            { 107, 50903, 10, 21, 802 },
            { 108, 50903, 16, 21,   0 },
            { 109, 50903, 12, 24,   0 },
            { 110, 50903, 14, 24,   0 },

            { 111, 50908, 12, 22,   0 },
            { 112, 50908, 14, 22, 116 },
            { 113, 50908,  8, 19,   0 },
            { 114, 50908, 18, 19, 102 },

            { 115, 50905, 11, 23,   0 },
            { 116, 50905, 15, 23, 105 },
            { 117, 50905,  7, 21,   0 },
            { 118, 50905, 19, 21,   0 },
            { 119, 50906, 11, 17,   0 },
            { 120, 50906, 15, 17,   0 },

            { 121, 50907, 18, 23, 801 },
            { 122, 50907,  8, 23,   0 },
            { 123, 50907, 12, 18,   0 },
            { 124, 50907, 14, 18,   0 },

            { 125, 50901, 12, 16,   0 },
            { 126, 50901, 14, 16,   0 },
        };

        /// <summary>The guards that hold their ground until turn 7, the captain among them.</summary>
        private static readonly int[] StandByIds = new int[] { 103, 104, 113, 114, 119, 120, 125, 126, CaptainId };

        /// <summary>Laiting, captain of the royal guard, standing behind the notice board.</summary>
        private const int CaptainId = 199;
        private const int CaptainDefinitionId = 50910;
        private static readonly FDPosition CaptainPost = FDPosition.At(13, 16);

        /// <summary>Laiting as a party member, for the rest of this battle only.</summary>
        private const int LaitingFriendId = 13;
        private const int LaitingFriendDefinitionId = 13;
        private const int LaitingJoinHp = 1;

        /// <summary>The turn the guard's centre stops holding and attacks.</summary>
        private const int AllAttackTurn = 7;

        /// <summary>
        /// Grey's men, who arrive when Laiting changes sides, as (id, definition, x, y):
        /// two riders on the mid-road, six along the map edges, and their officer (209)
        /// with four guards at the top of the road.
        /// </summary>
        private static readonly int[,] GreysMen = new int[,]
        {
            { 201, 50909,  2, 16 },
            { 202, 50909, 24, 16 },

            { 203, 50904,  1,  6 },
            { 204, 50904, 25,  6 },
            { 205, 50904,  1, 16 },
            { 206, 50904, 25, 16 },
            { 207, 50904,  1, 24 },
            { 208, 50904, 25, 24 },

            { 209, 50902, 13,  2 },
            { 210, 50911, 12,  1 },
            { 211, 50911, 14,  1 },
            { 212, 50911, 12,  3 },
            { 213, 50911, 14,  3 },
        };

        /// <summary>The guard who runs in from the castle with the news, once the road is clear.</summary>
        private const int MessengerId = 301;
        private const int MessengerDefinitionId = 50911;
        private static readonly FDPosition MessengerEntry = FDPosition.At(13, 1);

        /// <summary>
        /// Where the opening cutscene leaves the cursor. The original asked for (16, 40)
        /// on a map 36 rows tall; this is that point clamped onto the board.
        /// </summary>
        private static readonly FDPosition OpeningCursor = FDPosition.At(16, 36);

        public Chapter9(GameMain gameMain) : base(gameMain, 9)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:7" for the charge, which fired once the last friend
            // had acted in turn 7, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, AllAttackTurn, CreatureFaction.Npc, allAttack);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, CaptainId, captainDying);
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

            for (int i = 0; i < Guard.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Guard[i, 0], Guard[i, 1],
                    FDPosition.At(Guard[i, 2], Guard[i, 3]), Guard[i, 4]);
            }

            AddCreatureToMap(gameMain, CreatureFaction.Enemy, CaptainId, CaptainDefinitionId, CaptainPost);

            // The centre of the line, and the captain, hold their ground.
            foreach (int id in StandByIds)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_StandBy);
            }

            gameMain.PushActivity(new SlideCursorActivity(OpeningCursor.X, OpeningCursor.Y));

            // Talking
            PushConversationsActivities(gameMain, 9, 1, 1, 7);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 7: the whole guard attacks.</summary>
        private Action<GameMain> allAttack = (gameMain) =>
        {
            foreach (int id in StandByIds)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
        };

        /// <summary>
        /// The blow that kills the captain. Laiting changes sides where he stands -- a
        /// party member at 1 HP, the original's hpCurrent = 1 -- and Grey's men arrive to
        /// arrest him, their officer (209) speaking in the conversation that follows.
        /// </summary>
        private Action<GameMain> captainDying = (gameMain) =>
        {
            FDCreature captain = gameMain.gameMap.Map.GetCreatureById(CaptainId);
            FDPosition position = captain != null ? captain.Position : CaptainPost;

            FDCreature laiting = AddCreatureToMap(gameMain, CreatureFaction.Friend,
                LaitingFriendId, LaitingFriendDefinitionId, position);
            if (laiting != null)
            {
                laiting.Hp = LaitingJoinHp;
            }

            for (int i = 0; i < GreysMen.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, GreysMen[i, 0], GreysMen[i, 1],
                    FDPosition.At(GreysMen[i, 2], GreysMen[i, 3]));
            }

            // Talking
            PushConversationsActivities(gameMain, 9, 2, 1, 21);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            AddCreatureToMap(gameMain, CreatureFaction.Npc, MessengerId, MessengerDefinitionId, MessengerEntry);

            // Talking
            PushConversationsActivities(gameMain, 9, 3, 1, 5);

            // Laiting leaves in AdjustFriendsAfterWon, after the closing lines he speaks
            // in. OnGameWin queues itself behind them, so they play out first.
            gameMain.OnGameWin();
        };

        internal override void AdjustFriendsAfterWon()
        {
            // Laiting goes ahead into the castle alone: he is not part of the party that
            // walks on. The original's removeLaiting ran here, after conversation 3. He
            // is taken out of the fallen as well, or a Laiting who died in the battle
            // would follow the party to the church waiting to be revived.
            gameMain.gameMap.RemoveCreature(LaitingFriendId);
            gameMain.gameMap.Map.DeadCreatures.RemoveAll(creature => creature.Id == LaitingFriendId);
        }

        /// <summary>
        /// Whether the party that walked in from the village carries this creature. A
        /// battle started with no party at all (a New Game jump straight to the chapter)
        /// carries nobody, and then only the mandatory friends take the field.
        /// </summary>
        private static bool PartyCarries(GameMain gameMain, int creatureId)
        {
            List<CreatureMapRecord> party = gameMain.PartyRecord?.Friends;
            return party != null && party.Exists(friend => friend.Id == creatureId);
        }
    }
}
