using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 5 -- Sera village.
    ///
    /// The party arrives to find a bandit troop already in the square. Their leader has
    /// the priests' guards pinned against the church and is talking about owning the
    /// place; the priestess standing in the church door hires the party on the spot.
    ///
    /// Three things arrive after the fight starts. On turn 3 the bandit leader loses
    /// patience and sends in the second and third companies, who have been standing
    /// about at the east end of the map since the start. On turn 4 a squad of royal
    /// soldiers marches in from the north gate. On turn 7 four wolves wander in out of
    /// the hills at the four map edges -- and, exactly as in chapter 4, the first one to
    /// fall breaks the rest, who recognise the party from last time and run.
    ///
    /// The wolves' escape events watch the very tiles they arrive on, so each is held
    /// back behind the matching death event (FDEvent.AddDependentEvent) the way the
    /// original held them back with "setEvent:dependentOn:". That is why the handlers
    /// that need those event handles are instance methods rather than the Action fields
    /// the rest of the chapter uses.
    /// </summary>
    public class Chapter5 : ChapterEvents
    {
        /// <summary>Where the party lines up, in creature id order 1..7.</summary>
        private static readonly int[,] PartyEntry = new int[,]
        {
            { 1, 15, 20 },
            { 2, 16, 21 },
            { 3, 14, 21 },
            { 4, 15, 21 },
            { 5, 16, 22 },
            { 6, 14, 22 },
            { 7, 15, 22 },
        };

        /// <summary>
        /// The priestess, who joins for this battle and stays with the party afterwards.
        /// She is standing in the church doorway, which is a Blocked tile -- the original
        /// put her there and MoveRangeFinder lets a creature step off the square it starts
        /// on whatever that square is, the same way the original's searchMoveScope did.
        /// </summary>
        private const int PriestessId = 8;
        private static readonly FDPosition PriestessPosition = FDPosition.At(10, 9);

        /// <summary>The village guards she has with her, as (id, x, y).</summary>
        private const int GuardDefinitionId = 50503;
        private static readonly int[,] Guards = new int[,]
        {
            { 81, 17,  8 },
            { 82, 16,  9 },
            { 83, 16, 10 },
            { 84, 17, 11 },
        };

        /// <summary>
        /// The company already fighting when the party walks in, as
        /// (id, definition, x, y, drop item). These come on aggressive.
        /// </summary>
        private static readonly int[,] FirstCompany = new int[,]
        {
            { 21, 50505, 25,  8,   0 },
            { 22, 50505, 27,  8, 901 },
            { 23, 50506, 28,  9,   0 },
            { 24, 50501, 26,  7, 802 },
            { 25, 50502, 26,  9,   0 },
            { 26, 50501, 24,  9,   0 },
            { 27, 50501, 25, 10, 102 },
            { 28, 50501, 27, 10, 101 },
            { 29, 50501, 26, 11,   0 },
        };

        /// <summary>
        /// The second and third companies, and the leader with them. They are on the map
        /// from the start but stand about at the east end until turn 3 -- the original
        /// gave every id from 31 to 59, plus 99, AIType_StandBy and then switched the same
        /// set to aggressive, so one table serves both.
        /// </summary>
        private static readonly int[,] SecondCompany = new int[,]
        {
            { 31, 50501, 30,  7,   0 },
            { 32, 50501, 31,  7,   0 },
            { 33, 50501, 32,  7,   0 },
            { 34, 50501, 30,  8, 802 },
            { 35, 50502, 31,  8,   0 },
            { 36, 50501, 32,  8, 901 },

            { 41, 50501, 30, 11,   0 },
            { 42, 50501, 31, 11,   0 },
            { 43, 50501, 32, 11,   0 },
            { 44, 50501, 30, 12,   0 },
            { 45, 50502, 31, 12, 902 },
            { 46, 50501, 32, 12,   0 },

            { 51, 50505, 35, 10, 108 },
            { 52, 50505, 37, 10,   0 },
            { 53, 50506, 38,  9,   0 },
            { 54, 50501, 36,  7,   0 },
            { 99, 50508, 36,  9, 214 },   // the bandit leader
            { 56, 50501, 34,  9,   0 },
            { 57, 50501, 35,  8,   0 },
            { 58, 50501, 37,  8,   0 },
            { 59, 50501, 36, 11,   0 },
        };

        /// <summary>The royal squad that marches in on turn 4, as (id, x, y) to walk to.</summary>
        private const int SoldierDefinitionId = 50507;
        private static readonly FDPosition SoldierGate = FDPosition.At(22, 1);
        private static readonly int[,] Soldiers = new int[,]
        {
            { 111, 22, 4 },
            { 112, 21, 3 },
            { 113, 23, 3 },
            { 114, 20, 2 },
            { 115, 24, 2 },
            { 116, 22, 2 },
        };

        /// <summary>
        /// The wolves that turn up on turn 7, as (id, x, y). Each comes in at its own edge
        /// of the map and runs back to the same tile once the pack loses its nerve, so one
        /// list is both the spawn points and the escape targets. They all carry 802.
        /// </summary>
        private const int WolfDefinitionId = 50504;
        private const int WolfDropItemId = 802;
        private static readonly int[,] Wolves = new int[,]
        {
            { 101,  1,  9 },
            { 102, 39,  9 },
            { 103, 15, 23 },
            { 104, 22, 23 },
        };

        /// <summary>
        /// The death events for the four wolves, kept so the pack's nerve breaks exactly
        /// once: the first death deactivates all four, which both silences the other three
        /// messages and releases the escape events waiting on them.
        /// </summary>
        private readonly List<FDEvent> wolfDeathEvents = new List<FDEvent>();

        public Chapter5(GameMain gameMain) : base(gameMain, 5)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend had
            // acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 3, CreatureFaction.Npc, turn3);
            LoadTurnEvent(++eventId, 4, CreatureFaction.Npc, turn4);
            LoadTurnEvent(++eventId, 7, CreatureFaction.Npc, turn7);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            for (int i = 0; i < Wolves.GetLength(0); i++)
            {
                wolfDeathEvents.Add(LoadDeadEvent(++eventId, Wolves[i, 0], WolfDead));
            }

            // Each wolf leaves the battle when it gets back to the tile it came in on. It
            // is standing on that tile the moment it arrives, so the event only becomes
            // live once its own death event is done with -- i.e. once the pack has broken.
            for (int i = 0; i < Wolves.GetLength(0); i++)
            {
                int wolfId = Wolves[i, 0];
                FDEvent escaped = LoadReachPositionEvent(++eventId, wolfId,
                    FDPosition.At(Wolves[i, 1], Wolves[i, 2]),
                    (gameMain) => gameMain.gameMap.RemoveCreature(wolfId));
                escaped.AddDependentEvent(wolfDeathEvents[i]);
            }
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            for (int i = 0; i < PartyEntry.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Friend, PartyEntry[i, 0], PartyEntry[i, 0],
                    FDPosition.At(PartyEntry[i, 1], PartyEntry[i, 2]));
            }

            // The priestess is a friend from the moment the battle starts, not an NPC who
            // joins at the end, so she needs no AdjustFriendsAfterWon to come along.
            AddCreatureToMap(gameMain, CreatureFaction.Friend, PriestessId, PriestessId, PriestessPosition);

            for (int i = 0; i < Guards.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Guards[i, 0], GuardDefinitionId,
                    FDPosition.At(Guards[i, 1], Guards[i, 2]));
            }

            for (int i = 0; i < FirstCompany.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, FirstCompany[i, 0], FirstCompany[i, 1],
                    FDPosition.At(FirstCompany[i, 2], FirstCompany[i, 3]), FirstCompany[i, 4]);
            }

            // The rest of the troop is drawn up further east and does not move until the
            // leader tells it to, on turn 3.
            for (int i = 0; i < SecondCompany.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, SecondCompany[i, 0], SecondCompany[i, 1],
                    FDPosition.At(SecondCompany[i, 2], SecondCompany[i, 3]), SecondCompany[i, 4],
                    AITypes.AIType_StandBy);
            }

            // Talking
            PushConversationsActivities(gameMain, 5, 1, 1, 15);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>The leader gives up on the first company and sends the other two in.</summary>
        private Action<GameMain> turn3 = (gameMain) =>
        {
            for (int i = 0; i < SecondCompany.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, SecondCompany[i, 0], AITypes.AIType_Aggressive);
            }

            // Talking
            PushConversationsActivities(gameMain, 5, 2, 1, 1);
        };

        /// <summary>
        /// The royal squad arrives at the north gate. They all come on stacked on the gate
        /// tile and fan out from it, which is the one walk this chapter has: the original
        /// moved the sergeant on the main line and opened a parallel branch for each of the
        /// five behind him, and all six collapse into one ParallelActivity here.
        /// </summary>
        private Action<GameMain> turn4 = (gameMain) =>
        {
            for (int i = 0; i < Soldiers.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Soldiers[i, 0], SoldierDefinitionId, SoldierGate);
            }

            gameMain.PushActivity(new ParallelActivity(SquadMarchIn()));

            // Talking
            PushConversationsActivities(gameMain, 5, 3, 1, 3);
        };

        /// <summary>
        /// Wolves, drawn in by the noise. Two come in at the left and right edges, which
        /// are far from the fighting; the two at the bottom edge arrive behind the party,
        /// so they take the first free tile they can find rather than landing on somebody.
        /// </summary>
        private Action<GameMain> turn7 = (gameMain) =>
        {
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, Wolves[0, 0], WolfDefinitionId,
                FDPosition.At(Wolves[0, 1], Wolves[0, 2]), WolfDropItemId);
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, Wolves[1, 0], WolfDefinitionId,
                FDPosition.At(Wolves[1, 1], Wolves[1, 2]), WolfDropItemId);
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, Wolves[2, 0], WolfDefinitionId,
                FDPosition.At(Wolves[2, 1], Wolves[2, 2]), WolfDropItemId);
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, Wolves[3, 0], WolfDefinitionId,
                FDPosition.At(Wolves[3, 1], Wolves[3, 2]), WolfDropItemId);

            // Talking
            PushConversationsActivities(gameMain, 5, 4, 1, 4);
        };

        /// <summary>
        /// The first wolf to fall. The other three recognise the party from the woods
        /// outside the village and bolt for the edges they came in at.
        /// </summary>
        private void WolfDead(GameMain gameMain)
        {
            // Talking
            PushConversationsActivities(gameMain, 5, 5, 1, 1);

            // Only the first death gets a line, and only the first one breaks the pack.
            // Standing the other three down is also what arms their escape events.
            foreach (FDEvent deathEvent in wolfDeathEvents)
            {
                deathEvent.SetActive(false);
            }

            for (int i = 0; i < Wolves.GetLength(0); i++)
            {
                SetCreatureAiEscape(gameMain, Wolves[i, 0], FDPosition.At(Wolves[i, 1], Wolves[i, 2]));
            }
        }

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 5, 6, 1, 20);

            // OnGameWin queues itself behind the conversation above, so the twenty closing
            // lines -- the priestess hiring the party on to Priz -- play out first.
            gameMain.OnGameWin();
        };

        /// <summary>
        /// The squad fanning out from the north gate. They all start stacked on it, which
        /// is where the original settled them, and step onto the tiles in front. The
        /// waypoint on each path is the corner: down the lane first, then sideways, so
        /// nobody cuts across the trees on either side of the gate.
        /// </summary>
        private static ActivityBase[] SquadMarchIn()
        {
            return new ActivityBase[]
            {
                ActivityFactory.CreatureWalkActivity(111,
                    FDMovePath.Create(SoldierGate, FDPosition.At(22, 4))),
                ActivityFactory.CreatureWalkActivity(112,
                    FDMovePath.Create(SoldierGate, FDPosition.At(22, 3), FDPosition.At(21, 3))),
                ActivityFactory.CreatureWalkActivity(113,
                    FDMovePath.Create(SoldierGate, FDPosition.At(22, 3), FDPosition.At(23, 3))),
                ActivityFactory.CreatureWalkActivity(114,
                    FDMovePath.Create(SoldierGate, FDPosition.At(22, 2), FDPosition.At(20, 2))),
                ActivityFactory.CreatureWalkActivity(115,
                    FDMovePath.Create(SoldierGate, FDPosition.At(22, 2), FDPosition.At(24, 2))),
                ActivityFactory.CreatureWalkActivity(116,
                    FDMovePath.Create(SoldierGate, FDPosition.At(22, 2))),
            };
        }
    }
}
