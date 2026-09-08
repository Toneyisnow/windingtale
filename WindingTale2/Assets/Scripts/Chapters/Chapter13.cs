using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 13 -- the camp in the desert.
    ///
    /// A nomad camp of twelve villagers (51..62) sits between the party, coming in from
    /// the west, and an enemy host drawn up along the east side. The villagers cannot
    /// fight; if the last of them dies the chapter is lost, as it is if Sol (1) falls.
    /// Hawate (16) rides in at the south-east corner on turn 4 and joins the party;
    /// on turn 9 the enemy general (199) arrives with his escort (151..159) at the
    /// eastern edge. One enemy (125) is a thief who goes for the chest by the party's
    /// start and then runs for the northern edge.
    ///
    /// Sol stepping onto the tile the enemy standard-bearer (106) holds at the start,
    /// (25, 6), finds a hero's badge: a ghost (88) speaks to him and he is given item
    /// 811. The chapter is won when the last enemy falls.
    /// </summary>
    public class Chapter13 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 4, 16 },
            {  2, 5, 15 },
            {  3, 3, 17 },
            {  4, 5, 18 },
            {  5, 3, 19 },
            {  6, 2, 18 },
            {  7, 2, 16 },
            {  8, 2, 13 },
            {  9, 5, 20 },
            { 10, 4, 13 },
            { 11, 6, 13 },
            { 12, 3, 15 },
            { 13, 6, 17 },
            { 14, 1, 17 },
            { 15, 7, 19 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The enemy host, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 51304, 29, 15,   0 },
            { 102, 51304, 27,  4, 903 },
            { 103, 51304, 34, 16,   0 },
            { 104, 51304, 34, 22,   0 },
            { 105, 51304, 33,  3, 901 },

            { 106, 51302, 25,  6, 903 },
            { 107, 51302, 27, 13, 902 },
            { 108, 51302, 26, 21,   0 },
            { 109, 51302, 23, 23,   0 },
            { 110, 51302, 21, 25,   0 },
            { 111, 51302, 29,  2,   0 },
            { 112, 51302, 30, 10,   0 },
            { 113, 51302, 31, 14, 116 },
            { 114, 51302, 29, 19, 102 },
            { 115, 51302, 27, 23,   0 },
            { 116, 51302, 31, 24, 901 },
            { 117, 51302, 34, 12,   0 },
            { 118, 51302, 33,  7,   0 },
            { 119, 51302, 34,  1,   0 },
            { 120, 51302, 37,  7,   0 },
            { 121, 51302, 37, 13,   0 },
            { 122, 51302, 36, 17, 116 },
            { 123, 51302, 37, 19,   0 },
            { 124, 51302, 39, 16, 102 },
            { 125, 51302, 10,  3, 803 },
            { 126, 51302, 40, 10, 102 },

            { 127, 51303, 31, 17,   0 },
            { 128, 51303, 31,  1, 901 },
            { 129, 51303, 34, 10,   0 },
            { 130, 51303, 28, 25,   0 },
            { 131, 51303, 38, 11, 903 },
            { 132, 51303, 39, 21, 102 },
            { 133, 51303, 34, 25,   0 },
        };

        /// <summary>The thief: the chest it wants and the edge it runs for.</summary>
        private const int ThiefId = 125;
        private static readonly FDPosition ThiefChest = FDPosition.At(6, 4);
        private static readonly FDPosition ThiefEscape = FDPosition.At(25, 1);

        /// <summary>The villagers, as (id, x, y). They all share one definition.</summary>
        private const int VillagerDefinitionId = 51305;
        private static readonly int[,] Villagers = new int[,]
        {
            { 51, 20, 15 },
            { 52, 21, 14 },
            { 53, 22, 17 },
            { 54, 24, 18 },
            { 55, 23, 12 },
            { 56, 21, 11 },
            { 57, 21,  8 },
            { 58, 19,  7 },
            { 59, 18, 11 },
            { 60, 17, 14 },
            { 61, 18, 18 },
            { 62, 20, 20 },
        };

        private const int HawateId = 16;
        private static readonly FDPosition HawateEntry = FDPosition.At(24, 25);
        private const int HawateTurn = 4;

        /// <summary>
        /// The general's escort, as (id, x, y, drop item), arriving at the eastern edge on
        /// turn 9. They land on the first free tile at or next to the one named, as the
        /// original's "Around:" did.
        /// </summary>
        private const int EscortDefinitionId = 51302;
        private static readonly int[,] Escort = new int[,]
        {
            { 151, 36, 10,   0 },
            { 152, 37,  9,   0 },
            { 153, 37, 11, 901 },
            { 154, 38,  8,   0 },
            { 155, 38, 10,   0 },
            { 156, 38, 12,   0 },
            { 157, 39,  9,   0 },
            { 158, 39, 11, 103 },
            { 159, 40, 10,   0 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 51301;
        private const int BossDropItemId = 309;
        private static readonly FDPosition BossEntry = FDPosition.At(35, 9);
        private const int ReinforcementTurn = 9;

        /// <summary>The hero's badge: where it lies, who speaks from it, and what Sol is given.</summary>
        private static readonly FDPosition BadgeTile = FDPosition.At(25, 6);
        private const int GhostId = 88;
        private const int GhostDefinitionId = 718;
        private const int BadgeItemId = 811;

        public Chapter13(GameMain gameMain) : base(gameMain, 13)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, HawateTurn, CreatureFaction.Npc, hawateAppear);
            LoadTurnEvent(++eventId, ReinforcementTurn, CreatureFaction.Npc, reinforcement);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Npc, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadReachPositionEvent(++eventId, ThiefId, ThiefEscape,
                (gameMain) => gameMain.gameMap.RemoveCreature(ThiefId));

            LoadReachPositionEvent(++eventId, 1, BadgeTile, heroBadge);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4]);
            }

            SetCreatureAiTreasure(gameMain, ThiefId, ThiefChest, ThiefEscape);

            for (int i = 0; i < Villagers.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Villagers[i, 0], VillagerDefinitionId,
                    FDPosition.At(Villagers[i, 1], Villagers[i, 2]));
            }

            // Talking
            PushConversationsActivities(gameMain, 13, 1, 1, 6);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>
        /// Sol finds the hero's badge. The ghost who speaks is never on the field: the
        /// original put him straight into the dead list so the talk could find his
        /// portrait, and TalkActivity looks there too.
        /// </summary>
        private Action<GameMain> heroBadge = (gameMain) =>
        {
            if (!gameMain.gameMap.Map.DeadCreatures.Exists(c => c.Id == GhostId))
            {
                CreatureDefinition definition = DefinitionStore.Instance.GetCreatureDefinition(GhostDefinitionId);
                FDCreature ghost = new FDAICreature(GhostId, definition, CreatureFaction.Npc, AITypes.AIType_StandBy);
                gameMain.gameMap.Map.DeadCreatures.Add(ghost);
            }

            // Talking
            PushConversationsActivities(gameMain, 13, 5, 1, 3);

            FDCreature sol = gameMain.gameMap.Map.GetCreatureById(1);
            if (sol != null && !sol.IsItemsFull())
            {
                sol.AddItem(BadgeItemId);
            }
        };

        /// <summary>Turn 4: Hawate rides in at the south-east corner and joins the party.</summary>
        private Action<GameMain> hawateAppear = (gameMain) =>
        {
            AddCreatureToMap(gameMain, CreatureFaction.Friend, HawateId, HawateId, HawateEntry);

            // Talking
            PushConversationsActivities(gameMain, 13, 2, 1, 7);
        };

        /// <summary>Turn 9: the general and his escort arrive at the eastern edge.</summary>
        private Action<GameMain> reinforcement = (gameMain) =>
        {
            for (int i = 0; i < Escort.GetLength(0); i++)
            {
                AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, Escort[i, 0], EscortDefinitionId,
                    FDPosition.At(Escort[i, 1], Escort[i, 2]), Escort[i, 3]);
            }
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossEntry, BossDropItemId);

            // Talking
            PushConversationsActivities(gameMain, 13, 3, 1, 1);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 13, 4, 1, 11);

            gameMain.OnGameWin();
        };
    }
}
