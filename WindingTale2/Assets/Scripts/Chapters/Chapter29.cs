using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 29 -- in the boundless dark.
    ///
    /// The core of the Golden City: the dimensional reactor and, at the top of the
    /// map, the fortress' defence centre under the three dragon heads. The party comes
    /// in at the bottom. Six sentries (101..106) and the first host (107..130) attack
    /// at once; three more hosts stand by up the corridor until turns 5 (131..146),
    /// 10 (147..153) and 13 (154..163). Losing Sol (1) or Youni (2) ends the chapter.
    ///
    /// The battle is about the console at (16, 22): when Youni (2) reaches it she
    /// starts taking over the system (conversation 2), and then for every turn she
    /// holds it -- at the end of the enemy turn, with her still standing there -- the
    /// fortress answers: two guards at (15, 33) and (17, 33) on each of four turns
    /// (201..208), and on the fifth the three dragons (301..303) wake on their heads
    /// (conversation 3). Only then can the battle end: the enemy team event waits on
    /// the dragons' waking. When the last enemy falls ASR-06 (999) walks in from the
    /// top of the map for the closing scene (conversation 4) and the party goes on to
    /// chapter 30.
    ///
    /// The original's turn-1..4 handlers also called endTurn on Youni; that ran at the
    /// end of the enemy turn, just before the new turn cleared it again, so it changed
    /// nothing and is not repeated here. Its setExtraInfo noted the turn number and was
    /// never read. Friends 21..32, which the original never settled, take free tiles
    /// beside the party's formation when the party carries them, since the save keeps
    /// only friends on the map.
    /// </summary>
    public class Chapter29 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 16, 59 },
            {  2, 16, 60 },
            {  3, 15, 61 },
            {  4, 16, 61 },
            {  5, 17, 61 },
            {  6, 15, 62 },
            {  7, 16, 62 },
            {  8, 17, 62 },
            {  9, 15, 63 },
            { 10, 16, 63 },
            { 11, 17, 63 },
            { 12, 15, 64 },
            { 13, 16, 64 },
            { 14, 17, 64 },
            { 15, 14, 63 },
            { 16, 14, 64 },
            { 17, 13, 64 },
            { 18, 18, 63 },
            { 19, 18, 64 },
            { 20, 19, 64 },
            { 21, 14, 62 },
            { 22, 18, 62 },
            { 23, 15, 60 },
            { 24, 17, 60 },
            { 25, 12, 62 },
            { 26, 15, 59 },
            { 27, 16, 58 },
            { 28, 17, 59 },
            { 29, 20, 62 },
            { 30, 11, 63 },
            { 31, 21, 63 },
            { 32, 14, 59 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The sentries and the first host, attacking from the start, as (id, definition, x, y).</summary>
        private static readonly int[,] FirstEnemies = new int[,]
        {
            { 101, 52905, 16, 39 },
            { 102, 52905,  2, 38 },
            { 103, 52905, 30, 39 },
            { 104, 52905,  4, 32 },
            { 105, 52905, 30, 34 },
            { 106, 52905, 16, 24 },
            { 107, 52907,  2, 57 },
            { 108, 52907,  3, 57 },
            { 109, 52907,  2, 58 },
            { 110, 52907,  3, 58 },
            { 111, 52907, 29, 57 },
            { 112, 52907, 30, 57 },
            { 113, 52907, 29, 58 },
            { 114, 52907, 30, 58 },
            { 115, 52907, 15, 50 },
            { 116, 52907, 17, 50 },
            { 117, 52907, 14, 49 },
            { 118, 52907, 18, 49 },
            { 119, 52909, 14, 47 },
            { 120, 52909, 18, 47 },
            { 121, 52908,  2, 54 },
            { 122, 52908,  3, 54 },
            { 123, 52908, 29, 54 },
            { 124, 52908, 30, 54 },
            { 125, 52908, 14, 51 },
            { 126, 52908, 18, 51 },
            { 127, 52908, 16, 48 },
            { 128, 52910, 15, 46 },
            { 129, 52910, 17, 46 },
            { 130, 52906, 16, 45 },
        };

        private static readonly int[,] SecondEnemies = new int[,]
        {
            { 131, 52907, 15, 41 },
            { 132, 52907, 17, 41 },
            { 133, 52907, 15, 38 },
            { 134, 52907, 17, 38 },
            { 135, 52907, 15, 35 },
            { 136, 52907, 17, 35 },
            { 137, 52908, 13, 44 },
            { 138, 52908, 19, 44 },
            { 139, 52908, 10, 39 },
            { 140, 52908, 11, 39 },
            { 141, 52908, 12, 39 },
            { 142, 52908, 20, 39 },
            { 143, 52908, 21, 39 },
            { 144, 52908, 22, 39 },
            { 145, 52906, 15, 33 },
            { 146, 52906, 17, 33 },
        };

        private static readonly int[,] ThirdEnemies = new int[,]
        {
            { 147, 52907, 15, 31 },
            { 148, 52907, 17, 31 },
            { 149, 52909, 14, 27 },
            { 150, 52909, 18, 27 },
            { 151, 52908, 15, 29 },
            { 152, 52908, 17, 29 },
            { 153, 52906, 16, 28 },
        };

        private static readonly int[,] FourthEnemies = new int[,]
        {
            { 154, 52907, 15, 25 },
            { 155, 52907, 16, 25 },
            { 156, 52907, 17, 25 },
            { 157, 52909, 14, 21 },
            { 158, 52909, 18, 21 },
            { 159, 52908, 15, 24 },
            { 160, 52908, 17, 24 },
            { 161, 52910, 14, 23 },
            { 162, 52910, 18, 23 },
            { 163, 52906, 16, 22 },
        };

        private const int SecondHostTurn = 5;
        private const int ThirdHostTurn = 10;
        private const int FourthHostTurn = 13;

        /// <summary>The console Youni has to hold, and what the fortress sends each turn she does.</summary>
        private static readonly FDPosition Console = FDPosition.At(16, 22);

        private static readonly int[,] ConsoleAnswers = new int[,]
        {
            { 201, 202, 52907 },
            { 203, 204, 52909 },
            { 205, 206, 52908 },
            { 207, 208, 52906 },
        };

        private static readonly FDPosition[] ConsoleAnswerTiles = new FDPosition[]
        {
            FDPosition.At(15, 33), FDPosition.At(17, 33),
        };

        /// <summary>The three dragons of the defence centre, on their heads.</summary>
        private static readonly int[,] Dragons = new int[,]
        {
            { 301, 52902, 14, 13 },
            { 302, 52903, 18, 13 },
            { 303, 52904, 16, 13 },
        };

        /// <summary>ASR-06, who walks in from the top of the map once the defence centre is down.</summary>
        private const int BossId = 999;
        private const int BossDefinitionId = 52901;
        private static readonly FDPosition BossEntry = FDPosition.At(16, 1);
        private static readonly FDPosition BossStop = FDPosition.At(16, 5);

        public Chapter29(GameMain gameMain) : base(gameMain, 29)
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

            LoadReachPositionEvent(++eventId, 2, Console, onConsole);

            // One turn at the console after another: each answer waits on the one before,
            // and the dragons on the last answer. The end of the battle waits on the dragons.
            FDEvent previous = null;
            for (int i = 0; i < ConsoleAnswers.GetLength(0); i++)
            {
                int answer = i;
                FDEvent held = LoadTurnEndEvent(++eventId, 2, Console, (gameMain) => ConsoleAnswer(gameMain, answer));
                held.AddDependentEvent(previous);
                previous = held;
            }
            FDEvent dragons = LoadTurnEndEvent(++eventId, 2, Console, dragonsWake);
            dragons.AddDependentEvent(previous);

            FDEvent final = LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
            final.AddDependentEvent(dragons);
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

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            AddEnemies(gameMain, FirstEnemies);
            AddEnemies(gameMain, SecondEnemies, AITypes.AIType_StandBy);
            AddEnemies(gameMain, ThirdEnemies, AITypes.AIType_StandBy);
            AddEnemies(gameMain, FourthEnemies, AITypes.AIType_StandBy);

            // Talking
            PushConversationsActivities(gameMain, 29, 1, 1, 14);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Youni reaches the console.</summary>
        private Action<GameMain> onConsole = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 29, 2, 1, 5);
        };

        /// <summary>Another turn at the console: two more guards come up the corridor.</summary>
        private static void ConsoleAnswer(GameMain gameMain, int answer)
        {
            for (int k = 0; k < 2; k++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, ConsoleAnswers[answer, k], ConsoleAnswers[answer, 2],
                    ConsoleAnswerTiles[k]);
            }
        }

        /// <summary>The fifth turn at the console: the defence centre goes to its last line and the dragons wake.</summary>
        private Action<GameMain> dragonsWake = (gameMain) =>
        {
            AddEnemies(gameMain, Dragons);

            // Talking
            PushConversationsActivities(gameMain, 29, 3, 1, 11);
        };

        private Action<GameMain> youniDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 29, 5, 1, 1);
        };

        /// <summary>The defence centre is down: ASR-06 shows himself, and the reactor is turned.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 29, 4, 1, 4);

            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossEntry, 0, AITypes.AIType_StandBy);
            gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(BossId, FDMovePath.Create(BossEntry, BossStop)));

            // Talking
            PushConversationsActivities(gameMain, 29, 4, 5, 29);

            gameMain.OnGameWin();
        };
    }
}
