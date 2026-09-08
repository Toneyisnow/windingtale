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
    /// Chapter 12 -- the canyon road.
    ///
    /// The party enters the canyon from the south while Mia (15), alone at the far end,
    /// runs the length of it to reach them with the enemy captain (199) and his men
    /// (101..109) behind her. Losing Sol (1) or Mia ends the chapter.
    ///
    /// The enemy brings up reinforcements in three groups, each stepping out of a side
    /// passage and spreading out before the next appears: once at the end of the first
    /// turn, and once more when the captain falls -- his dying words call them in. The
    /// original numbered every group's four men the same (201..204, and again for the
    /// second wave); here each wave takes twelve ids of its own so that events and the
    /// creature lookups can tell them apart.
    ///
    /// When the last of them falls Mia joins the party where she stands.
    /// </summary>
    public class Chapter12 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 16, 45 },
            {  2, 16, 49 },
            {  3, 18, 49 },
            {  4, 17, 48 },
            {  5, 19, 48 },
            {  6, 15, 48 },
            {  7, 20, 47 },
            {  8, 18, 47 },
            {  9, 16, 47 },
            { 10, 20, 45 },
            { 11, 21, 46 },
            { 12, 19, 46 },
            { 13, 17, 46 },
            { 14, 18, 45 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The captain's men at the head of the canyon, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 51202, 10, 1,   0 },
            { 102, 51202, 12, 1, 103 },
            { 103, 51202,  9, 2,   0 },
            { 104, 51202, 11, 2, 802 },
            { 105, 51202, 13, 2,   0 },
            { 106, 51202, 10, 3, 902 },
            { 107, 51202, 12, 3,   0 },
            { 108, 51202,  9, 4,   0 },
            { 109, 51202, 13, 4, 102 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 51201;
        private const int BossDropItemId = 217;
        private static readonly FDPosition BossPost = FDPosition.At(11, 4);

        /// <summary>Mia, running for the party's end of the canyon.</summary>
        private const int MiaId = 15;
        private static readonly FDPosition MiaEntry = FDPosition.At(11, 8);
        private static readonly FDPosition MiaEscape = FDPosition.At(16, 45);

        /// <summary>
        /// One wave of reinforcements: three groups of four, as (spawn x, spawn y,
        /// definition, drop item, via x, via y, stop x, stop y). Each group appears
        /// stacked on its spawn tile and walks apart in parallel; a "via" of 0 is a
        /// straight one-leg walk. The groups come in the original's order -- the east
        /// passage, the west passage, then the north -- each after the previous group
        /// has finished walking.
        /// </summary>
        private const int GroupSize = 4;
        private static readonly int[,] Wave = new int[,]
        {
            { 26, 39, 51202,   0,  0,  0, 25, 39 },
            { 26, 39, 51202, 115,  0,  0, 26, 40 },
            { 26, 39, 51203, 103,  0,  0, 27, 39 },
            { 26, 39, 51204, 105, 26, 40, 24, 40 },

            {  2, 31, 51202,   0,  0,  0,  3, 31 },
            {  2, 31, 51202, 901,  0,  0,  2, 32 },
            {  2, 31, 51203, 108,  0,  0,  1, 31 },
            {  2, 31, 51204, 112,  2, 32,  4, 32 },

            { 21, 10, 51202,   0, 21, 11, 20, 11 },
            { 21, 10, 51202, 114,  0,  0, 21, 12 },
            { 21, 10, 51203, 901, 21, 11, 22, 11 },
            { 21, 10, 51204,   0, 21, 12, 19, 12 },
        };

        private const int FirstWaveFirstId = 201;
        private const int SecondWaveFirstId = 221;

        public Chapter12(GameMain gameMain) : base(gameMain, 12)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:1", which fired once the last friend had acted in turn
            // 1, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 1, CreatureFaction.Npc, (gameMain) => Reinforce(gameMain, FirstWaveFirstId));

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, MiaId, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, BossId, bossDying);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4]);
            }
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost, BossDropItemId);

            AddCreatureToMap(gameMain, CreatureFaction.Npc, MiaId, MiaId, MiaEntry);
            SetCreatureAiEscape(gameMain, MiaId, MiaEscape);

            // Talking
            PushConversationsActivities(gameMain, 12, 1, 1, 11);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>The captain's last words call the second wave in.</summary>
        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 12, 2, 1, 1);

            Reinforce(gameMain, SecondWaveFirstId);
        };

        /// <summary>
        /// Queues one wave: the first group is spawned and walked, and only then the
        /// next, exactly as the original chained reinforcement -> _2 -> _3 behind each
        /// group's main walk. Every step is queued as an activity, so a wave called from
        /// the captain's dying words plays after them.
        /// </summary>
        private static void Reinforce(GameMain gameMain, int firstId)
        {
            ReinforceGroup(gameMain, firstId, 0);
        }

        private static void ReinforceGroup(GameMain gameMain, int firstId, int group)
        {
            gameMain.PushActivity((gameMain) =>
            {
                ActivityBase[] walks = new ActivityBase[GroupSize];
                for (int k = 0; k < GroupSize; k++)
                {
                    int row = group * GroupSize + k;
                    int creatureId = firstId + row;
                    FDPosition spawn = FDPosition.At(Wave[row, 0], Wave[row, 1]);
                    FDPosition stop = FDPosition.At(Wave[row, 6], Wave[row, 7]);

                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, creatureId, Wave[row, 2], spawn, Wave[row, 3]);

                    FDMovePath path = Wave[row, 4] > 0
                        ? FDMovePath.Create(spawn, FDPosition.At(Wave[row, 4], Wave[row, 5]), stop)
                        : FDMovePath.Create(spawn, stop);
                    walks[k] = ActivityFactory.CreatureWalkActivity(creatureId, path);
                }

                gameMain.PushActivity(new ParallelActivity(walks));

                if ((group + 1) * GroupSize < Wave.GetLength(0))
                {
                    ReinforceGroup(gameMain, firstId, group + 1);
                }
            });
        }

        /// <summary>The last enemy falls: Mia joins the party where she stands.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            RecruitNpc(gameMain, MiaId, MiaId, MiaEscape);

            // Talking
            PushConversationsActivities(gameMain, 12, 2, 2, 10);

            gameMain.OnGameWin();
        };
    }
}
