using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

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
    /// four demons rest on the walls (stand-by) until each is brought down, and go
    /// aggressive on the altar; the original gave them the default AI from the start.
    /// Like any sleeper, one that is hit or that a friend comes within two tiles of
    /// wakes early.
    ///
    /// The Sky Demon moves his people by transfer, drawn as 传送术 draws it: a demon
    /// coming down leaves his wall in a column of light and appears on the altar in
    /// another, and every demon and soldier he calls up appears in one -- see
    /// TransferToAltar and ChapterEvents.TransferInCreatures.
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

        /// <summary>A wave's four soldiers on the tiles beside the altar, as TransferInCreatures rows from <paramref name="firstId"/> on.</summary>
        private static int[,] Soldiers(int firstId)
        {
            int[,] soldiers = new int[SoldierTiles.Length, 4];
            for (int k = 0; k < SoldierTiles.Length; k++)
            {
                soldiers[k, 0] = firstId + k;
                soldiers[k, 1] = SoldierDefinitionId;
                soldiers[k, 2] = SoldierTiles[k].X;
                soldiers[k, 3] = SoldierTiles[k].Y;
            }
            return soldiers;
        }

        /// <summary>
        /// The Sky Demon transfers <paramref name="creatureId"/> -- a demon, or himself -- down
        /// to the altar, as 传送术 does: a column of light where he stands and he is gone,
        /// then the cursor goes to the altar, a column rises there and on each of
        /// <paramref name="soldiers"/>' tiles, and he appears with them. He lands on the altar,
        /// or beside it when someone is standing on it, and stops resting unless
        /// <paramref name="wakes"/> is false. A demon who fell on his wall is not there to
        /// come down; his soldiers still come.
        /// </summary>
        private static void TransferToAltar(GameMain gameMain, int creatureId, int[,] soldiers, bool wakes = true)
        {
            FDCreature transferred = gameMain.gameMap.Map.GetCreatureById(creatureId);
            if (transferred != null)
            {
                gameMain.PushActivity(ActivityFactory.CreatureTeleportActivity(transferred, transferred.Position, false));
            }

            gameMain.PushActivity(ActivityFactory.CreaturesArriveActivity(Altar, (gameMain) =>
            {
                List<FDCreature> arrived = new List<FDCreature>();
                if (transferred != null && gameMain.gameMap.Map.GetCreatureById(creatureId) != null)
                {
                    FDPosition landing = FindStandingTile(gameMain.gameMap.Map, transferred, Altar);
                    if (landing != null)
                    {
                        gameMain.gameMap.RelocateCreature(creatureId, landing);
                    }
                    if (wakes)
                    {
                        SetCreatureAiType(gameMain, creatureId, AITypes.AIType_Aggressive);
                    }

                    // He rested through the enemy turn that has just ended and is greyed out
                    // for it; he lands fresh, like the soldiers appearing beside him.
                    gameMain.gameMap.GetCreature(transferred)?.SetActioned(false);
                    arrived.Add(transferred);
                }

                for (int i = 0; soldiers != null && i < soldiers.GetLength(0); i++)
                {
                    arrived.Add(AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, soldiers[i, 0], soldiers[i, 1],
                        FDPosition.At(soldiers[i, 2], soldiers[i, 3])));
                }
                return arrived;
            }));
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost, 0, AITypes.AIType_StandBy);

            // Talking
            PushConversationsActivities(gameMain, 30, 1, 1, 10);

            // "Come out, all of you": the four demons appear on the walls, in columns of light,
            // and rest there until they are sent down (TransferToAltar wakes them).
            TransferInCreatures(gameMain, CreatureFaction.Enemy, Demons, AITypes.AIType_StandBy);

            // Talking
            PushConversationsActivities(gameMain, 30, 1, 11, 19);

            // The first wave: five soldiers on the altar, in columns of light.
            int[,] firstWave = new int[5, 4];
            firstWave[0, 0] = 101;
            firstWave[0, 1] = SoldierDefinitionId;
            firstWave[0, 2] = Altar.X;
            firstWave[0, 3] = Altar.Y;
            int[,] beside = Soldiers(102);
            for (int k = 0; k < 4; k++)
            {
                for (int c = 0; c < 4; c++)
                {
                    firstWave[k + 1, c] = beside[k, c];
                }
            }
            TransferInCreatures(gameMain, CreatureFaction.Enemy, firstWave);

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

            TransferToAltar(gameMain, 200 + number, Soldiers(101 + 10 * number));
        }

        /// <summary>The fire demon's wave is gone: the Sky Demon himself comes down to the altar.</summary>
        private Action<GameMain> bossComesDown = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 30, 1, 28, 28);

            // He stays on stand-by, as the original left him.
            TransferToAltar(gameMain, BossId, null, false);
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
