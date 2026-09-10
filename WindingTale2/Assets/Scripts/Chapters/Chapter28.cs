using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 28 -- the explorers.
    ///
    /// The transfer station on the Golden City, the party's first foothold on the
    /// fortress. The party arrives on the east side; eight guards (101..108) attack
    /// from the pools at once, while the two guard captains (201, 202) and their four
    /// men (109..112) hold the west end until turn 7. Defence squads keep arriving by
    /// transfer, each announced by a captain and its own leader: the first (114..119)
    /// at the end of turn 1, then on turns 4, 5 and 6 (120..125, 126..131, and twelve
    /// more on the far side). The chapter ends when both captains have fallen -- there
    /// is no team event; the third captain 114's dying words and theirs are
    /// conversation 3. Losing Sol (1) or Youni (2) ends the chapter.
    ///
    /// The original gave six of the fourth squad ids already in use on the map (124,
    /// 125, 126, 102, 130, 103); they are 150..155 here so the map can tell them apart.
    /// Friends 21..32, which the original never settled, take free tiles beside the
    /// party's formation when the party carries them, since the save keeps only
    /// friends on the map.
    /// </summary>
    public class Chapter28 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 33, 22 },
            {  2, 34, 22 },
            {  3, 38, 22 },
            {  4, 39, 22 },
            {  5, 39, 21 },
            {  6, 38, 21 },
            {  7, 37, 21 },
            {  8, 36, 21 },
            {  9, 35, 21 },
            { 10, 34, 21 },
            { 11, 33, 21 },
            { 12, 33, 20 },
            { 13, 34, 20 },
            { 14, 35, 20 },
            { 15, 36, 20 },
            { 16, 37, 20 },
            { 17, 38, 20 },
            { 18, 39, 20 },
            { 19, 39, 19 },
            { 20, 33, 19 },
            { 21, 36, 23 },
            { 22, 36, 24 },
            { 23, 34, 23 },
            { 24, 35, 24 },
            { 25, 36, 25 },
            { 26, 37, 24 },
            { 27, 38, 23 },
            { 28, 33, 23 },
            { 29, 34, 24 },
            { 30, 35, 25 },
            { 31, 36, 26 },
            { 32, 37, 25 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The guards at the pools, attacking from the start, as (id, definition, x, y).</summary>
        private static readonly int[,] FirstEnemies = new int[,]
        {
            { 101, 52805, 13, 20 },
            { 102, 52805, 13, 21 },
            { 103, 52803, 14, 19 },
            { 104, 52803, 14, 20 },
            { 105, 52803, 14, 21 },
            { 106, 52803, 14, 22 },
            { 107, 52801,  5, 19 },
            { 108, 52801,  5, 22 },
        };

        /// <summary>The captains' guard and the captains, holding the west end until turn 7.</summary>
        private static readonly int[,] CaptainGroup = new int[,]
        {
            { 109, 52803,  5, 20 },
            { 110, 52803,  5, 21 },
            { 111, 52806,  3, 20 },
            { 112, 52806,  3, 21 },
            { 201, 52802,  4, 20 },
            { 202, 52802,  4, 21 },
        };

        private static readonly int[,] FirstSquad = new int[,]
        {
            { 114, 52802, 23, 22 },
            { 115, 52803, 24, 21 },
            { 116, 52803, 24, 22 },
            { 117, 52803, 24, 23 },
            { 118, 52805, 23, 21 },
            { 119, 52805, 23, 23 },
        };

        private static readonly int[,] SecondSquad = new int[,]
        {
            { 120, 52803, 20, 12 },
            { 121, 52803, 21, 12 },
            { 122, 52803, 22, 12 },
            { 123, 52804, 22, 11 },
            { 124, 52801, 21, 11 },
            { 125, 52804, 20, 11 },
        };

        private static readonly int[,] ThirdSquad = new int[,]
        {
            { 126, 52803, 14, 12 },
            { 127, 52803, 15, 12 },
            { 128, 52803, 16, 12 },
            { 129, 52805, 14, 11 },
            { 130, 52805, 15, 11 },
            { 131, 52805, 16, 11 },
        };

        /// <summary>The fourth squad: the original's 124, 125, 126, 102, 130, 103 are 150..155.</summary>
        private static readonly int[,] FourthSquad = new int[,]
        {
            { 150, 52803,  8, 12 },
            { 151, 52803,  9, 12 },
            { 152, 52803, 10, 12 },
            { 132, 52804,  8, 11 },
            { 153, 52801,  9, 11 },
            { 133, 52804, 10, 11 },
            { 136, 52805,  2, 12 },
            { 137, 52805,  3, 12 },
            { 138, 52805,  4, 12 },
            { 139, 52804,  2, 11 },
            { 154, 52804,  4, 11 },
            { 155, 52801,  3, 11 },
        };

        private static readonly int[] CaptainIds = new int[] { 201, 202 };
        private const int ThirdCaptainId = 114;

        private const int FirstSquadTurn = 1;
        private const int SecondSquadTurn = 4;
        private const int ThirdSquadTurn = 5;
        private const int FourthSquadTurn = 6;
        private const int CaptainAttackTurn = 7;

        public Chapter28(GameMain gameMain) : base(gameMain, 28)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, FirstSquadTurn, CreatureFaction.Npc, (gameMain) => Squad(gameMain, FirstSquad, 9));
            LoadTurnEvent(++eventId, SecondSquadTurn, CreatureFaction.Npc, (gameMain) => Squad(gameMain, SecondSquad, 11));
            LoadTurnEvent(++eventId, ThirdSquadTurn, CreatureFaction.Npc, (gameMain) => Squad(gameMain, ThirdSquad, 13));
            LoadTurnEvent(++eventId, FourthSquadTurn, CreatureFaction.Npc, (gameMain) => Squad(gameMain, FourthSquad, 15));
            LoadTurnEvent(++eventId, CaptainAttackTurn, CreatureFaction.Npc, (gameMain) => SetAiType(gameMain, CaptainGroup, AITypes.AIType_Aggressive));

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, 2, (gameMain) => gameMain.OnGameOver());
            LoadDyingEvent(++eventId, 2, youniDying);

            LoadDyingEvent(++eventId, ThirdCaptainId, captainDying);
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

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            AddEnemies(gameMain, FirstEnemies);
            AddEnemies(gameMain, CaptainGroup, AITypes.AIType_StandBy);

            // Talking
            PushConversationsActivities(gameMain, 28, 1, 1, 8);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>A defence squad arrives by transfer, announced by its two lines of conversation 1.</summary>
        private static void Squad(GameMain gameMain, int[,] squad, int firstLine)
        {
            AddEnemies(gameMain, squad);

            // Talking
            PushConversationsActivities(gameMain, 28, 1, firstLine, firstLine + 1);
        }

        private Action<GameMain> captainDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 28, 3, 1, 1);
        };

        private Action<GameMain> captainDead = (gameMain) =>
        {
            foreach (int captainId in CaptainIds)
            {
                if (gameMain.gameMap.Map.GetCreatureById(captainId) != null)
                {
                    return;
                }
            }

            // Talking
            PushConversationsActivities(gameMain, 28, 5, 1, 5);

            gameMain.OnGameWin();
        };

        private Action<GameMain> youniDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 28, 6, 1, 1);
        };
    }
}
