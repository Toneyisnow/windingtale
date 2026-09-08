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
    /// Chapter 8 -- the battle before the royal castle.
    ///
    /// The party reaches the capital and finds the royal army drawn up on the road in
    /// front of the gate: the king has ordered the "bandits" destroyed. Xilia reveals
    /// herself as the princess, but the captain will not disobey the king's order even
    /// for her, and his own lieutenant Rona (11) -- of the Skadi family, sworn to guard
    /// the royal line -- changes sides on the spot and joins the party.
    ///
    /// The garrison holds the road between the two low walls; the captain (199) waits
    /// on the bridge on AIType_StandBy until turn 9. From turn 2 to turn 7 two more
    /// knights ride out of the castle gate every turn. The chapter ends when the last
    /// soldier falls; the captain's dying words are conversation 2's first line and the
    /// rest of it closes the chapter.
    /// </summary>
    public class Chapter8 : ChapterEvents
    {
        /// <summary>
        /// Where the party lines up, in creature id order 1..10. Kaili (10) is only in the
        /// party if she was saved in chapter 7, so she is settled only when the record
        /// carries her -- the original's settleFriend did nothing for an empty slot.
        /// </summary>
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 15, 38 },
            {  2, 14, 39 },
            {  3, 16, 39 },
            {  4, 13, 40 },
            {  5, 15, 40 },
            {  6, 16, 37 },
            {  7, 17, 38 },
            {  8, 13, 38 },
            {  9, 14, 37 },
            { 10, 17, 40 },
        };

        /// <summary>The first friend id that is optional: everyone from here on may be missing.</summary>
        private const int FirstOptionalFriendId = 10;

        /// <summary>Rona, the knight who joins. She starts on the road ahead of the party.</summary>
        private const int RonaId = 11;
        private static readonly FDPosition RonaEntry = FDPosition.At(15, 32);

        /// <summary>
        /// The royal army, as (id, definition, x, y, drop item), in the order the original
        /// added them.
        /// </summary>
        private static readonly int[,] Garrison = new int[,]
        {
            { 101, 50803, 13, 26,   0 },
            { 102, 50803, 17, 26,   0 },
            { 103, 50804, 13, 22, 105 },
            { 104, 50804, 17, 22,   0 },
            { 105, 50804,  6, 20, 105 },
            { 106, 50804, 24, 20, 105 },

            { 107, 50806,  9, 19,   0 },
            { 108, 50806, 21, 19,   0 },

            { 109, 50802, 15, 25,   0 },
            { 110, 50802, 14, 20, 102 },
            { 111, 50802, 16, 20,   0 },
            { 112, 50802, 12, 15, 204 },
            { 113, 50802, 18, 15,   0 },

            { 114, 50805, 14, 24,   0 },
            { 115, 50805, 16, 24, 101 },
            { 116, 50805, 13, 18,   0 },
            { 117, 50805, 17, 18,   0 },
        };

        /// <summary>The captain, standing on the bridge and holding his post until turn 9.</summary>
        private const int CaptainId = 199;
        private const int CaptainDefinitionId = 50801;
        private const int CaptainDropItemId = 306;
        private static readonly FDPosition CaptainPost = FDPosition.At(15, 13);

        /// <summary>
        /// The knights that come out of the castle gate, two a turn from turn 2 to turn 7.
        /// The original numbered them from 200 upward as they appeared, so turn N's pair
        /// is 200 + 2 (N - 2) + 1 and + 2: 201 and 202 on turn 2, up to 211 and 212 on turn 7.
        /// </summary>
        private const int KnightDefinitionId = 50807;
        private const int FirstKnightId = 200;
        private const int FirstKnightTurn = 2;
        private const int LastKnightTurn = 7;
        private static readonly FDPosition[] GateExit = new FDPosition[]
        {
            FDPosition.At(14, 8),
            FDPosition.At(16, 8),
        };

        /// <summary>The turn the captain leaves the bridge.</summary>
        private const int CaptainChargeTurn = 9;

        /// <summary>Where the opening cutscene leaves the cursor.</summary>
        private static readonly FDPosition OpeningCursor = FDPosition.At(21, 43);

        public Chapter8(GameMain gameMain) : base(gameMain, 8)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend had
            // acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            for (int turn = FirstKnightTurn; turn <= LastKnightTurn; turn++)
            {
                LoadTurnEvent(++eventId, turn, CreatureFaction.Npc, KnightsAppear(turn));
            }

            LoadTurnEvent(++eventId, CaptainChargeTurn, CreatureFaction.Npc, captainCharge);

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

            // Rona is a new friend, not a guest: she arrives as one and stays one.
            AddCreatureToMap(gameMain, CreatureFaction.Friend, RonaId, RonaId, RonaEntry);

            for (int i = 0; i < Garrison.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Garrison[i, 0], Garrison[i, 1],
                    FDPosition.At(Garrison[i, 2], Garrison[i, 3]), Garrison[i, 4]);
            }

            // The captain keeps the bridge until his turn comes.
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, CaptainId, CaptainDefinitionId,
                CaptainPost, CaptainDropItemId, AITypes.AIType_StandBy);

            gameMain.PushActivity(new SlideCursorActivity(OpeningCursor.X, OpeningCursor.Y));

            // Talking
            PushConversationsActivities(gameMain, 8, 1, 1, 17);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Two more knights ride out of the gate. No dialog goes with them.</summary>
        private static Action<GameMain> KnightsAppear(int turn)
        {
            int firstId = FirstKnightId + 2 * (turn - FirstKnightTurn);
            return (gameMain) =>
            {
                for (int i = 0; i < GateExit.Length; i++)
                {
                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, firstId + 1 + i, KnightDefinitionId, GateExit[i]);
                }
            };
        }

        /// <summary>The captain comes off the bridge.</summary>
        private Action<GameMain> captainCharge = (gameMain) =>
            SetCreatureAiType(gameMain, CaptainId, AITypes.AIType_Aggressive);

        /// <summary>The captain's last words, on the blow that kills him.</summary>
        private Action<GameMain> captainDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 8, 2, 1, 1);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 8, 2, 2, 9);

            // OnGameWin queues itself behind the closing lines, so they play out first.
            gameMain.OnGameWin();
        };

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
