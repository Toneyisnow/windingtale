using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

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
    /// Once won, the party gathers before the transfer station -- Youni (2) at (16, 12)
    /// facing them, Sol and 003 at (16, 14) and (16, 15), the rest around them at random,
    /// all facing her -- and the ending depends on the Sky Key (item 814): with it the party
    /// takes the transfer station (conversation 4) and goes on to chapter 28; without it
    /// Youni walks up onto the station after line 5 of conversation 5, goes up alone, and
    /// the game ends in the bad ending, GameEndingScene.
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

        /// <summary>Where the party appears and walks in from, on the map's bottom row.</summary>
        private static readonly FDPosition PartyMarchEntry = FDPosition.At(16, 57);

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

        private const int SolId = 1;
        private const int YouniId = 2;
        private const int ThirdFriendId = 3;

        /// <summary>The transfer station, which Youni walks up to in the bad ending.</summary>
        private static readonly FDPosition YouniStation = FDPosition.At(16, 10);

        /// <summary>Where Youni stands for the closing talk, two tiles below the station, facing the party.</summary>
        private static readonly FDPosition YouniGathering = FDPosition.At(16, 12);

        /// <summary>The middle of the party's formation: Sol in front, 003 behind him.</summary>
        private static readonly FDPosition SolGathering = FDPosition.At(16, 14);
        private static readonly FDPosition ThirdFriendGathering = FDPosition.At(16, 15);

        /// <summary>
        /// The rest of the formation, as the original drew it: two on each side of row 13,
        /// row 14 from x 14 to 18, and rows 15 and 16 from x 15 to 17, Sol's and 003's tiles
        /// left out. The others take these at random.
        /// </summary>
        private static readonly int[,] GatheringTiles = new int[,]
        {
            { 13, 13 }, { 14, 13 }, { 18, 13 }, { 19, 13 },
            { 14, 14 }, { 15, 14 }, { 17, 14 }, { 18, 14 },
            { 15, 15 }, { 17, 15 },
            { 15, 16 }, { 16, 16 }, { 17, 16 },
        };

        /// <summary>
        /// Where anyone the formation has no room for goes, in this order: the rest of row
        /// 13 and row 12 either side of Youni, then the steps below the formation.
        /// </summary>
        private static readonly int[,] OverflowTiles = new int[,]
        {
            { 15, 13 }, { 17, 13 }, { 12, 13 }, { 20, 13 },
            { 15, 12 }, { 17, 12 }, { 14, 12 }, { 18, 12 }, { 13, 12 }, { 19, 12 }, { 12, 12 }, { 20, 12 },
            { 15, 17 }, { 16, 17 }, { 17, 17 }, { 16, 18 }, { 17, 18 }, { 15, 18 },
        };

        /// <summary>Youni walks up to the station once this line of conversation 5 has been said.</summary>
        private const int YouniWalksAfterLine = 5;

        /// <summary>Where the cursor (and camera) goes to watch the party gather.</summary>
        private static readonly FDPosition GatheringCursor = FDPosition.At(16, 13);

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
            // The party walks in from the bottom edge, all from one tile, and the talking
            // waits until everyone has arrived.
            MarchInParty(gameMain, PartyEntry, FirstOptionalFriendId, PartyMarchEntry);

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

        /// <summary>
        /// The last captain has fallen. The win's cleanup goes first (the guards left blow up,
        /// the party is no longer grey), then everyone gathers before the transfer station
        /// and the talk begins: with the Sky Key the party goes up and on to chapter 28,
        /// without it Youni goes alone and the game goes to its bad ending.
        /// </summary>
        private static void EnemyClear(GameMain gameMain)
        {
            bool hasSkyKey = TeamHasItem(gameMain, SkyKeyItemId);

            // Declared before anything is queued, so the cleanup runs ahead of the gathering.
            gameMain.FadeOutMusicForVictory();

            GatherAtStation(gameMain);

            if (hasSkyKey)
            {
                // Talking
                PushConversationsActivities(gameMain, 27, 4, 1, 23);

                gameMain.OnGameWin();
            }
            else
            {
                // Talking -- partway through, Youni turns away from the party and walks
                // up onto the station, where she faces them again for the rest.
                PushConversationsActivities(gameMain, 27, 5, 1, YouniWalksAfterLine);
                gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(YouniId,
                    FDMovePath.Create(YouniGathering, YouniStation)));
                PushConversationsActivities(gameMain, 27, 5, YouniWalksAfterLine + 1, 15);

                // The station takes her: 传送术's column of light rises under her and she
                // is gone, and with that the field ends.
                FDCreature youni = gameMain.gameMap.Map.GetCreatureById(YouniId);
                if (youni != null)
                {
                    gameMain.PushActivity(ActivityFactory.CreatureTeleportActivity(youni, YouniStation, false));
                }

                gameMain.OnBadEnding();
            }
        }

        /// <summary>
        /// The party walks up to the transfer station, as the original drew it: Youni two
        /// tiles below the platform facing everyone, Sol and 003 in the middle of the
        /// formation below her, and everyone else on the formation's other tiles at random,
        /// all of them turned to face her. Worked out once the guards have blown up, so the
        /// walk sees the field as it is then; each takes the shortest way over open ground,
        /// all at once, and the talk waits until the last of them has arrived.
        /// </summary>
        private static void GatherAtStation(GameMain gameMain)
        {
            gameMain.PushActivity(new SlideCursorActivity(GatheringCursor));

            gameMain.PushActivity((gameMain) =>
            {
                FDMap map = gameMain.gameMap.Map;
                List<FDCreature> friends = new List<FDCreature>(map.Friends);
                friends.Sort((a, b) => a.Id.CompareTo(b.Id));

                Dictionary<int, FDPosition> places = new Dictionary<int, FDPosition>();
                List<FDPosition> others = Shuffled(TilesOf(GatheringTiles));
                foreach (KeyValuePair<int, FDPosition> fixedPlace in new Dictionary<int, FDPosition>
                    { { YouniId, YouniGathering }, { SolId, SolGathering }, { ThirdFriendId, ThirdFriendGathering } })
                {
                    if (friends.Exists(friend => friend.Id == fixedPlace.Key))
                    {
                        places[fixedPlace.Key] = fixedPlace.Value;
                    }
                    else if (fixedPlace.Key != YouniId)
                    {
                        // Nobody to stand in the middle: the tile goes to the others.
                        others.Add(fixedPlace.Value);
                    }
                }
                others.AddRange(TilesOf(OverflowTiles));

                List<ActivityBase> walks = new List<ActivityBase>();
                foreach (FDCreature friend in friends)
                {
                    Dictionary<FDPosition, FDPosition> cameFrom = SearchOpenGround(map.Field, friend.Position);

                    FDPosition place;
                    if (!places.TryGetValue(friend.Id, out place) || !cameFrom.ContainsKey(place))
                    {
                        place = others.Find(tile => cameFrom.ContainsKey(tile) && IsFreeForFriend(map, tile));
                        if (place != null)
                        {
                            others.Remove(place);
                        }
                    }

                    FDMovePath path = place != null ? PathFromSearch(cameFrom, friend.Position, place) : null;
                    if (path != null)
                    {
                        walks.Add(ActivityFactory.CreatureWalkActivity(friend.Id, path));
                    }
                }

                // Inserted at the front, so in reverse: the walk, then the party turns to face
                // Youni. She ends her walk facing down the map, at them.
                gameMain.InsertActivity((gameMain) =>
                {
                    foreach (FDCreature friend in gameMain.gameMap.Map.Friends)
                    {
                        if (friend.Id != YouniId)
                        {
                            gameMain.gameMap.GetCreature(friend)?.FaceUp();
                        }
                    }
                });
                gameMain.InsertActivity(new ParallelActivity(walks));
            });
        }

        private static List<FDPosition> TilesOf(int[,] tiles)
        {
            List<FDPosition> positions = new List<FDPosition>();
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                positions.Add(FDPosition.At(tiles[i, 0], tiles[i, 1]));
            }
            return positions;
        }

        private static List<FDPosition> Shuffled(List<FDPosition> tiles)
        {
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                FDPosition swap = tiles[i];
                tiles[i] = tiles[j];
                tiles[j] = swap;
            }
            return tiles;
        }

        /// <summary>A tile nobody but a friend stands on -- the friends are all about to move.</summary>
        private static bool IsFreeForFriend(FDMap map, FDPosition tile)
        {
            FDCreature standing = map.GetCreatureAt(tile);
            return standing == null || standing.Faction == CreatureFaction.Friend;
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
