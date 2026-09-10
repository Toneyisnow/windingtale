using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 23 -- the twin circles.
    ///
    /// The party comes up from the south of a stone shrine laid out around a central
    /// altar, with Kalisi (28) and Luodeman (29) waiting for them at the foot of the
    /// stairs and joining as friends. After the opening talk the enemy (101..123) and
    /// its master (199) appear around the altar, and on turn 15 and again on turn 18
    /// twelve more (201..224) come in over the two side platforms. Losing Sol (1) or
    /// Xiya (25) ends the chapter; it is won when the last enemy falls.
    ///
    /// The ending sorts out who stays. Kalisi stays only if the party carries item
    /// 814 (chapter 21's reward); otherwise he says his farewell and leaves. Luodeman
    /// leaves if the battle ran past turn 15, or if Midi (18) is with the party --
    /// living or fallen -- below level 25; otherwise he stays. The original's
    /// removeFriend took the leaver out of both the living and the fallen, which is
    /// what RemoveFriend does here, since the save keeps both.
    ///
    /// Friends 17..27, which the original never settled, take free tiles around the
    /// formation when the party carries them, since the save keeps only friends on
    /// the map.
    /// </summary>
    public class Chapter23 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 20, 36 },
            {  2, 21, 36 },
            {  3, 22, 36 },
            {  4, 19, 37 },
            {  5, 20, 37 },
            {  6, 21, 37 },
            {  7, 22, 37 },
            {  8, 23, 37 },
            {  9, 19, 38 },
            { 10, 20, 38 },
            { 11, 21, 38 },
            { 12, 22, 38 },
            { 13, 23, 38 },
            { 14, 20, 39 },
            { 15, 21, 39 },
            { 16, 22, 39 },
            { 17, 20, 35 },
            { 18, 21, 35 },
            { 19, 22, 35 },
            { 20, 18, 36 },
            { 21, 24, 36 },
            { 22, 18, 37 },
            { 23, 24, 37 },
            { 24, 19, 39 },
            { 25, 23, 39 },
            { 26, 18, 35 },
            { 27, 24, 35 },
        };

        private const int FirstOptionalFriendId = 10;

        private const int KalisiId = 28;
        private static readonly FDPosition KalisiPost = FDPosition.At(20, 31);
        private const int LuodemanId = 29;
        private static readonly FDPosition LuodemanPost = FDPosition.At(22, 31);

        /// <summary>The enemy around the altar, as (id, definition, x, y).</summary>
        private static readonly int[,] Enemies = new int[,]
        {
            { 101, 52302, 18, 30 },
            { 102, 52302, 24, 30 },
            { 103, 52302, 15, 22 },
            { 104, 52302, 27, 22 },
            { 105, 52302,  7, 20 },
            { 106, 52302,  8, 20 },
            { 107, 52302,  7, 21 },
            { 108, 52302,  8, 21 },
            { 109, 52302, 34, 20 },
            { 110, 52302, 35, 20 },
            { 111, 52302, 34, 21 },
            { 112, 52302, 35, 21 },
            { 113, 52302, 21, 23 },
            { 114, 52302, 20, 22 },
            { 115, 52302, 22, 22 },
            { 116, 52302, 19, 21 },
            { 117, 52302, 23, 21 },
            { 118, 52302, 18, 20 },
            { 119, 52302, 24, 20 },
            { 120, 52303, 19, 19 },
            { 121, 52303, 23, 19 },
            { 122, 52308, 20, 20 },
            { 123, 52308, 22, 20 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 52301;
        private static readonly FDPosition BossPost = FDPosition.At(21, 18);

        /// <summary>The two waves over the side platforms, as (id, definition, x, y).</summary>
        private static readonly int[,] FirstWave = new int[,]
        {
            { 201, 52304,  8, 16 },
            { 202, 52304,  9, 16 },
            { 203, 52304, 10, 16 },
            { 204, 52304, 32, 16 },
            { 205, 52304, 33, 16 },
            { 206, 52304, 34, 16 },
            { 207, 52306,  8, 17 },
            { 208, 52306,  9, 17 },
            { 209, 52306, 10, 17 },
            { 210, 52306, 32, 17 },
            { 211, 52306, 33, 17 },
            { 212, 52306, 34, 17 },
        };
        private static readonly int[,] SecondWave = new int[,]
        {
            { 213, 52305,  8, 16 },
            { 214, 52305,  9, 16 },
            { 215, 52305, 10, 16 },
            { 216, 52305, 32, 16 },
            { 217, 52305, 33, 16 },
            { 218, 52305, 34, 16 },
            { 219, 52307,  8, 17 },
            { 220, 52307,  9, 17 },
            { 221, 52307, 10, 17 },
            { 222, 52307, 32, 17 },
            { 223, 52307, 33, 17 },
            { 224, 52307, 34, 17 },
        };

        private const int XiyaId = 25;
        private const int MidiId = 18;
        private const int KalisiStaysItemId = 814;
        private const int LuodemanLastTurn = 15;
        private const int LuodemanMidiLevel = 25;

        public Chapter23(GameMain gameMain) : base(gameMain, 23)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the waves, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, 15, CreatureFaction.Npc, (gameMain) => AddWave(gameMain, FirstWave));
            LoadTurnEvent(++eventId, 18, CreatureFaction.Npc, (gameMain) => AddWave(gameMain, SecondWave));

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, XiyaId, (gameMain) => gameMain.OnGameOver());
            LoadDyingEvent(++eventId, BossId, bossDying);

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private static void AddWave(GameMain gameMain, int[,] wave)
        {
            for (int i = 0; i < wave.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, wave[i, 0], wave[i, 1],
                    FDPosition.At(wave[i, 2], wave[i, 3]));
            }
        }

        private Action<GameMain> turn1 = (gameMain) =>
        {
            SettleParty(gameMain, PartyEntry, FirstOptionalFriendId);
            AddCreatureToMap(gameMain, CreatureFaction.Friend, KalisiId, KalisiId, KalisiPost);
            AddCreatureToMap(gameMain, CreatureFaction.Friend, LuodemanId, LuodemanId, LuodemanPost);

            // Talking
            PushConversationsActivities(gameMain, 23, 1, 1, 25);

            // The enemy appears only once the talk is over -- the original chained
            // this as initialBattle2.
            gameMain.PushActivity((gameMain) =>
            {
                for (int i = 0; i < Enemies.GetLength(0); i++)
                {
                    AddCreatureToMap(gameMain, CreatureFaction.Enemy, Enemies[i, 0], Enemies[i, 1],
                        FDPosition.At(Enemies[i, 2], Enemies[i, 3]));
                }
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost);
            });

            // Talking
            PushConversationsActivities(gameMain, 23, 1, 26, 30);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        private Action<GameMain> bossDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 23, 2, 1, 1);
        };

        /// <summary>The last enemy falls; Kalisi and Luodeman each decide whether to stay.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 23, 2, 2, 5);

            if (TeamHasItem(gameMain, KalisiStaysItemId))
            {
                PushConversationsActivities(gameMain, 23, 2, 6, 12);
            }
            else
            {
                PushConversationsActivities(gameMain, 23, 2, 13, 16);
                gameMain.PushActivity((gameMain) => RemoveFriend(gameMain, KalisiId));
            }

            PushConversationsActivities(gameMain, 23, 2, 17, 17);

            FDCreature midi = gameMain.gameMap.Map.GetCreatureById(MidiId)
                ?? gameMain.gameMap.Map.DeadCreatures.Find(c => c.Id == MidiId);

            if (gameMain.gameMap.Map.TurnNo > LuodemanLastTurn)
            {
                PushConversationsActivities(gameMain, 23, 2, 28, 30);
                gameMain.PushActivity((gameMain) => RemoveFriend(gameMain, LuodemanId));
            }
            else if (midi != null && midi.Level < LuodemanMidiLevel)
            {
                PushConversationsActivities(gameMain, 23, 2, 18, 27);
                gameMain.PushActivity((gameMain) => RemoveFriend(gameMain, LuodemanId));
            }
            else
            {
                PushConversationsActivities(gameMain, 23, 2, 32, 36);
            }

            PushConversationsActivities(gameMain, 23, 2, 37, 51);

            gameMain.OnGameWin();
        };

        /// <summary>Takes a friend out of the party for good: off the map, and out of the fallen.</summary>
        private static void RemoveFriend(GameMain gameMain, int creatureId)
        {
            gameMain.gameMap.RemoveCreature(creatureId);
            gameMain.gameMap.Map.DeadCreatures.RemoveAll(c => c.Id == creatureId);
        }

        /// <summary>The party's members, living and fallen -- the original's teamHasItem looked in both lists.</summary>
        private static IEnumerable<FDCreature> TeamMembers(GameMain gameMain)
        {
            foreach (FDCreature creature in gameMain.gameMap.Map.Friends)
            {
                yield return creature;
            }
            foreach (FDCreature creature in gameMain.gameMap.Map.DeadCreatures)
            {
                if (creature.Faction == CreatureFaction.Friend)
                {
                    yield return creature;
                }
            }
        }

        private static bool TeamHasItem(GameMain gameMain, int itemId)
        {
            foreach (FDCreature creature in TeamMembers(gameMain))
            {
                if (creature.Items != null && creature.Items.Contains(itemId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
