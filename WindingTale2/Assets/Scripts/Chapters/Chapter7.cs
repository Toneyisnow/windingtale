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
    /// Chapter 7 -- the road to the royal capital.
    ///
    /// The fast road north is also the one the royal garrison watches, and the party walks
    /// straight into the troop that has been sent after the "bandits from Priz harbour".
    /// Their captain will not hear the misunderstanding out, so the whole forest becomes
    /// the battlefield: the party comes on at the southern edge, the garrison is drawn up
    /// in ranks across the north.
    ///
    /// The garrison waits. Every one of the twenty-five soldiers starts on AIType_StandBy
    /// and comes off it in three waves -- the front rank on turn 2, the second on turn 5,
    /// and the captain's own guard on turn 9 -- so the party can pick the fight apart
    /// instead of meeting all of it at once.
    ///
    /// On turn 10 Kaili turns up on the east side of the map with nine soldiers chasing
    /// her, but only if Suoer has pushed far enough north to see it (the original tested
    /// his y against 15). Saving her is optional and she is what the ending hangs on:
    /// alive, she asks to join and the chapter closes on conversation 4; dead or never
    /// summoned, it closes on the shorter conversation 5. She joins as an Npc, so
    /// AdjustFriendsAfterWon is what actually carries her into chapter 8.
    /// </summary>
    public class Chapter7 : ChapterEvents
    {
        /// <summary>Where the party lines up, in creature id order 1..9.</summary>
        private static readonly int[,] PartyEntry = new int[,]
        {
            { 1, 11, 33 },
            { 2, 16, 31 },
            { 3, 13, 32 },
            { 4, 14, 30 },
            { 5, 11, 31 },
            { 6, 12, 29 },
            { 7, 10, 30 },
            { 8,  8, 32 },
            { 9, 15, 33 },
        };

        /// <summary>
        /// The whole garrison, as (id, definition, x, y, drop item), in the order the
        /// original added them. They all go down on AIType_StandBy; which of them wakes up
        /// when is decided by id alone -- 101..110 on turn 2, 111..120 on turn 5, and
        /// 121..125 on turn 9 -- which is why the ids run out of order down the ranks.
        /// </summary>
        private static readonly int[,] Garrison = new int[,]
        {
            { 101, 50702, 12,  8,   0 },
            { 102, 50702, 14,  8,   0 },
            { 111, 50702, 16,  8,   0 },
            { 112, 50702, 18,  8, 102 },

            { 113, 50704, 11,  3,   0 },
            { 114, 50704, 17,  3,   0 },
            { 103, 50704, 12,  4,   0 },
            { 104, 50704, 16,  4, 102 },

            { 115, 50701, 14,  5,   0 },
            { 105, 50701, 13,  6,   0 },
            { 106, 50701, 15,  6,   0 },
            { 116, 50701, 11,  6,   0 },
            { 117, 50701, 17,  6, 801 },
            { 107, 50701, 12,  7,   0 },
            { 108, 50701, 14,  7, 901 },
            { 118, 50701, 16,  7,   0 },

            { 109, 50703, 13,  9,   0 },
            { 110, 50703, 15,  9,   0 },
            { 119, 50703, 17,  9,   0 },
            { 120, 50703, 19,  9, 101 },

            { 122, 50705, 13,  2,   0 },
            { 123, 50705, 15,  2,   0 },
            { 124, 50705, 13,  4,   0 },
            { 125, 50705, 15,  4,   0 },

            { 121, 50706, 14,  3, 317 },
        };

        /// <summary>The captain, who has the last word when he falls.</summary>
        private const int CaptainId = 121;

        /// <summary>
        /// Kaili, the girl the royal soldiers are chasing. She appears at the east edge and
        /// walks in to where the cursor is sent.
        /// </summary>
        private const int KailiId = 10;
        private static readonly FDPosition KailiEntry = FDPosition.At(27, 13);
        private static readonly FDPosition KailiStand = FDPosition.At(22, 13);

        /// <summary>
        /// How far north Suoer has to have got for Kaili's scene to play at all. The
        /// original read his position and gave up when y was past this.
        /// </summary>
        private const int KailiTriggerY = 15;

        /// <summary>
        /// The squad chasing her. All nine come on at the same tile she came from and then
        /// spread out; 159 is their officer, the one who does the talking. Id 151 is left
        /// standing where it lands, exactly as in the original.
        /// </summary>
        private static readonly FDPosition ChaserEntry = FDPosition.At(27, 14);
        private const int ChaserDefinitionId = 50708;
        private const int ChaserOfficerId = 159;
        private const int ChaserOfficerDefinitionId = 50707;
        private const int ChaserOfficerDropItemId = 316;

        public Chapter7(GameMain gameMain) : base(gameMain, 7)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend had
            // acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 2, CreatureFaction.Npc, turn2);
            LoadTurnEvent(++eventId, 5, CreatureFaction.Npc, turn5);
            LoadTurnEvent(++eventId, 9, CreatureFaction.Npc, turn9);
            LoadTurnEvent(++eventId, 10, CreatureFaction.Npc, kailiAppear);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);

            LoadDyingEvent(++eventId, CaptainId, captainDying);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            for (int i = 0; i < PartyEntry.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Friend, PartyEntry[i, 0], PartyEntry[i, 0],
                    FDPosition.At(PartyEntry[i, 1], PartyEntry[i, 2]));
            }

            // Nobody in the garrison moves until its own wave is called.
            for (int i = 0; i < Garrison.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, Garrison[i, 0], Garrison[i, 1],
                    FDPosition.At(Garrison[i, 2], Garrison[i, 3]), Garrison[i, 4],
                    AITypes.AIType_StandBy);
            }

            // Talking
            PushConversationsActivities(gameMain, 7, 1, 1, 8);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>The front rank charges. No dialog goes with any of the three waves.</summary>
        private Action<GameMain> turn2 = (gameMain) => WakeGarrison(gameMain, 101, 110);

        /// <summary>The second rank follows.</summary>
        private Action<GameMain> turn5 = (gameMain) => WakeGarrison(gameMain, 111, 120);

        /// <summary>The captain and his guard leave their post.</summary>
        private Action<GameMain> turn9 = (gameMain) => WakeGarrison(gameMain, 121, 125);

        /// <summary>
        /// Kaili's scene, which is skipped altogether unless Suoer is alive and has come
        /// north far enough to be part of it.
        /// </summary>
        private Action<GameMain> kailiAppear = (gameMain) =>
        {
            FDCreature suoer = gameMain.gameMap.Map.GetCreatureById(1);
            if (suoer == null || suoer.Position == null || suoer.Position.Y > KailiTriggerY)
            {
                return;
            }

            AddCreatureToMap(gameMain, CreatureFaction.Npc, KailiId, KailiId, KailiEntry);

            gameMain.PushActivity(new SlideCursorActivity(KailiStand.X, KailiStand.Y));

            gameMain.PushActivity(ActivityFactory.CreatureWalkActivity(KailiId,
                FDMovePath.Create(KailiEntry, KailiStand)));

            gameMain.PushActivity((gameMain) =>
            {
                // The squad comes out of the trees behind her, all on the one tile.
                for (int i = 1; i <= 8; i++)
                {
                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, 150 + i, ChaserDefinitionId, ChaserEntry);
                }

                AddCreatureToMap(gameMain, CreatureFaction.Enemy, ChaserOfficerId, ChaserOfficerDefinitionId,
                    ChaserEntry, ChaserOfficerDropItemId);
            });

            // The original wrote one main move and then opened a parallel branch per
            // soldier; 151 is given no move at all and stays on the entry tile.
            gameMain.PushActivity(new ParallelActivity(
                new ActivityBase[] {
                    ActivityFactory.CreatureWalkActivity(154, FDMovePath.Create(ChaserEntry, FDPosition.At(25, 14), FDPosition.At(25, 12))),
                    ActivityFactory.CreatureWalkActivity(152, FDMovePath.Create(ChaserEntry, FDPosition.At(23, 14))),
                    ActivityFactory.CreatureWalkActivity(153, FDMovePath.Create(ChaserEntry, FDPosition.At(24, 14), FDPosition.At(24, 13))),
                    ActivityFactory.CreatureWalkActivity(159, FDMovePath.Create(ChaserEntry, FDPosition.At(25, 14))),
                    ActivityFactory.CreatureWalkActivity(155, FDMovePath.Create(ChaserEntry, FDPosition.At(26, 14), FDPosition.At(26, 13))),
                    ActivityFactory.CreatureWalkActivity(156, FDMovePath.Create(ChaserEntry, FDPosition.At(24, 14), FDPosition.At(24, 15))),
                    ActivityFactory.CreatureWalkActivity(157, FDMovePath.Create(ChaserEntry, FDPosition.At(25, 14), FDPosition.At(25, 16))),
                    ActivityFactory.CreatureWalkActivity(158, FDMovePath.Create(ChaserEntry, FDPosition.At(26, 14), FDPosition.At(26, 15)))
                }
            ));

            // Talking
            PushConversationsActivities(gameMain, 7, 2, 1, 9);
        };

        /// <summary>The captain's last words, on the blow that kills him.</summary>
        private Action<GameMain> captainDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 7, 3, 1, 3);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            if (IsKailiSaved(gameMain))
            {
                PushConversationsActivities(gameMain, 7, 4, 1, 8);
            }
            else
            {
                PushConversationsActivities(gameMain, 7, 5, 1, 4);
            }

            // OnGameWin queues itself behind the closing lines, so they play out first.
            gameMain.OnGameWin();
        };

        /// <summary>
        /// Kaili fought as an Npc, and only Friends are carried to the next chapter, so she
        /// has to change sides before the record is written -- the original's
        /// "[[field getFriendList] addObject:kaili]".
        /// </summary>
        internal override void AdjustFriendsAfterWon()
        {
            FDCreature kaili = gameMain.gameMap.Map.GetCreatureById(KailiId);
            if (kaili == null)
            {
                return;
            }

            FDPosition position = kaili.Position ?? KailiStand;

            gameMain.gameMap.RemoveCreature(KailiId);
            AddCreatureToMap(gameMain, CreatureFaction.Friend, KailiId, KailiId, position);
        }

        /// <summary>
        /// Whether Kaili is standing on the map when the last soldier falls -- the
        /// original's "[field getCreatureById:10] != nil", which is false both when she was
        /// killed and when the scene never played at all.
        /// </summary>
        private static bool IsKailiSaved(GameMain gameMain)
        {
            return gameMain.gameMap.Map.GetCreatureById(KailiId) != null;
        }

        /// <summary>Takes one wave of the garrison off standby, by id range.</summary>
        private static void WakeGarrison(GameMain gameMain, int fromId, int toId)
        {
            for (int creatureId = fromId; creatureId <= toId; creatureId++)
            {
                SetCreatureAiType(gameMain, creatureId, AITypes.AIType_Aggressive);
            }
        }
    }
}
