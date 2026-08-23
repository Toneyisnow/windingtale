using System;
using System.Collections.Generic;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 3 -- the bridge on the road to Sera village.
    ///
    /// Tienuo comes down the bridge from the north with a squad of soldiers on his heels.
    /// The party is at the south end, decides the odds are unfair, and wades in. Tienuo
    /// fights as an npc; if he is still standing when the last soldier falls he joins the
    /// party, and if he is not the chapter closes on a different scene.
    /// </summary>
    public class Chapter3 : ChapterEvents
    {
        /// <summary>Tienuo -- an npc for this battle, a party member after it.</summary>
        private const int TienuoId = 7;

        /// <summary>Where Tienuo stops after walking down the bridge.</summary>
        private static readonly FDPosition TienuoStand = FDPosition.At(9, 12);

        /// <summary>
        /// The soldiers chasing Tienuo, as (id, spawn x, spawn y, drop item). Each walks
        /// six tiles south of where it appears -- the original derived the destination
        /// that way rather than listing it.
        /// </summary>
        private static readonly int[,] Chasers = new int[,]
        {
            { 21,  9, 4,   0 },
            { 22, 10, 4, 801 },
            { 23,  8, 3,   0 },
            { 24, 11, 3,   0 },
            { 25,  9, 2, 101 },
            { 26, 10, 2,   0 },
            { 27,  7, 2,   0 },
            { 28, 12, 2, 901 },
        };

        /// <summary>How far south a chaser walks from where it appears.</summary>
        private const int ChaserWalkSouth = 6;

        public Chapter3(GameMain gameMain) : base(gameMain, 3)
        {
            int eventId = 0;
            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Enemy Turn:3", which fired once the enemy had finished turn 3,
            // i.e. the opening of turn 4 here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 4, CreatureFaction.Friend, turn4);

            LoadDyingEvent(++eventId, TienuoId, tienuoDyingMessage);
            LoadDyingEvent(++eventId, 40, bossDyingMessage);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            // The party has come up the road and stops at the south end of the bridge.
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 1, 1, FDPosition.At(8, 23));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 2, 2, FDPosition.At(10, 23));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 3, 3, FDPosition.At(9, 24));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 4, 4, FDPosition.At(11, 24));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 5, 5, FDPosition.At(10, 25));
            AddCreatureToMap(gameMain, CreatureFaction.Friend, 6, 6, FDPosition.At(8, 25));

            // Talking
            PushConversationsActivities(gameMain, 3, 1, 1, 2);

            gameMain.PushActivity((gameMain) =>
            {
                // Tienuo comes onto the bridge from the north bank, out of breath and
                // sure he has shaken them off.
                AddCreatureToMap(gameMain, CreatureFaction.Npc, TienuoId, 7, FDPosition.At(9, 6));
            });

            // Frame the middle of the bridge, so his walk down it is on screen.
            gameMain.PushActivity(new SlideCursorActivity(15, 14));

            gameMain.PushActivity(
                ActivityFactory.CreatureWalkActivity(TienuoId, FDMovePath.Create(FDPosition.At(9, 6), TienuoStand))
            );

            // Talking
            PushConversationsActivities(gameMain, 3, 1, 3, 3);

            gameMain.PushActivity((gameMain) =>
            {
                // He has not. The squad comes over the north bank behind him.
                for (int i = 0; i < Chasers.GetLength(0); i++)
                {
                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, Chasers[i, 0], 50301,
                        FDPosition.At(Chasers[i, 1], Chasers[i, 2]), Chasers[i, 3]);
                }
            });

            gameMain.PushActivity(new ParallelActivity(ChaserWalkIn()));

            // Talking
            PushConversationsActivities(gameMain, 3, 1, 4, 14);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        private Action<GameMain> turn4 = (gameMain) =>
        {
            // The captain has come for Tienuo. With Tienuo already dead there is nobody
            // left to come for, and the reinforcements never arrive.
            if (IsTienuoLost(gameMain))
            {
                return;
            }

            // The second squad comes down off the north bank...
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 29, 50301, FDPosition.At(7, 1));
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 30, 50301, FDPosition.At(9, 1));
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 31, 50301, FDPosition.At(11, 1));
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 32, 50301, FDPosition.At(8, 2), 101);
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 33, 50301, FDPosition.At(10, 2));
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 34, 50301, FDPosition.At(9, 3), 901);

            // ...and the captain's own comes up behind the party, off the south bank.
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 35, 50301, FDPosition.At(7, 26), 801);
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 36, 50301, FDPosition.At(8, 25));
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 37, 50301, FDPosition.At(9, 24));
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 38, 50301, FDPosition.At(10, 25), 101);
            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 39, 50301, FDPosition.At(11, 26));

            AddCreatureAroundToMap(gameMain, CreatureFaction.Enemy, 40, 50302, FDPosition.At(9, 26), 202);

            // Talking
            PushConversationsActivities(gameMain, 3, 2, 1, 7);
        };

        private Action<GameMain> tienuoDyingMessage = (gameMain) =>
        {
            PushConversationsActivities(gameMain, 3, 99, 1, 1);
        };

        private Action<GameMain> bossDyingMessage = (gameMain) =>
        {
            // Sequence 4 opens on the captain going down, which is why the closing scene
            // below picks the same conversation up at 2.
            PushConversationsActivities(gameMain, 3, 4, 1, 1);
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            if (IsTienuoLost(gameMain))
            {
                // They fought for him and lost him anyway.
                PushConversationsActivities(gameMain, 3, 3, 1, 5);
            }
            else
            {
                PushConversationsActivities(gameMain, 3, 4, 2, 11);
            }

            // The chapter is over once the last enemy falls. OnGameWin queues itself
            // behind the conversation above, so the closing lines play out first.
            gameMain.OnGameWin();
        };

        /// <summary>
        /// Tienuo walks out of the battle as a party member. He is on the field as an npc
        /// and only Friends are carried to the next chapter, so he has to change sides
        /// before the record is built -- and an npc is an FDAICreature with a fixed
        /// faction, so the only way across is to take him off the map and put him back.
        ///
        /// He comes back fresh from his definition rather than carrying his battle damage,
        /// which is what the original did too: it built a new FDFriend from definition 7
        /// and appended it to the friend list.
        /// </summary>
        internal override void AdjustFriendsAfterWon()
        {
            if (IsTienuoLost(gameMain))
            {
                return;
            }

            FDCreature tienuo = gameMain.gameMap.Map.GetCreatureById(TienuoId);
            FDPosition position = tienuo != null ? tienuo.Position : TienuoStand;

            gameMain.gameMap.RemoveCreature(TienuoId);
            AddCreatureToMap(gameMain, CreatureFaction.Friend, TienuoId, 7, position);
        }

        /// <summary>
        /// Whether Tienuo fell during this battle. The unrevived are skipped for the same
        /// reason CreatureDeadEvent skips them, even though a guest who joins here can
        /// never be one of them.
        /// </summary>
        private static bool IsTienuoLost(GameMain gameMain)
        {
            return gameMain.gameMap.Map.DeadCreatures.Exists(
                creature => creature.Id == TienuoId && !creature.IsUnrevived);
        }

        /// <summary>
        /// The eight chasers walking onto the bridge together, each straight down its own
        /// column. The bridge is clear between the north bank and where they stop, so a
        /// two-point path is enough.
        /// </summary>
        private static ActivityBase[] ChaserWalkIn()
        {
            List<ActivityBase> walks = new List<ActivityBase>();

            for (int i = 0; i < Chasers.GetLength(0); i++)
            {
                int x = Chasers[i, 1];
                int y = Chasers[i, 2];
                walks.Add(ActivityFactory.CreatureWalkActivity(Chasers[i, 0],
                    FDMovePath.Create(FDPosition.At(x, y), FDPosition.At(x, y + ChaserWalkSouth))));
            }

            return walks.ToArray();
        }
    }
}
