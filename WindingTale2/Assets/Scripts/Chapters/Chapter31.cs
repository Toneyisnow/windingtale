using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 31 -- the good ending.
    ///
    /// Not a battle: the party, sent home by Youni, stands on the soil of Mara once
    /// more and talks (conversation 1, lines 1..16), and the game ends. Chapter 30 hands
    /// over here through GameMain.OnGameEnding, with no village in between.
    ///
    /// The original brought everyone back for the scene, the fallen included, at full
    /// HP -- so any party member the record carries at 0 HP is stood up before the
    /// party is settled. Youni (2) stayed on the fortress and is not settled; Sol (1)
    /// stands apart from the others, as the original placed him last.
    ///
    /// The party comes home by transfer: the cursor and camera open on the middle of
    /// their tiles, a column of light grows on each tile and everyone appears in it
    /// together (ActivityFactory.CreaturesArriveActivity). After line 16 the field fades
    /// out into GameEndingScene, which plays lines 17..24 under the Golden City and the
    /// epilogue, and goes on to each companion's epilogue -- see GameMain.OnGoodEnding.
    /// </summary>
    public class Chapter31 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  3, 16,  9 },
            {  4, 17,  9 },
            {  5, 18,  9 },
            {  6, 19,  9 },
            {  7, 20,  9 },
            {  8, 15, 10 },
            {  9, 16, 10 },
            { 10, 17, 10 },
            { 11, 18, 10 },
            { 12, 19, 10 },
            { 13, 20, 10 },
            { 14, 21, 10 },
            { 15, 15, 11 },
            { 16, 16, 11 },
            { 17, 17, 11 },
            { 18, 18, 11 },
            { 19, 19, 11 },
            { 20, 20, 11 },
            { 21, 21, 11 },
            { 22, 15, 12 },
            { 23, 16, 12 },
            { 24, 17, 12 },
            { 25, 18, 12 },
            { 26, 19, 12 },
            { 27, 20, 12 },
            { 28, 21, 12 },
            { 29, 16, 13 },
            { 30, 17, 13 },
            { 31, 18, 13 },
            { 32, 19, 13 },
            {  1, 18, 16 },
        };

        private const int FirstOptionalFriendId = 10;

        public Chapter31(GameMain gameMain) : base(gameMain, 31)
        {
            int eventId = 0;

            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            // Everyone comes home on their feet.
            List<CreatureMapRecord> party = gameMain.PartyRecord?.Friends;
            if (party != null)
            {
                foreach (CreatureMapRecord friend in party)
                {
                    if (friend.Hp <= 0)
                    {
                        friend.Hp = friend.HpMax;
                    }
                }
            }

            ArriveParty(gameMain);

            // Talking
            PushConversationsActivities(gameMain, 31, 1, 1, 16);

            gameMain.OnGoodEnding();
        };

        /// <summary>
        /// SettleParty's line-up, by transfer: the cursor and camera are put straight on
        /// the middle of the party's tiles, then each one's pillar of light grows and they
        /// all appear in it together.
        /// </summary>
        private static void ArriveParty(GameMain gameMain)
        {
            List<int> members = new List<int>();
            List<FDPosition> tiles = new List<FDPosition>();
            for (int i = 0; i < PartyEntry.GetLength(0); i++)
            {
                int creatureId = PartyEntry[i, 0];
                if (creatureId >= FirstOptionalFriendId && !PartyCarries(gameMain, creatureId))
                {
                    continue;
                }
                members.Add(i);
                tiles.Add(FDPosition.At(PartyEntry[i, 1], PartyEntry[i, 2]));
            }

            FDPosition centre = MiddleOf(tiles);
            gameMain.gameMap.SnapCursorAndCameraTo(centre);

            gameMain.PushActivity(ActivityFactory.CreaturesArriveActivity(centre, (gameMain) =>
            {
                List<FDCreature> arrived = new List<FDCreature>();
                foreach (int i in members)
                {
                    arrived.Add(AddCreatureToMap(gameMain, CreatureFaction.Friend, PartyEntry[i, 0], PartyEntry[i, 0],
                        FDPosition.At(PartyEntry[i, 1], PartyEntry[i, 2])));
                }
                return arrived;
            }));
        }
    }
}
