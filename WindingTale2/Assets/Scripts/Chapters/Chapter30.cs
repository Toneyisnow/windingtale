using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 30 -- the last chapter of the legend.
    ///
    /// The altar of the Golden City after the reactor's burst. The party stands on the
    /// terrace below it; ASR-06, the Sky Demon (999), waits at the head of the water
    /// channel and, as the opening talk goes on, calls up his four remade demons --
    /// earth (201), water (202), wind (203) and fire (204) -- onto the walls beside
    /// him, then sends five of his soldiers (101..105) down to the altar. Each time a
    /// wave falls the next demon comes: at the end of the following turn he steps down
    /// onto the altar at (23, 20) with four more soldiers around him (111..114 for the
    /// earth demon, 121..124 water, 131..134 wind, 141..144 fire), and when the fire
    /// demon's wave is gone the Sky Demon himself comes down. The chapter -- and the
    /// game -- ends when the last enemy falls: conversation 2 and the good ending,
    /// chapter 31. Losing Sol (1) or Youni (2) ends the chapter.
    ///
    /// The one-turn delay is the original's: each wave's five death events gate a
    /// TurnEndEvent that does nothing, which in turn gates the TurnEndEvent that brings
    /// the demon down, so he appears at the end of the turn after the one the wave died
    /// in. The Sky Demon is on stand-by from the start and the original never changed
    /// that, so once he is on the altar he waits to be attacked, as it wrote him. The
    /// four demons take the default AI, as in the original; penned on the walls by the
    /// channel they have nowhere to go until they are brought down.
    ///
    /// Friends 21..32, which the original never settled, take free tiles inside the
    /// party's formation when the party carries them, since the save keeps only
    /// friends on the map.
    /// </summary>
    public class Chapter30 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 23, 24 },
            {  2, 21, 24 },
            {  3, 22, 26 },
            {  4, 25, 25 },
            {  5, 26, 27 },
            {  6, 27, 29 },
            {  7, 22, 32 },
            {  8, 24, 31 },
            {  9, 18, 28 },
            { 10, 23, 28 },
            { 11, 20, 23 },
            { 12, 26, 23 },
            { 13, 26, 31 },
            { 14, 22, 30 },
            { 15, 20, 27 },
            { 16, 20, 30 },
            { 17, 25, 29 },
            { 18, 28, 27 },
            { 19, 24, 26 },
            { 20, 21, 28 },
            { 21, 23, 27 },
            { 22, 24, 27 },
            { 23, 24, 28 },
            { 24, 22, 27 },
            { 25, 23, 26 },
            { 26, 22, 28 },
            { 27, 23, 29 },
            { 28, 25, 27 },
            { 29, 24, 29 },
            { 30, 25, 28 },
            { 31, 21, 27 },
            { 32, 23, 25 },
        };

        private const int FirstOptionalFriendId = 10;

        private const int BossId = 999;
        private const int BossDefinitionId = 53001;
        private static readonly FDPosition BossPost = FDPosition.At(23, 5);

        /// <summary>The four demons on the walls, as (id, definition, x, y).</summary>
        private static readonly int[,] Demons = new int[,]
        {
            { 201, 53002, 25, 8 },
            { 202, 53003, 24, 8 },
            { 203, 53004, 22, 8 },
            { 204, 53005, 21, 8 },
        };

        private const int SoldierDefinitionId = 53006;

        /// <summary>The altar, where each demon comes down, and the tiles his soldiers take.</summary>
        private static readonly FDPosition Altar = FDPosition.At(23, 20);

        private static readonly FDPosition[] SoldierTiles = new FDPosition[]
        {
            FDPosition.At(22, 20), FDPosition.At(24, 20), FDPosition.At(21, 20), FDPosition.At(25, 20),
        };

        private const int GoodEndingChapterId = 31;

        public Chapter30(GameMain gameMain) : base(gameMain, 30)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, 2, (gameMain) => gameMain.OnGameOver());
            LoadDyingEvent(++eventId, 2, youniDying);

            // Wave by wave: the first wave is the five soldiers, each later one a demon with
            // his four. When a wave has died, the end of the next turn brings the next demon.
            LoadWave(ref eventId, new int[] { 101, 102, 103, 104, 105 }, (gameMain) => DemonComesDown(gameMain, 1));
            LoadWave(ref eventId, new int[] { 201, 111, 112, 113, 114 }, (gameMain) => DemonComesDown(gameMain, 2));
            LoadWave(ref eventId, new int[] { 202, 121, 122, 123, 124 }, (gameMain) => DemonComesDown(gameMain, 3));
            LoadWave(ref eventId, new int[] { 203, 131, 132, 133, 134 }, (gameMain) => DemonComesDown(gameMain, 4));
            LoadWave(ref eventId, new int[] { 204, 141, 142, 143, 144 }, bossComesDown);

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, BossId, bossDying);
        }

        /// <summary>
        /// The original's chain for one wave: a death event per member, a turn-end event
        /// that waits on all of them and does nothing, and the turn-end event that waits on
        /// that one and brings the next demon -- one turn later.
        /// </summary>
        private void LoadWave(ref int eventId, int[] members, Action<GameMain> next)
        {
            List<FDEvent> deaths = new List<FDEvent>();
            foreach (int memberId in members)
            {
                deaths.Add(LoadDeadEvent(++eventId, memberId, (gameMain) => { }));
            }

            FDEvent waveGone = LoadTurnEndEvent(++eventId, (gameMain) => { });
            foreach (FDEvent death in deaths)
            {
                waveGone.AddDependentEvent(death);
            }

            FDEvent comesDown = LoadTurnEndEvent(++eventId, next);
            comesDown.AddDependentEvent(waveGone);
        }

        private static void AddSoldiers(GameMain gameMain, int firstId)
        {
            for (int k = 0; k < SoldierTiles.Length; k++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, firstId + k, SoldierDefinitionId, SoldierTiles[k]);
            }
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost, 0, AITypes.AIType_StandBy);

            // Talking
            PushConversationsActivities(gameMain, 30, 1, 1, 10);

            // "Come out, all of you": the four demons appear on the walls.
            gameMain.PushActivity((gameMain) =>
            {
                for (int i = 0; i < Demons.GetLength(0); i++)
                {
                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, Demons[i, 0], Demons[i, 1],
                        FDPosition.At(Demons[i, 2], Demons[i, 3]));
                }
            });

            // Talking
            PushConversationsActivities(gameMain, 30, 1, 11, 19);

            // The first wave: five soldiers on the altar.
            gameMain.PushActivity((gameMain) =>
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, 101, SoldierDefinitionId, Altar);
                AddSoldiers(gameMain, 102);
            });

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>The Sky Demon sends the next demon down: his two lines, then the demon on the altar with four soldiers.</summary>
        private static void DemonComesDown(GameMain gameMain, int number)
        {
            // Talking
            PushConversationsActivities(gameMain, 30, 1, 18 + 2 * number, 19 + 2 * number);

            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.RelocateCreature(200 + number, Altar);
                AddSoldiers(gameMain, 101 + 10 * number);
            });
        }

        /// <summary>The fire demon's wave is gone: the Sky Demon himself comes down to the altar.</summary>
        private Action<GameMain> bossComesDown = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 30, 1, 28, 28);

            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.RelocateCreature(BossId, Altar);
            });
        };

        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 30, 2, 1, 1);
        };

        private Action<GameMain> youniDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 30, 3, 1, 1);
        };

        /// <summary>The Sky Demon has fallen: the story is told, and the game goes to its ending.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 30, 2, 2, 26);

            gameMain.OnGameEnding(GoodEndingChapterId);
        };
    }
}
