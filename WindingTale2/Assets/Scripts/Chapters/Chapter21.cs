using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 21 -- the ruined shrine on the plateau.
    ///
    /// The party arrives in two halves at the east and west edges of a round stone
    /// plateau, with the enemy (101..148) and its master (199) holding the shrine in
    /// the south and Xiya (25) and Tuo (26) caught on the altar in the north with
    /// eight guards (201..208) around them. Every other turn from turn 2 to turn 8
    /// four more enemies come up over the four corners of the map. Losing Sol (1)
    /// or Xiya ends the chapter; it is won when the last enemy falls.
    ///
    /// At the end, if the party has gathered the five crystals (117..121) and the
    /// key (805), they are spent and the party is given item 814 -- with a longer
    /// closing scene to go with it.
    ///
    /// Departures from the original: it numbered the corner reinforcements 201..216,
    /// the same ids as the guards, so here they are 221..236 and, since four of them
    /// arrive on each corner tile over the battle, each takes the first free tile at
    /// or next to its corner; its turn-4 handler also set an AI type on Xiya, who is a
    /// friend and has none. Friends 17..24, which it never settled, take free tiles
    /// beside the two halves when the party carries them.
    /// </summary>
    public class Chapter21 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 38, 18 },
            {  2,  4, 19 },
            {  3, 39, 19 },
            {  4, 40, 20 },
            {  5, 40, 18 },
            {  6, 40, 16 },
            {  7, 41, 17 },
            {  8, 41, 19 },
            {  9, 39, 17 },
            { 10,  3, 18 },
            { 11,  3, 20 },
            { 12,  2, 17 },
            { 13,  2, 19 },
            { 14,  2, 21 },
            { 15,  1, 18 },
            { 16,  1, 20 },
            { 17, 39, 18 },
            { 18, 40, 17 },
            { 19, 40, 19 },
            { 20, 41, 18 },
            { 21,  3, 19 },
            { 22,  2, 18 },
            { 23,  2, 20 },
            { 24,  1, 19 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The garrison of the shrine, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 52102, 20, 27,   0 },
            { 102, 52102, 22, 27, 106 },
            { 103, 52102, 14, 25, 112 },
            { 104, 52102, 28, 25,   0 },
            { 105, 52102,  8, 20,   0 },
            { 106, 52102, 34, 20,   0 },
            { 107, 52103, 20, 30, 103 },
            { 108, 52103, 22, 30,   0 },
            { 109, 52103, 20, 20,   0 },
            { 110, 52103, 22, 20, 103 },
            { 111, 52105, 15, 35,   0 },
            { 112, 52105, 16, 34,   0 },
            { 113, 52105, 17, 34,   0 },
            { 114, 52105, 18, 35,   0 },
            { 115, 52105, 24, 35,   0 },
            { 116, 52105, 25, 34,   0 },
            { 117, 52105, 26, 34,   0 },
            { 118, 52105, 27, 35,   0 },
            { 119, 52105, 12, 30,   0 },
            { 120, 52105, 13, 31,   0 },
            { 121, 52105, 14, 30,   0 },
            { 122, 52105, 13, 29,   0 },
            { 123, 52105, 28, 30,   0 },
            { 124, 52105, 30, 30,   0 },
            { 125, 52105, 29, 29,   0 },
            { 126, 52105, 29, 31,   0 },
            { 127, 52106, 19, 33, 331 },
            { 128, 52106, 19, 31,   0 },
            { 129, 52106, 23, 31,   0 },
            { 130, 52106, 23, 33,   0 },
            { 131, 52107, 19, 26, 103 },
            { 132, 52107, 21, 26,   0 },
            { 133, 52107, 23, 26,   0 },
            { 134, 52107, 19, 19,   0 },
            { 135, 52107, 21, 19, 903 },
            { 136, 52107, 23, 19,   0 },
            { 137, 52108, 19, 28,   0 },
            { 138, 52108, 21, 28,   0 },
            { 139, 52108, 23, 28,   0 },
            { 140, 52108, 19, 21,   0 },
            { 141, 52108, 21, 21,   0 },
            { 142, 52108, 23, 21,   0 },
            { 143, 52109, 20, 32,   0 },
            { 144, 52109, 22, 32,   0 },
            { 145, 52109, 11, 29,   0 },
            { 146, 52109, 12, 28,   0 },
            { 147, 52109, 30, 28,   0 },
            { 148, 52109, 31, 29,   0 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 52101;
        private static readonly FDPosition BossPost = FDPosition.At(21, 33);

        /// <summary>Xiya (25) and Tuo (26) on the altar, and the guards around them as (id, x, y).</summary>
        private const int XiyaId = 25;
        private static readonly FDPosition XiyaPost = FDPosition.At(20, 13);
        private const int TuoId = 26;
        private static readonly FDPosition TuoPost = FDPosition.At(22, 13);
        private const int GuardDefinitionId = 52110;
        private static readonly int[,] Guards = new int[,]
        {
            { 201, 19, 13 },
            { 202, 20, 12 },
            { 203, 20, 14 },
            { 204, 21, 12 },
            { 205, 21, 14 },
            { 206, 22, 12 },
            { 207, 22, 14 },
            { 208, 23, 13 },
        };

        /// <summary>The four corners the reinforcements come over, one wave of four every other turn.</summary>
        private const int ReinforcementDefinitionId = 52104;
        private static readonly int[,] Corners = new int[,]
        {
            {  2,  2 },
            { 40,  2 },
            {  2, 39 },
            { 40, 39 },
        };
        private const int FirstReinforcementId = 221;
        private static readonly int[] ReinforcementTurns = new int[] { 2, 4, 6, 8 };

        /// <summary>The crystals and the key that, together, earn item 814 at the end.</summary>
        private static readonly int[] RelicItemIds = new int[] { 117, 118, 119, 120, 121, 805 };
        private const int RewardItemId = 814;

        public Chapter21(GameMain gameMain) : base(gameMain, 21)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            for (int wave = 0; wave < ReinforcementTurns.Length; wave++)
            {
                int firstId = FirstReinforcementId + wave * Corners.GetLength(0);
                LoadTurnEvent(++eventId, ReinforcementTurns[wave], CreatureFaction.Npc,
                    (gameMain) => Reinforce(gameMain, firstId));
            }

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, XiyaId, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, BossId, bossDying);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4]);
            }
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost);

            AddCreatureToMap(gameMain, CreatureFaction.Friend, XiyaId, XiyaId, XiyaPost);
            AddCreatureToMap(gameMain, CreatureFaction.Friend, TuoId, TuoId, TuoPost);
            for (int i = 0; i < Guards.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Guards[i, 0], GuardDefinitionId,
                    FDPosition.At(Guards[i, 1], Guards[i, 2]));
            }

            // Talking
            PushConversationsActivities(gameMain, 21, 1, 1, 17);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>One wave: four enemies, one over each corner.</summary>
        private static void Reinforce(GameMain gameMain, int firstId)
        {
            for (int i = 0; i < Corners.GetLength(0); i++)
            {
                AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, firstId + i, ReinforcementDefinitionId,
                    FDPosition.At(Corners[i, 0], Corners[i, 1]));
            }
        }

        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 21, 3, 1, 4);
        };

        /// <summary>
        /// The last enemy falls. With all six relics in hand the party trades them for
        /// item 814 and gets the longer ending.
        /// </summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 21, 3, 5, 14);

            if (Array.TrueForAll(RelicItemIds, itemId => TeamHasItem(gameMain, itemId)))
            {
                foreach (int itemId in RelicItemIds)
                {
                    TeamConsumeItem(gameMain, itemId);
                }
                AddItemToTeam(gameMain, RewardItemId);

                // Talking
                PushConversationsActivities(gameMain, 21, 3, 15, 26);
            }

            // Talking
            PushConversationsActivities(gameMain, 21, 3, 27, 29);

            gameMain.OnGameWin();
        };

        /// <summary>The party's members, living and fallen -- the original's teamHasItem looked in both lists.</summary>
        private static IEnumerable<FDCreature> TeamMembers(GameMain gameMain)
        {
            foreach (FDCreature creature in gameMain.gameMap.Map.Friends)
            {
                yield return creature;
            }
            foreach (FDCreature creature in gameMain.gameMap.Map.DeadCreatures)
            {
                if (creature.Faction == CreatureFaction.Friend)
                {
                    yield return creature;
                }
            }
        }

        private static bool TeamHasItem(GameMain gameMain, int itemId)
        {
            foreach (FDCreature creature in TeamMembers(gameMain))
            {
                if (creature.Items != null && creature.Items.Contains(itemId))
                {
                    return true;
                }
            }
            return false;
        }

        private static void TeamConsumeItem(GameMain gameMain, int itemId)
        {
            foreach (FDCreature creature in TeamMembers(gameMain))
            {
                int index = creature.Items != null ? creature.Items.IndexOf(itemId) : -1;
                if (index >= 0)
                {
                    creature.RemoveItemAt(index);
                    return;
                }
            }
        }

        private static void AddItemToTeam(GameMain gameMain, int itemId)
        {
            foreach (FDCreature creature in gameMain.gameMap.Map.Friends)
            {
                if (!creature.IsItemsFull())
                {
                    creature.AddItem(itemId);
                    return;
                }
            }
        }
    }
}
