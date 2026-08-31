using System;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 6 -- the harbour of Priz.
    ///
    /// The party comes looking for the sage Yona and finds the docks sealed off by a royal
    /// investigation troop. A soldier recognises one of the escorts the priestess's father
    /// sent along as a wanted man, she refuses to hand him over, and the squad leader
    /// settles it by ordering everyone arrested -- resisters cut down. The battle is the
    /// whole quay: the party at the churches in the south, the soldiers among the cargo
    /// crates in the north.
    ///
    /// Only the front rank moves at first. The half of the troop drawn up at the top of the
    /// map, the squad leader with them, stands its ground until turn 5. On turn 10 the
    /// leader stops believing the party are travellers and lets slip that Lord Laiting is
    /// on his way, and on turn 15 Laiting arrives -- thirteen more soldiers, this time
    /// coming in behind the party from the town side. Both of those are skipped if the
    /// leader is already dead, exactly as the original checked.
    ///
    /// Winning brings Beikewei up from the harbour road: Yona left two days ago, and
    /// Beikewei is to meet him at the royal capital. He walks in as a Friend, so he is
    /// carried into the next chapter without an AdjustFriendsAfterWon step.
    /// </summary>
    public class Chapter6 : ChapterEvents
    {
        /// <summary>Where the party lines up, in creature id order 1..8.</summary>
        private static readonly int[,] PartyEntry = new int[,]
        {
            { 1, 15, 23 },
            { 2,  8, 24 },
            { 3, 11, 26 },
            { 4, 16, 24 },
            { 5, 15, 26 },
            { 6, 17, 25 },
            { 7, 10, 25 },
            { 8, 13, 26 },
        };

        /// <summary>
        /// The four escorts the priestess's father sent with her. Id 51 is the one the
        /// soldier picks out of the line as Edisalins, and is the one that speaks -- the
        /// silence at conversation 1 sequence 8.
        /// </summary>
        private const int EscortDefinitionId = 50606;
        private static readonly int[,] Escorts = new int[,]
        {
            { 51, 11, 23 },
            { 52, 13, 23 },
            { 53, 12, 25 },
            { 54, 14, 25 },
        };

        /// <summary>
        /// The rank standing between the party and the quay, as (id, definition, x, y, drop
        /// item). These come on with no AI type of their own, so they charge. Id 107 is the
        /// soldier who starts the argument.
        /// </summary>
        private static readonly int[,] FrontRank = new int[,]
        {
            { 101, 50603, 11, 15,   0 },
            { 102, 50603, 12, 14,   0 },
            { 103, 50608,  9, 15, 102 },
            { 104, 50608, 13, 15,   0 },
            { 105, 50605, 10, 14, 101 },
            { 106, 50605, 14, 14,   0 },
            { 107, 50602, 10, 17,   0 },
            { 108, 50602, 11, 16, 901 },
            { 109, 50602, 12, 16,   0 },
            { 110, 50602, 13, 17, 203 },
        };

        /// <summary>
        /// The rest of the troop, up on the quay with the squad leader (id 199) in the
        /// middle of it. The original gave every id from 111 to 120 plus 199 AIType_StandBy
        /// and then switched that same set to aggressive on turn 5, so one table serves
        /// both.
        /// </summary>
        private const int LeaderId = 199;
        private static readonly int[,] RearRank = new int[,]
        {
            { 111, 50603,  8,  9,   0 },
            { 112, 50603, 12,  9, 102 },
            { 113, 50608, 10,  7,   0 },
            { 114, 50608, 10, 11,   0 },
            { 115, 50605,  7, 11,   0 },
            { 116, 50605, 14, 11, 201 },
            { 117, 50602,  9,  8, 802 },
            { 118, 50602, 11,  8,   0 },
            { 119, 50602, 11, 10, 201 },
            { 120, 50602,  9, 10,   0 },
            { 199, 50601, 10,  9, 803 },
        };

        /// <summary>
        /// Laiting's column on turn 15, as (id, definition, x, y, drop item). Id 163 is
        /// Laiting himself. They arrive from the town side, which is where the party is
        /// standing, so every one of them takes the first free tile at or beside the one it
        /// is aimed at -- the original used addEnemy:Around: for all thirteen.
        /// </summary>
        private static readonly int[,] Reinforcements = new int[,]
        {
            { 151, 50607, 20, 22, 803 },
            { 152, 50607, 20, 24, 803 },
            { 153, 50607, 20, 26, 803 },
            { 154, 50607, 19, 23, 803 },
            { 155, 50607, 19, 25,   0 },
            { 156, 50607, 18, 24, 803 },
            { 157, 50607, 18, 26, 803 },
            { 158, 50607, 17, 23, 803 },
            { 159, 50607, 17, 25, 803 },
            { 160, 50607, 16, 22, 803 },
            { 161, 50607, 16, 24, 803 },
            { 162, 50607, 16, 26, 803 },
            { 163, 50609, 15, 24, 237 },
        };

        /// <summary>
        /// Beikewei, who turns up once the fighting stops and comes along to the capital.
        /// He appears at the bottom of the harbour road and walks up to the party.
        /// </summary>
        private const int BeikeweiId = 9;
        private static readonly FDPosition BeikeweiEntry = FDPosition.At(12, 26);
        private static readonly FDPosition BeikeweiStand = FDPosition.At(12, 23);

        public Chapter6(GameMain gameMain) : base(gameMain, 6)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend had
            // acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 5, CreatureFaction.Npc, turn5);
            LoadTurnEvent(++eventId, 10, CreatureFaction.Npc, turn10);
            LoadTurnEvent(++eventId, 15, CreatureFaction.Npc, turn15);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, LeaderId, leaderDying);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            for (int i = 0; i < PartyEntry.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Friend, PartyEntry[i, 0], PartyEntry[i, 0],
                    FDPosition.At(PartyEntry[i, 1], PartyEntry[i, 2]));
            }

            for (int i = 0; i < Escorts.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Escorts[i, 0], EscortDefinitionId,
                    FDPosition.At(Escorts[i, 1], Escorts[i, 2]));
            }

            for (int i = 0; i < FrontRank.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, FrontRank[i, 0], FrontRank[i, 1],
                    FDPosition.At(FrontRank[i, 2], FrontRank[i, 3]), FrontRank[i, 4]);
            }

            // The quay watch holds its post until the leader gives the word, on turn 5.
            for (int i = 0; i < RearRank.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, RearRank[i, 0], RearRank[i, 1],
                    FDPosition.At(RearRank[i, 2], RearRank[i, 3]), RearRank[i, 4],
                    AITypes.AIType_StandBy);
            }

            // Talking
            PushConversationsActivities(gameMain, 6, 1, 1, 18);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>The quay watch comes down off the docks. No dialog goes with it.</summary>
        private Action<GameMain> turn5 = (gameMain) =>
        {
            for (int i = 0; i < RearRank.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, RearRank[i, 0], AITypes.AIType_Aggressive);
            }
        };

        /// <summary>
        /// The leader gives up on the party being ordinary travellers and tells his men to
        /// hold out until Laiting arrives. Nothing happens here if he has already fallen --
        /// he is the one doing the talking.
        /// </summary>
        private Action<GameMain> turn10 = (gameMain) =>
        {
            if (IsLeaderDown(gameMain))
            {
                return;
            }

            // Talking
            PushConversationsActivities(gameMain, 6, 2, 1, 6);
        };

        /// <summary>
        /// Laiting's column, arriving behind the party from the town. Skipped along with its
        /// dialog when the leader is already dead, since the scene is the two of them
        /// talking.
        /// </summary>
        private Action<GameMain> turn15 = (gameMain) =>
        {
            if (IsLeaderDown(gameMain))
            {
                return;
            }

            for (int i = 0; i < Reinforcements.GetLength(0); i++)
            {
                AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, Reinforcements[i, 0], Reinforcements[i, 1],
                    FDPosition.At(Reinforcements[i, 2], Reinforcements[i, 3]), Reinforcements[i, 4]);
            }

            // Talking
            PushConversationsActivities(gameMain, 6, 3, 1, 3);
        };

        /// <summary>The squad leader's last words, on the blow that kills him.</summary>
        private Action<GameMain> leaderDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 6, 4, 1, 2);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Beikewei comes up the harbour road behind the party. He is a Friend from the
            // moment he appears, which is what carries him into chapter 7.
            AddCreatureToMap(gameMain, CreatureFaction.Friend, BeikeweiId, BeikeweiId, BeikeweiEntry);

            gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(BeikeweiId,
                FDMovePath.Create(BeikeweiEntry, BeikeweiStand)));

            // Talking
            PushConversationsActivities(gameMain, 6, 5, 1, 19);

            // OnGameWin queues itself behind the walk and the nineteen closing lines, so
            // they play out first.
            gameMain.OnGameWin();
        };

        /// <summary>
        /// Whether the squad leader fell earlier in this battle -- the original's
        /// "[field getDeadCreatureById:199] != nil". The unrevived are skipped for the same
        /// reason CreatureDeadEvent skips them, though an enemy can never be one.
        /// </summary>
        private static bool IsLeaderDown(GameMain gameMain)
        {
            return gameMain.gameMap.Map.DeadCreatures.Exists(
                creature => creature.Id == LeaderId && !creature.IsUnrevived);
        }
    }
}
