using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 18 -- the bridge over the chasm.
    ///
    /// A single long bridge spans the chasm between the party's western cliff and the
    /// enemy's eastern one, where the enemy commander (199) waits with his army
    /// (101..129) and a picket (130..134) that holds to the south until turn 8. Lan (20)
    /// and Yue (21) are caught in the middle of the bridge and fall back towards the
    /// party until turn 5, when they turn and fight; losing either of them, or Sol (1),
    /// ends the chapter. On turn 8 the enemy also lands a force (135..156) behind the
    /// party on the western cliff.
    ///
    /// There is no need to clear the field: the chapter is won when the commander
    /// falls, and Lan and Yue join the party where they stand.
    ///
    /// Two departures from the original: it settled only friends 1..16, which would
    /// drop Saikebangle (17), Midi (18) and Ailan (19) from the party in this port, so
    /// they are settled on free tiles inside the formation when the party carries them;
    /// and the force landing behind the party was placed exactly on the party's start
    /// tiles, stacking on anyone still standing there, so here each takes the first
    /// free tile at or next to the one named.
    /// </summary>
    public class Chapter18 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 7,  9 },
            {  2, 7,  7 },
            {  3, 7, 11 },
            {  4, 6, 13 },
            {  5, 5, 11 },
            {  6, 5,  9 },
            {  7, 5,  7 },
            {  8, 6,  5 },
            {  9, 4,  8 },
            { 10, 3, 10 },
            { 11, 4, 12 },
            { 12, 3, 14 },
            { 13, 2, 13 },
            { 14, 2, 11 },
            { 15, 1,  9 },
            { 16, 2, 16 },
            { 17, 2,  9 },
            { 18, 3, 12 },
            { 19, 6,  9 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>The army on the eastern cliff, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 51802, 38,  8,   0 },
            { 102, 51802, 40,  6,   0 },
            { 103, 51802, 40, 11, 103 },
            { 104, 51802, 43, 14,   0 },
            { 105, 51802, 46, 14,   0 },
            { 106, 51803, 35, 15,   0 },
            { 107, 51803, 38, 15, 103 },
            { 108, 51803, 36, 18,   0 },
            { 109, 51803, 39,  2, 103 },
            { 110, 51803, 41,  3,   0 },
            { 111, 51803, 41,  1,   0 },
            { 112, 51804, 36,  8, 104 },
            { 113, 51804, 37,  7,   0 },
            { 114, 51804, 37,  9, 106 },
            { 115, 51804, 38,  6,   0 },
            { 116, 51804, 38, 10,   0 },
            { 117, 51804, 45,  9,   0 },
            { 118, 51804, 46,  8,   0 },
            { 119, 51804, 46, 10,   0 },
            { 120, 51804, 47,  7,   0 },
            { 121, 51804, 47, 11,   0 },
            { 122, 51805, 39,  7, 106 },
            { 123, 51805, 39,  9,   0 },
            { 124, 51805, 49,  7,   0 },
            { 125, 51805, 49,  9,   0 },
            { 126, 51806, 40,  8,   0 },
            { 127, 51806, 44,  6,   0 },
            { 128, 51806, 44, 11, 103 },
            { 129, 51806, 47,  9, 902 },
        };

        /// <summary>The picket to the south, as (id, definition, x, y, drop item): it holds until turn 8.</summary>
        private static readonly int[,] Picket = new int[,]
        {
            { 130, 51802, 40, 24,   0 },
            { 131, 51805, 39, 23, 104 },
            { 132, 51805, 41, 25,   0 },
            { 133, 51806, 41, 23,   0 },
            { 134, 51806, 39, 25, 103 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 51801;
        private static readonly FDPosition BossPost = FDPosition.At(49, 9);

        /// <summary>Lan (20) and Yue (21), caught on the bridge, and where they run to.</summary>
        private const int LanId = 20;
        private const int YueId = 21;
        private static readonly FDPosition LanEntry = FDPosition.At(23, 9);
        private static readonly FDPosition YueEntry = FDPosition.At(23, 8);
        private static readonly FDPosition NpcEscape = FDPosition.At(7, 9);
        private const int NpcFightTurn = 5;

        /// <summary>The force landing behind the party on turn 8, as (id, definition, x, y).</summary>
        private static readonly int[,] Landing = new int[,]
        {
            { 135, 51803, 7,  3 },
            { 136, 51803, 7, 16 },
            { 137, 51807, 7,  8 },
            { 138, 51807, 7, 11 },
            { 139, 51804, 1,  9 },
            { 140, 51804, 2,  9 },
            { 141, 51804, 3,  9 },
            { 142, 51804, 1, 10 },
            { 143, 51804, 2, 10 },
            { 144, 51804, 3, 10 },
            { 145, 51804, 3, 11 },
            { 146, 51804, 4, 10 },
            { 147, 51805, 4, 12 },
            { 148, 51805, 4, 13 },
            { 149, 51805, 4, 14 },
            { 150, 51805, 5, 12 },
            { 151, 51805, 5, 13 },
            { 152, 51805, 5, 14 },
            { 153, 51806, 4,  7 },
            { 154, 51806, 4,  8 },
            { 155, 51806, 5,  7 },
            { 156, 51806, 5,  8 },
        };
        private const int ReinforcementTurn = 8;

        public Chapter18(GameMain gameMain) : base(gameMain, 18)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, NpcFightTurn, CreatureFaction.Npc, npcFightBack);
            LoadTurnEvent(++eventId, ReinforcementTurn, CreatureFaction.Npc, reinforcement);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, LanId, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, YueId, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, BossId, bossDead);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4]);
            }

            for (int i = 0; i < Picket.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Picket[i, 0], Picket[i, 1],
                    FDPosition.At(Picket[i, 2], Picket[i, 3]), Picket[i, 4], AITypes.AIType_StandBy);
            }

            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost);

            AddCreatureToMap(gameMain, CreatureFaction.Npc, LanId, LanId, LanEntry);
            AddCreatureToMap(gameMain, CreatureFaction.Npc, YueId, YueId, YueEntry);
            SetCreatureAiEscape(gameMain, LanId, NpcEscape);
            SetCreatureAiEscape(gameMain, YueId, NpcEscape);

            // Talking
            PushConversationsActivities(gameMain, 18, 1, 1, 24);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 5: Lan and Yue stop running and fight.</summary>
        private Action<GameMain> npcFightBack = (gameMain) =>
        {
            SetCreatureAiType(gameMain, LanId, AITypes.AIType_Aggressive);
            SetCreatureAiType(gameMain, YueId, AITypes.AIType_Aggressive);
        };

        /// <summary>Turn 8: the picket attacks, and a force lands behind the party.</summary>
        private Action<GameMain> reinforcement = (gameMain) =>
        {
            for (int i = 0; i < Picket.GetLength(0); i++)
            {
                SetCreatureAiType(gameMain, Picket[i, 0], AITypes.AIType_Aggressive);
            }

            for (int i = 0; i < Landing.GetLength(0); i++)
            {
                AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, Landing[i, 0], Landing[i, 1],
                    FDPosition.At(Landing[i, 2], Landing[i, 3]));
            }

            // Talking
            PushConversationsActivities(gameMain, 18, 2, 1, 4);
        };

        /// <summary>The commander falls: Lan and Yue join the party where they stand, and the chapter is won.</summary>
        private Action<GameMain> bossDead = (gameMain) =>
        {
            RecruitNpc(gameMain, LanId, LanId, LanEntry);
            RecruitNpc(gameMain, YueId, YueId, YueEntry);

            // Talking
            PushConversationsActivities(gameMain, 18, 3, 1, 22);

            gameMain.OnGameWin();
        };
    }
}
