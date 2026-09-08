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
    /// Chapter 17 -- the island shrine.
    ///
    /// The party lands in four groups of four at the four ends of the island's
    /// causeways, with the enemy dug in around the shrine at its centre and its master
    /// (199) in the middle. The outer ring (101..107) and the inner ring (118..135)
    /// guard their posts and the master keeps his own, until turn 15 when everything
    /// closes on the party. On turn 4 eight islanders (201..208) land at the south
    /// causeway to help. Losing Sol (1) ends the chapter.
    ///
    /// Midi (18) matters to the story here. If she joined in chapter 16 she is in the
    /// party and fights; if not, the original still put her on the field for this one
    /// battle -- her farewell is the ending -- and took her out of the party again after
    /// the win. Both cases stand her at (24, 35). The original left a Midi carried in
    /// the party off the field (settleFriend was only called for 1..16), which in this
    /// port would drop her from the party, so she is settled at the same tile instead.
    ///
    /// When the last enemy falls, Ailan (19) walks down from the north end of the
    /// island and joins the party. The islanders were all numbered 201 in the original;
    /// here they are 201..208.
    /// </summary>
    public class Chapter17 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 24, 37 },
            {  2, 25, 37 },
            {  3, 24, 38 },
            {  4, 25, 38 },
            {  5, 23,  7 },
            {  6, 24,  7 },
            {  7, 23,  8 },
            {  8, 24,  8 },
            {  9,  7, 24 },
            { 10,  8, 24 },
            { 11,  7, 25 },
            { 12,  8, 25 },
            { 13, 39, 23 },
            { 14, 40, 23 },
            { 15, 39, 24 },
            { 16, 40, 24 },
        };

        private const int FirstOptionalFriendId = 10;

        private const int MidiId = 18;
        private static readonly FDPosition MidiPost = FDPosition.At(24, 35);

        /// <summary>The garrison, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 51703, 18, 30,   0 },
            { 102, 51703, 30, 30,   0 },
            { 103, 51703, 15, 24, 103 },
            { 104, 51703, 32, 25,   0 },
            { 105, 51703, 20, 22,   0 },
            { 106, 51703, 28, 22,   0 },
            { 107, 51703, 24, 18,   0 },
            { 108, 51705, 14, 28,   0 },
            { 109, 51705, 33, 28,   0 },
            { 110, 51705, 23, 28,   0 },
            { 111, 51705, 24, 28,   0 },
            { 112, 51705, 25, 28,   0 },
            { 113, 51705, 14, 21, 103 },
            { 114, 51705, 33, 21,   0 },
            { 115, 51705, 23, 13,   0 },
            { 116, 51705, 24, 13,   0 },
            { 117, 51705, 25, 13,   0 },
            { 118, 51704, 23, 31, 103 },
            { 119, 51704, 26, 31,   0 },
            { 120, 51704, 22, 26,   0 },
            { 121, 51704, 26, 26, 903 },
            { 122, 51704, 22, 23,   0 },
            { 123, 51704, 26, 23, 104 },
            { 124, 51704, 13, 25,   0 },
            { 125, 51704, 13, 23,   0 },
            { 126, 51704, 35, 25,   0 },
            { 127, 51704, 35, 23, 103 },
            { 128, 51704, 21, 17,   0 },
            { 129, 51704, 27, 17,   0 },
            { 130, 51704, 24, 15,   0 },
            { 131, 51704, 21, 14,   0 },
            { 132, 51704, 27, 14,   0 },
            { 133, 51702, 24, 21, 803 },
            { 134, 51702, 23, 20,   0 },
            { 135, 51702, 25, 20,   0 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 51701;
        private const int BossDropItemId = 120;
        private static readonly FDPosition BossPost = FDPosition.At(24, 19);

        /// <summary>The outer ring (101..107) and the inner ring (118..135) guard their posts until turn 15.</summary>
        private const int FirstOuterId = 101;
        private const int LastOuterId = 107;
        private const int FirstInnerId = 118;
        private const int LastInnerId = 135;
        private const int AttackTurn = 15;

        /// <summary>The islanders, as (id, x, y), landing at the south causeway on turn 4.</summary>
        private const int IslanderDefinitionId = 51706;
        private static readonly int[,] Islanders = new int[,]
        {
            { 201, 23, 43 },
            { 202, 24, 43 },
            { 203, 25, 43 },
            { 204, 23, 44 },
            { 205, 24, 44 },
            { 206, 25, 44 },
            { 207, 23, 45 },
            { 208, 25, 45 },
        };
        private const int IslandersTurn = 4;

        /// <summary>Ailan (19) comes down from the north end of the island at the end.</summary>
        private const int AilanId = 19;
        private static readonly FDPosition AilanEntry = FDPosition.At(24, 1);
        private static readonly FDPosition AilanStop = FDPosition.At(24, 7);

        /// <summary>Whether Midi is here for this battle only and leaves the party after the win.</summary>
        private bool midiLeavesAfterWin = false;

        public Chapter17(GameMain gameMain) : base(gameMain, 17)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, Turn1);
            LoadTurnEvent(++eventId, IslandersTurn, CreatureFaction.Npc, IslandersLand);
            LoadTurnEvent(++eventId, AttackTurn, CreatureFaction.Npc, GarrisonAttack);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, EnemyClear);

            LoadDyingEvent(++eventId, BossId, BossDying);
        }

        private void Turn1(GameMain gameMain)
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);

            // Midi: from the party if she joined in chapter 16, otherwise a guest for
            // this battle only.
            midiLeavesAfterWin = !PartyCarries(gameMain, MidiId);
            AddCreatureToMap(gameMain, CreatureFaction.Friend, MidiId, MidiId, MidiPost);

            for (int i = 0; i < Enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                    FDPosition.At(Enemies[i, 2], Enemies[i, 3]), Enemies[i, 4]);
            }
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost, BossDropItemId,
                AITypes.AIType_Defensive);

            for (int id = FirstOuterId; id <= LastOuterId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Guard);
            }
            for (int id = FirstInnerId; id <= LastInnerId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Guard);
            }

            // Talking
            PushConversationsActivities(gameMain, 17, 1, 1, 12);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        }

        /// <summary>Turn 4: the islanders land at the south causeway.</summary>
        private void IslandersLand(GameMain gameMain)
        {
            for (int i = 0; i < Islanders.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, Islanders[i, 0], IslanderDefinitionId,
                    FDPosition.At(Islanders[i, 1], Islanders[i, 2]));
            }

            // Talking
            PushConversationsActivities(gameMain, 17, 2, 1, 3);
        }

        /// <summary>Turn 15: both rings and their master leave their posts and attack.</summary>
        private void GarrisonAttack(GameMain gameMain)
        {
            for (int id = FirstOuterId; id <= LastOuterId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
            for (int id = FirstInnerId; id <= LastInnerId; id++)
            {
                SetCreatureAiType(gameMain, id, AITypes.AIType_Aggressive);
            }
            SetCreatureAiType(gameMain, BossId, AITypes.AIType_Aggressive);
        }

        private void BossDying(GameMain gameMain)
        {
            // Talking
            PushConversationsActivities(gameMain, 17, 3, 1, 6);
        }

        /// <summary>
        /// The last enemy falls: Midi's scene -- one for a party member, another for the
        /// guest taking her leave -- then Ailan walks down from the north and joins.
        /// </summary>
        private void EnemyClear(GameMain gameMain)
        {
            // Talking
            if (!midiLeavesAfterWin)
            {
                PushConversationsActivities(gameMain, 17, 4, 1, 4);
            }
            else
            {
                PushConversationsActivities(gameMain, 17, 5, 1, 3);
            }

            // Ailan appears only once that scene is over -- the original chained this
            // as enemyClear2.
            gameMain.PushActivity((gameMain) =>
            {
                AddCreatureToMap(gameMain, CreatureFaction.Friend, AilanId, AilanId, AilanEntry);
                gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(AilanId,
                    FDMovePath.Create(AilanEntry, AilanStop)));

                // Talking
                PushConversationsActivities(gameMain, 17, 6, 1, 19);

                gameMain.OnGameWin();
            });
        }

        /// <summary>A guest Midi goes her own way: she is not carried to the next chapter.</summary>
        internal override void AdjustFriendsAfterWon()
        {
            if (!midiLeavesAfterWin)
            {
                return;
            }

            gameMain.gameMap.RemoveCreature(MidiId);
            gameMain.gameMap.Map.DeadCreatures.RemoveAll(c => c.Id == MidiId);
        }
    }
}
