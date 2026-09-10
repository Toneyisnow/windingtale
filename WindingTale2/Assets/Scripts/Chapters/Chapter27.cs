using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 27 -- the crossing point of fate.
    ///
    /// The ruins under the mountain, the transfer station to the Golden City. The
    /// party comes in at the bottom; ten guards (101..110) attack at once and three
    /// more hosts stand by on the terraces: twenty-nine (118..146) until turn 2,
    /// seventeen (147..163) with the three guard captains (201..203) until turn 10,
    /// fourteen (166..179) until turn 13. The chapter ends when the three captains
    /// have fallen -- there is no team event -- and when the second of them falls,
    /// seven guards (181..187) drop in behind the party at the entrance. A captain's
    /// dying words are conversation 2 while another captain still stands, 3 for the
    /// last. Losing Sol (1) or Youni (2) ends the chapter.
    ///
    /// The ending depends on the Sky Key (item 814): with it the party takes the
    /// transfer station (conversation 4) and goes on to chapter 28; without it Youni
    /// goes up alone and the game ends in the bad ending, chapter 32.
    ///
    /// The original's youniDead pointed at conversation 7, whose one line is in the
    /// strings, so it is kept. Friends 21..32, which the original never settled, take
    /// free tiles behind the party's formation when the party carries them, since the
    /// save keeps only friends on the map.
    /// </summary>
    public class Chapter27 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 16, 54 },
            {  2, 14, 55 },
            {  3, 15, 55 },
            {  4, 16, 55 },
            {  5, 17, 55 },
            {  6, 18, 55 },
            {  7, 14, 56 },
            {  8, 15, 56 },
            {  9, 16, 56 },
            { 10, 17, 56 },
            { 11, 18, 56 },
            { 12, 14, 57 },
            { 13, 15, 57 },
            { 14, 16, 57 },
            { 15, 17, 57 },
            { 16, 18, 57 },
            { 17, 13, 57 },
            { 18, 19, 57 },
            { 19, 13, 56 },
            { 20, 19, 56 },
            { 21, 15, 54 },
            { 22, 16, 53 },
            { 23, 17, 54 },
            { 24, 13, 55 },
            { 25, 14, 54 },
            { 26, 15, 53 },
            { 27, 16, 52 },
            { 28, 17, 53 },
            { 29, 18, 54 },
            { 30, 19, 55 },
            { 31, 12, 55 },
            { 32, 13, 54 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The guards awake from the start, as (id, definition, x, y).</summary>
        private static readonly int[,] FirstEnemies = new int[,]
        {
            { 101, 52701, 15, 34 },
            { 102, 52701, 17, 34 },
            { 103, 52701, 13, 32 },
            { 104, 52701, 19, 32 },
            { 105, 52701,  3, 36 },
            { 106, 52701, 29, 35 },
            { 107, 52701,  9, 23 },
            { 108, 52701, 23, 24 },
            { 109, 52701, 15, 19 },
            { 110, 52701, 17, 19 },
        };

        /// <summary>The host on the lower terrace, standing by until turn 2.</summary>
        private static readonly int[,] SecondEnemies = new int[,]
        {
            { 118, 52703, 15, 38 },
            { 119, 52703, 16, 38 },
            { 120, 52703, 17, 38 },
            { 121, 52703, 14, 37 },
            { 122, 52703, 18, 38 },
            { 123, 52703, 13, 36 },
            { 124, 52703, 19, 36 },
            { 125, 52703, 10, 33 },
            { 126, 52703, 11, 32 },
            { 127, 52703, 22, 33 },
            { 128, 52703, 21, 32 },
            { 129, 52705, 14, 34 },
            { 130, 52705, 15, 33 },
            { 131, 52705, 18, 34 },
            { 132, 52705, 17, 33 },
            { 133, 52705,  9, 32 },
            { 134, 52705, 10, 31 },
            { 135, 52705, 22, 31 },
            { 136, 52705, 23, 32 },
            { 137, 52704, 15, 36 },
            { 138, 52704, 17, 36 },
            { 139, 52704, 14, 35 },
            { 140, 52704, 18, 35 },
            { 141, 52704,  8, 31 },
            { 142, 52704,  9, 30 },
            { 143, 52704, 24, 31 },
            { 144, 52704, 23, 30 },
            { 145, 52706, 12, 30 },
            { 146, 52706, 20, 30 },
        };

        /// <summary>The host on the middle terrace with the three captains, standing by until turn 10.</summary>
        private static readonly int[,] ThirdEnemies = new int[,]
        {
            { 147, 52703, 12, 25 },
            { 148, 52703, 13, 25 },
            { 149, 52703, 14, 25 },
            { 150, 52703, 18, 25 },
            { 151, 52703, 19, 25 },
            { 152, 52703, 20, 25 },
            { 153, 52705, 12, 23 },
            { 154, 52705, 13, 23 },
            { 155, 52705, 19, 23 },
            { 156, 52705, 20, 23 },
            { 157, 52704, 14, 24 },
            { 158, 52704, 15, 24 },
            { 159, 52704, 16, 24 },
            { 160, 52704, 17, 24 },
            { 161, 52704, 18, 24 },
            { 162, 52706, 14, 21 },
            { 163, 52706, 18, 21 },
            { 201, 52702, 16, 22 },
            { 202, 52702, 15, 23 },
            { 203, 52702, 17, 23 },
        };

        /// <summary>The host on the upper terraces, standing by until turn 13.</summary>
        private static readonly int[,] FourthEnemies = new int[,]
        {
            { 166, 52703,  5, 16 },
            { 167, 52703,  6, 16 },
            { 168, 52703,  7, 16 },
            { 169, 52703,  5, 15 },
            { 170, 52703,  7, 15 },
            { 171, 52703, 25, 16 },
            { 172, 52703, 26, 16 },
            { 173, 52703, 27, 16 },
            { 174, 52703, 25, 15 },
            { 175, 52703, 27, 15 },
            { 176, 52705,  5, 14 },
            { 177, 52705,  7, 14 },
            { 178, 52705, 25, 14 },
            { 179, 52705, 27, 14 },
        };

        /// <summary>The guards that drop in at the entrance when the second captain falls.</summary>
        private static readonly int[,] EntranceEnemies = new int[,]
        {
            { 181, 52704, 15, 47 },
            { 182, 52704, 17, 47 },
            { 183, 52704, 15, 48 },
            { 184, 52704, 16, 48 },
            { 185, 52704, 17, 48 },
            { 186, 52704, 15, 49 },
            { 187, 52704, 17, 49 },
        };

        private static readonly int[] CaptainIds = new int[] { 201, 202, 203 };

        private const int SecondHostTurn = 2;
        private const int ThirdHostTurn = 10;
        private const int FourthHostTurn = 13;

        private const int SkyKeyItemId = 814;
        private const int BadEndingChapterId = 32;

        public Chapter27(GameMain gameMain) : base(gameMain, 27)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, SecondHostTurn, CreatureFaction.Npc, (gameMain) => SetAiType(gameMain, SecondEnemies, AITypes.AIType_Aggressive));
            LoadTurnEvent(++eventId, ThirdHostTurn, CreatureFaction.Npc, (gameMain) => SetAiType(gameMain, ThirdEnemies, AITypes.AIType_Aggressive));
            LoadTurnEvent(++eventId, FourthHostTurn, CreatureFaction.Npc, (gameMain) => SetAiType(gameMain, FourthEnemies, AITypes.AIType_Aggressive));

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, 2, (gameMain) => gameMain.OnGameOver());
            LoadDyingEvent(++eventId, 2, youniDying);

            foreach (int captainId in CaptainIds)
            {
                LoadDyingEvent(++eventId, captainId, captainDying);
            }
            foreach (int captainId in CaptainIds)
            {
                LoadDeadEvent(++eventId, captainId, captainDead);
            }
        }

        private static void AddEnemies(GameMain gameMain, int[,] enemies, AITypes? aiType = null)
        {
            for (int i = 0; i < enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, enemies[i, 0], enemies[i, 1],
                    FDPosition.At(enemies[i, 2], enemies[i, 3]), 0, aiType);
            }
        }

        private static void SetAiType(GameMain gameMain, int[,] enemies, AITypes aiType)
        {
            for (int i = 0; i < enemies.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, enemies[i, 0], aiType);
            }
        }

        /// <summary>
        /// How many captains are still on the field. A dying captain still counts -- he
        /// is on the map until his death is done with -- which is how the original told
        /// the second captain's last words from the third's.
        /// </summary>
        private static int CaptainsOnField(GameMain gameMain)
        {
            int count = 0;
            foreach (int captainId in CaptainIds)
            {
                if (gameMain.gameMap.Map.GetCreatureById(captainId) != null)
                {
                    count++;
                }
            }
            return count;
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            AddEnemies(gameMain, FirstEnemies);
            AddEnemies(gameMain, SecondEnemies, AITypes.AIType_StandBy);
            AddEnemies(gameMain, ThirdEnemies, AITypes.AIType_StandBy);
            AddEnemies(gameMain, FourthEnemies, AITypes.AIType_StandBy);

            // Talking
            PushConversationsActivities(gameMain, 27, 1, 1, 19);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        private Action<GameMain> captainDying = (gameMain) =>
        {
            // Talking
            if (CaptainsOnField(gameMain) > 1)
            {
                PushConversationsActivities(gameMain, 27, 2, 1, 1);
            }
            else
            {
                PushConversationsActivities(gameMain, 27, 3, 1, 1);
            }
        };

        private Action<GameMain> captainDead = (gameMain) =>
        {
            int left = CaptainsOnField(gameMain);
            if (left == 2)
            {
                AddEnemies(gameMain, EntranceEnemies);
            }
            else if (left == 0)
            {
                EnemyClear(gameMain);
            }
        };

        private Action<GameMain> youniDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 27, 7, 1, 1);
        };

        /// <summary>The last captain has fallen: with the Sky Key the party goes up, without it the story ends.</summary>
        private static void EnemyClear(GameMain gameMain)
        {
            if (TeamHasItem(gameMain, SkyKeyItemId))
            {
                // Talking
                PushConversationsActivities(gameMain, 27, 4, 1, 23);

                gameMain.OnGameWin();
            }
            else
            {
                gameMain.OnGameEnding(BadEndingChapterId);
            }
        }

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
    }
}
