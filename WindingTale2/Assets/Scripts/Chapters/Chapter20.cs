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
    /// Chapter 20 -- the swamp at night.
    ///
    /// The party comes in at the south-east of a black swamp ringed with stone
    /// causeways. Lieba (23) is trapped on the island in the middle with eight of his
    /// men (201..208) around him, and the enemy (101..136) closing in; twenty swamp
    /// creatures (51..70) lurk along the causeways and hold their ground until
    /// someone comes near. Losing Sol (1) or Lieba, or the last of Lieba's men, ends
    /// the chapter. Dakesai (24) waits at the bottom edge of the map.
    ///
    /// The chapter is won when the last enemy falls. If that takes no more than
    /// fifteen turns Dakesai walks up to the party and has something to say; past
    /// that he says nothing. Lieba and Dakesai are both friends -- the original added
    /// them with addFriend -- so both are carried on to the next chapter. Friends
    /// 17..22, which the original never settled, take free tiles in the formation
    /// when the party carries them, since the save keeps only friends on the map.
    /// </summary>
    public class Chapter20 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 31, 36 },
            {  2, 32, 35 },
            {  3, 34, 35 },
            {  4, 33, 36 },
            {  5, 32, 37 },
            {  6, 31, 38 },
            {  7, 33, 38 },
            {  8, 34, 37 },
            {  9, 35, 36 },
            { 10, 32, 39 },
            { 11, 30, 37 },
            { 12, 29, 38 },
            { 13, 29, 36 },
            { 14, 30, 35 },
            { 15, 31, 34 },
            { 16, 30, 39 },
            { 17, 31, 37 },
            { 18, 32, 36 },
            { 19, 32, 38 },
            { 20, 33, 37 },
            { 21, 30, 36 },
            { 22, 31, 35 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The swamp creatures along the causeways, as (id, x, y, drop item): they guard their posts.</summary>
        private const int LurkerDefinitionId = 52001;
        private static readonly int[,] Lurkers = new int[,]
        {
            { 51,  4, 23,   0 },
            { 52,  5, 23,   0 },
            { 53,  6, 24,   0 },
            { 54,  7, 24,   0 },
            { 55, 18, 29,   0 },
            { 56, 20, 29,   0 },
            { 57,  4, 26,   0 },
            { 58,  4, 28,   0 },
            { 59,  4, 32,   0 },
            { 60,  7, 33, 804 },
            { 61, 11, 32,   0 },
            { 62, 15, 35,   0 },
            { 63, 31, 28,   0 },
            { 64, 36, 23, 104 },
            { 65, 33, 23,   0 },
            { 66, 35, 17,   0 },
            { 67, 29,  6,   0 },
            { 68,  4, 10, 104 },
            { 69,  6,  6,   0 },
            { 70, 11,  5,   0 },
        };

        /// <summary>The enemy closing on the island, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 52002, 11, 30,   0 },
            { 102, 52002, 16, 19,   0 },
            { 103, 52008, 13, 20,   0 },
            { 104, 52008, 20, 19, 107 },
            { 105, 52003, 15, 24,   0 },
            { 106, 52003, 20, 24,   0 },
            { 107, 52003, 23, 19,   0 },
            { 108, 52003, 26, 19,   0 },
            { 109, 52004, 10, 30,   0 },
            { 110, 52004, 16, 22,   0 },
            { 111, 52004, 19, 22,   0 },
            { 112, 52004, 31,  8, 210 },
            { 113, 52005, 18, 25,   0 },
            { 114, 52005, 23, 24,   0 },
            { 115, 52005, 26, 21,   0 },
            { 116, 52005, 30, 19, 113 },
            { 117, 52005, 19, 19,   0 },
            { 118, 52005, 18, 19,   0 },
            { 119, 52005, 18, 18,   0 },
            { 120, 52005, 17, 19, 104 },
            { 121, 52005, 12, 15,   0 },
            { 122, 52005, 13, 14, 904 },
            { 123, 52005, 13, 16,   0 },
            { 124, 52005, 14, 15,   0 },
            { 125, 52006, 17, 25,   0 },
            { 126, 52006, 12, 24,   0 },
            { 127, 52006, 10, 22,   0 },
            { 128, 52006,  7, 19,   0 },
            { 129, 52006, 14, 12,   0 },
            { 130, 52006, 14, 13,   0 },
            { 131, 52006, 15, 12,   0 },
            { 132, 52006, 15, 13,   0 },
            { 133, 52006, 24, 16,   0 },
            { 134, 52006, 24, 17,   0 },
            { 135, 52006, 25, 16, 904 },
            { 136, 52006, 25, 17,   0 },
        };

        /// <summary>The two who hold still (109, 112) and the one who guards his post (101).</summary>
        private static readonly int[] StandByIds = new int[] { 109, 112 };
        private const int GuardingEnemyId = 101;

        /// <summary>Lieba (23) on the island, and his men around him as (id, x, y).</summary>
        private const int LiebaId = 23;
        private static readonly FDPosition LiebaPost = FDPosition.At(22, 13);
        private const int GuardDefinitionId = 52009;
        private static readonly int[,] Guards = new int[,]
        {
            { 201, 20, 13 },
            { 202, 21, 12 },
            { 203, 21, 14 },
            { 204, 22, 11 },
            { 205, 22, 15 },
            { 206, 23, 12 },
            { 207, 23, 14 },
            { 208, 24, 13 },
        };

        /// <summary>Dakesai (24), at the bottom edge, and where he walks to at the end.</summary>
        private const int DakesaiId = 24;
        private static readonly FDPosition DakesaiPost = FDPosition.At(26, 40);
        private static readonly FDPosition DakesaiStop = FDPosition.At(26, 37);
        private const int LastTurnForDakesai = 15;

        public Chapter20(GameMain gameMain) : base(gameMain, 20)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, LiebaId, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Npc, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < Lurkers.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Lurkers[i, 0], LurkerDefinitionId,
                    FDPosition.At(Lurkers[i, 1], Lurkers[i, 2]), Lurkers[i, 3], AITypes.AIType_Guard);
            }

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4]);
            }
            foreach (int id in StandByIds)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_StandBy);
            }
            SetCreatureAiType(gameMain, GuardingEnemyId, AITypes.AIType_Guard);

            AddCreatureToMap(gameMain, CreatureFaction.Friend, LiebaId, LiebaId, LiebaPost);
            for (int i = 0; i < Guards.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Guards[i, 0], GuardDefinitionId,
                    FDPosition.At(Guards[i, 1], Guards[i, 2]));
            }

            AddCreatureToMap(gameMain, CreatureFaction.Friend, DakesaiId, DakesaiId, DakesaiPost);

            // Talking
            PushConversationsActivities(gameMain, 20, 1, 1, 17);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>
        /// The last enemy falls. If it took fifteen turns or fewer, Dakesai walks up from
        /// the bottom edge and speaks -- the original chained this as enemyClear2 -- and
        /// either way the chapter is won.
        /// </summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 20, 2, 1, 13);

            gameMain.PushActivity((gameMain) =>
            {
                if (gameMain.gameMap.Map.TurnNo <= LastTurnForDakesai)
                {
                    FDCreature dakesai = gameMain.gameMap.Map.GetCreatureById(DakesaiId);
                    if (dakesai != null && dakesai.Position.AreSame(DakesaiPost))
                    {
                        gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(DakesaiId,
                            FDMovePath.Create(DakesaiPost, DakesaiStop)));
                    }

                    // Talking
                    PushConversationsActivities(gameMain, 20, 3, 1, 16);
                }

                gameMain.OnGameWin();
            });
        };
    }
}
