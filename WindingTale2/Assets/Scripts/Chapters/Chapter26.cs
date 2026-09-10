using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 26 -- the unknown corridor.
    ///
    /// The ancients' underground passage, a wire-mesh floor over black pits. The party
    /// comes in at the bottom and the mechanical guards wake: twenty (101..120) attack at
    /// once, eighteen more (121..138) hold the middle of the corridor on guard until turn
    /// 10, and the guard captain (139) waits at the top with his two escorts (140, 141)
    /// until turn 19. As long as the captain stands, three more guards drop in at the
    /// top of the corridor every other turn (201..227, turns 2..17). Losing Sol (1),
    /// Youni (2) or Azimegi (31) ends the chapter; the captain's dying words are
    /// conversation 3.
    ///
    /// In the bottom-left corner a dormant guard, Wode, stands in the rock with his
    /// head open. Any party member (1..31) stepping onto (2, 47) in front of him trips
    /// the event once: if it is Youni carrying the metal box (item 813) she puts it in,
    /// the box is spent, and Wode (32) wakes on (2, 46) as a party member with
    /// conversation 1 lines 10..20; anyone else only wonders what it is (conversation 2,
    /// spoken by whoever stepped up). Each creature's event fires once, as in the
    /// original -- Youni stepping there without the box spends hers. Wode's own tile is
    /// painted on the map and erased from the render map; it stays Gap in ShapeMatrix.
    ///
    /// The closing talk depends on Wode: conversation 4 if he joined (alive or fallen),
    /// conversation 5 if not, then conversation 6 for everyone.
    ///
    /// Friends 17..31, which the original never settled, take free tiles behind the
    /// party's formation when the party carries them, since the save keeps only friends
    /// on the map.
    /// </summary>
    public class Chapter26 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1, 16, 45 },
            {  2, 15, 45 },
            {  3, 17, 45 },
            {  4, 14, 45 },
            {  5, 18, 45 },
            {  6, 16, 46 },
            {  7, 15, 46 },
            {  8, 17, 46 },
            {  9, 14, 46 },
            { 10, 18, 46 },
            { 11, 16, 47 },
            { 12, 15, 47 },
            { 13, 17, 47 },
            { 14, 14, 47 },
            { 15, 18, 47 },
            { 16, 19, 47 },
            { 17, 16, 44 },
            { 18, 17, 44 },
            { 19, 19, 46 },
            { 20, 13, 46 },
            { 21, 15, 44 },
            { 22, 19, 45 },
            { 23, 20, 46 },
            { 24, 13, 47 },
            { 25, 12, 46 },
            { 26, 13, 45 },
            { 27, 20, 47 },
            { 28, 18, 43 },
            { 29, 19, 44 },
            { 30, 20, 45 },
            { 31, 21, 46 },
        };

        private const int FirstOptionalFriendId = 10;

        private const int AzimegiId = 31;

        /// <summary>The first host, awake from the start, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] FirstEnemies = new int[,]
        {
            { 101, 52602, 16, 26,   0 },
            { 102, 52605, 14, 34, 111 },
            { 103, 52605, 18, 34,   0 },
            { 104, 52605, 16, 32,   0 },
            { 105, 52605, 15, 31,   0 },
            { 106, 52605, 17, 31,   0 },
            { 107, 52605,  7, 31,   0 },
            { 108, 52605, 25, 31,   0 },
            { 109, 52605,  8, 30,   0 },
            { 110, 52605, 24, 30,   0 },
            { 111, 52605,  1, 34,   0 },
            { 112, 52605, 31, 34,   0 },
            { 113, 52611,  9, 36,   0 },
            { 114, 52611,  8, 35,   0 },
            { 115, 52611, 10, 35,   0 },
            { 116, 52611,  9, 34, 104 },
            { 117, 52611, 23, 37,   0 },
            { 118, 52611, 22, 36,   0 },
            { 119, 52611, 24, 36,   0 },
            { 120, 52611, 23, 35,   0 },
        };

        /// <summary>The second host, on guard in the middle of the corridor until turn 10.</summary>
        private static readonly int[,] SecondEnemies = new int[,]
        {
            { 121, 52602, 11, 13,   0 },
            { 122, 52602, 21, 13,   0 },
            { 123, 52602,  1, 20,   0 },
            { 124, 52602, 31, 20,   0 },
            { 125, 52605, 22, 19,   0 },
            { 126, 52605, 24, 19,   0 },
            { 127, 52605, 10, 19,   0 },
            { 128, 52605,  8, 19,   0 },
            { 129, 52610,  3, 19,   0 },
            { 130, 52610,  4, 18,   0 },
            { 131, 52610,  2, 18, 104 },
            { 132, 52610,  3, 17,   0 },
            { 133, 52610, 29, 19,   0 },
            { 134, 52610, 28, 18,   0 },
            { 135, 52610, 30, 18,   0 },
            { 136, 52610, 29, 17,   0 },
            { 137, 52609, 30, 10,   0 },
            { 138, 52609,  2, 10,   0 },
        };

        /// <summary>The captain and his escort at the top, on guard until turn 19.</summary>
        private static readonly int[,] CaptainGroup = new int[,]
        {
            { 139, 52601, 16, 11,   0 },
            { 140, 52608, 15, 10,   0 },
            { 141, 52608, 17, 10,   0 },
        };

        private const int CaptainId = 139;

        /// <summary>
        /// The guards that drop in at the top while the captain stands: per turn, the
        /// three definitions for (16, 3), (15, 2) and (17, 2). Ids run 201.. in this order.
        /// </summary>
        private static readonly int[,] Reinforcements = new int[,]
        {
            {  2, 52605, 52605, 52605 },
            {  4, 52606, 52607, 52607 },
            {  6, 52603, 52604, 52604 },
            {  8, 52610, 52610, 52610 },
            { 10, 52603, 52604, 52604 },
            { 12, 52610, 52610, 52610 },
            { 15, 52603, 52604, 52604 },
            { 16, 52610, 52610, 52610 },
            { 17, 52603, 52603, 52603 },
        };

        private static readonly FDPosition[] ReinforcementTiles = new FDPosition[]
        {
            FDPosition.At(16, 3), FDPosition.At(15, 2), FDPosition.At(17, 2),
        };

        private const int SecondHostAttackTurn = 10;
        private const int CaptainAttackTurn = 19;

        /// <summary>Wode, the dormant guard, the tile in front of him and the box that wakes him.</summary>
        private const int WodeId = 32;
        private static readonly FDPosition WodeTile = FDPosition.At(2, 46);
        private static readonly FDPosition WodeTrigger = FDPosition.At(2, 47);
        private const int MetalBoxItemId = 813;

        public Chapter26(GameMain gameMain) : base(gameMain, 26)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            for (int i = 0; i < Reinforcements.GetLength(0); i++)
            {
                int wave = i;
                LoadTurnEvent(++eventId, Reinforcements[i, 0], CreatureFaction.Npc,
                    (gameMain) => Reinforce(gameMain, wave));
            }
            LoadTurnEvent(++eventId, CaptainAttackTurn, CreatureFaction.Npc, captainAttack);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, 2, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, AzimegiId, (gameMain) => gameMain.OnGameOver());
            LoadDyingEvent(++eventId, CaptainId, captainDying);

            for (int creatureId = 1; creatureId <= 31; creatureId++)
            {
                LoadReachPositionEvent(++eventId, creatureId, WodeTrigger, onWode);
            }

            LoadTeamEvent(++eventId, CreatureFaction.Enemy, enemyClear);
        }

        private static void AddEnemies(GameMain gameMain, int[,] enemies, AITypes? aiType = null)
        {
            for (int i = 0; i < enemies.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, enemies[i, 0], enemies[i, 1],
                    FDPosition.At(enemies[i, 2], enemies[i, 3]), enemies[i, 4], aiType);
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
            AddEnemies(gameMain, SecondEnemies, AITypes.AIType_Guard);
            AddEnemies(gameMain, CaptainGroup, AITypes.AIType_Guard);

            // Talking
            PushConversationsActivities(gameMain, 26, 1, 1, 9);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>
        /// Every other turn while the captain stands, three more guards drop in at the
        /// top of the corridor. On turn 10 the second host also joins the attack.
        /// </summary>
        private static void Reinforce(GameMain gameMain, int wave)
        {
            if (gameMain.gameMap.Map.GetCreatureById(CaptainId) == null)
            {
                return;
            }

            int firstId = 201 + wave * 3;
            for (int k = 0; k < 3; k++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Enemy, firstId + k, Reinforcements[wave, 1 + k],
                    ReinforcementTiles[k]);
            }

            if (Reinforcements[wave, 0] == SecondHostAttackTurn)
            {
                SetAiType(gameMain, SecondEnemies, AITypes.AIType_Aggressive);
            }
        }

        /// <summary>Turn 19: the captain and his escort come down.</summary>
        private Action<GameMain> captainAttack = (gameMain) =>
        {
            if (gameMain.gameMap.Map.GetCreatureById(CaptainId) == null)
            {
                return;
            }

            SetAiType(gameMain, CaptainGroup, AITypes.AIType_Aggressive);
        };

        private Action<GameMain> captainDying = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 26, 3, 1, 1);
        };

        /// <summary>Someone has stepped up to the dormant guard.</summary>
        private Action<GameMain> onWode = (gameMain) =>
        {
            FDCreature creature = gameMain.gameMap.Map.GetCreatureAt(WodeTrigger);
            if (creature == null)
            {
                return;
            }

            int boxIndex = creature.Items != null ? creature.Items.IndexOf(MetalBoxItemId) : -1;
            if (creature.Id == 2 && boxIndex >= 0)
            {
                // Youni puts the box in, and Wode wakes as one of the party.
                creature.RemoveItemAt(boxIndex);
                AddCreatureToMap(gameMain, CreatureFaction.Friend, WodeId, WodeId, WodeTile);

                // Talking
                PushConversationsActivities(gameMain, 26, 1, 10, 20);
            }
            else
            {
                // Whoever it was only wonders at the thing -- the line is theirs, not the
                // speaker the conversation table names.
                gameMain.PushActivity(new TalkActivity(Conversation.Create(26, 2, 1), creature.Id));
            }
        };

        private Action<GameMain> enemyClear = (gameMain) =>
        {
            bool hasWode = gameMain.gameMap.Map.GetCreatureById(WodeId) != null
                || gameMain.gameMap.Map.DeadCreatures.Exists(c => c.Id == WodeId);

            // Talking
            if (hasWode)
            {
                PushConversationsActivities(gameMain, 26, 4, 2, 24);
            }
            else
            {
                PushConversationsActivities(gameMain, 26, 5, 1, 8);
            }
            PushConversationsActivities(gameMain, 26, 6, 1, 10);

            gameMain.OnGameWin();
        };
    }
}
