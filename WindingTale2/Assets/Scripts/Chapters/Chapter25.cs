using System;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Chapters
{
    /// <summary>
    /// Chapter 25 -- the judgement of fire.
    ///
    /// The lava cave. The party drops in at the bottom of the cave, while up in the
    /// middle of it Saint Kolas (30), king of the dragon-folk, stands with six of his
    /// warriors (201..206) facing the fire demon (199) and his host. Sixteen enemies
    /// (101..116) attack at once; a second host of seventeen (117..133) waits in the
    /// upper cave until turn 5, and the four demon guards (134..137) and the demon
    /// himself hold at the top until turn 10. Eight more dragon warriors (211..218)
    /// arrive as allies on turn 6 at the stone shrine where the party started. A
    /// sword stands in the rock at the very top of the map: when Sol (1) steps in front
    /// of it, at (11, 2), its guardian (301) rises on the sword's tile with the sword
    /// (297) to drop. Losing Sol or Saint Kolas ends the chapter; the demon's dying
    /// words are conversation 3, and when the last enemy falls the demon mage
    /// Azimegi (31) comes out of hiding to join the party.
    ///
    /// Saint Kolas is a Friend from the start, as in the original, so he is carried on
    /// with the party. Friends 17..29, which the original never settled, take free
    /// tiles inside the party's formation when the party carries them, since the save
    /// keeps only friends on the map.
    ///
    /// The original gave all six dragon warriors of the escort the same creature id
    /// (201); they are 201..206 here so the map can tell them apart. Every other id
    /// and position is the original's, including the enemies it stands on the black
    /// edges of the pits and the three (131..133) it stacks on 124..126.
    /// </summary>
    public class Chapter25 : ChapterEvents
    {
        private static readonly int[,] PartyEntry = new int[,]
        {
            {  1,  8, 44 },
            {  2, 10, 44 },
            {  3, 12, 45 },
            {  4, 14, 44 },
            {  5, 17, 44 },
            {  6, 18, 46 },
            {  7, 16, 46 },
            {  8, 14, 46 },
            {  9, 15, 48 },
            { 10, 17, 48 },
            { 11, 12, 48 },
            { 12, 10, 46 },
            { 13,  9, 48 },
            { 14,  7, 49 },
            { 15,  7, 46 },
            { 16, 20, 48 },
            { 17, 11, 46 },
            { 18, 13, 46 },
            { 19,  9, 46 },
            { 20, 11, 48 },
            { 21, 13, 48 },
            { 22, 16, 44 },
            { 23, 12, 44 },
            { 24,  9, 44 },
            { 25, 16, 48 },
            { 26, 18, 48 },
            { 27,  6, 44 },
            { 28, 15, 44 },
            { 29, 13, 44 },
        };

        private const int FirstOptionalFriendId = 10;

        /// <summary>Saint Kolas, king of the dragon-folk: a Friend from the first turn.</summary>
        private const int SaintKolasId = 30;
        private static readonly FDPosition SaintKolasPost = FDPosition.At(12, 28);

        /// <summary>Azimegi, the demon mage, who joins the party after the battle.</summary>
        private const int AzimegiId = 31;
        private static readonly FDPosition AzimegiEntry = FDPosition.At(1, 34);

        /// <summary>The dragon warriors, both the king's escort and the reinforcements.</summary>
        private const int DragonWarriorDefinitionId = 52508;

        private static readonly int[,] Escort = new int[,]
        {
            { 201, 13, 29 },
            { 202, 11, 29 },
            { 203, 10, 28 },
            { 204, 14, 28 },
            { 205, 11, 27 },
            { 206, 13, 27 },
        };

        private static readonly int[,] Reinforcements = new int[,]
        {
            { 211, 13, 50 },
            { 212, 12, 51 },
            { 213, 14, 51 },
            { 214, 11, 52 },
            { 215, 13, 52 },
            { 216, 15, 52 },
            { 217, 12, 53 },
            { 218, 14, 53 },
        };

        /// <summary>The enemy, host by host, as (id, definition, x, y, drop item).</summary>
        private static readonly int[,] FirstEnemies = new int[,]
        {
            { 101, 52502,  9, 35,   0 },
            { 102, 52502, 18, 22,   0 },
            { 103, 52504, 22, 32,   0 },
            { 104, 52504, 23, 33,   0 },
            { 105, 52504, 25, 33,   0 },
            { 106, 52504,  2, 28,   0 },
            { 107, 52504,  3, 27,   0 },
            { 108, 52504,  1, 27,   0 },
            { 109, 52505, 24, 32,   0 },
            { 110, 52505,  2, 26,   0 },
            { 111, 52506,  9, 37, 104 },
            { 112, 52506,  8, 34,   0 },
            { 113, 52506, 10, 34,   0 },
            { 114, 52506, 17, 22, 903 },
            { 115, 52506, 19, 23,   0 },
            { 116, 52506, 19, 21,   0 },
        };

        /// <summary>The second host, holding in the upper cave until turn 5.</summary>
        private static readonly int[,] SecondEnemies = new int[,]
        {
            { 117, 52502, 10, 17,   0 },
            { 118, 52502,  7, 11,   0 },
            { 119, 52503, 24,  9,   0 },
            { 120, 52503, 23,  8,   0 },
            { 121, 52503,  1,  8, 104 },
            { 122, 52503,  2,  7,   0 },
            { 123, 52504, 11, 11,   0 },
            { 124, 52506,  7, 12,   0 },
            { 125, 52506,  6, 10,   0 },
            { 126, 52506,  8, 10,   0 },
            { 127, 52506,  3,  8, 104 },
            { 128, 52506,  1,  6,   0 },
            { 129, 52506, 24,  7,   0 },
            { 130, 52506, 25,  8,   0 },
            { 131, 52506,  7, 12,   0 },
            { 132, 52506,  6, 10,   0 },
            { 133, 52506,  8, 10, 104 },
        };

        /// <summary>The demon's guard at the top of the cave, holding with him until turn 10.</summary>
        private static readonly int[,] ThirdEnemies = new int[,]
        {
            { 134, 52507, 13,  9,   0 },
            { 135, 52507, 12,  8,   0 },
            { 136, 52507, 10,  8, 804 },
            { 137, 52507,  9,  9,   0 },
        };

        private const int BossId = 199;
        private const int BossDefinitionId = 52501;
        private static readonly FDPosition BossPost = FDPosition.At(11, 8);

        private const int SecondHostTurn = 5;
        private const int ReinforcementTurn = 6;
        private const int BossAttackTurn = 10;

        /// <summary>The sword in the rock, its guardian, and the tile in front of it that wakes him.</summary>
        private const int SwordGuardianId = 301;
        private const int SwordGuardianDefinitionId = 52509;
        private const int SwordItemId = 297;
        private static readonly FDPosition SwordTile = FDPosition.At(11, 1);
        private static readonly FDPosition SwordTrigger = FDPosition.At(11, 2);

        public Chapter25(GameMain gameMain) : base(gameMain, 25)
        {
            int eventId = 0;

            // The original was "TurnType_Friend Turn:0" -- the opening cutscene -- and
            // "TurnType_Friend Turn:N" for the rest, which fired once the last friend
            // had acted in turn N, i.e. that turn's Npc phase here. See LoadTurnEvent.
            LoadTurnEvent(++eventId, 1, CreatureFaction.Friend, turn1);
            LoadTurnEvent(++eventId, SecondHostTurn, CreatureFaction.Npc, secondHostAttack);
            LoadTurnEvent(++eventId, ReinforcementTurn, CreatureFaction.Npc, reinforcement);
            LoadTurnEvent(++eventId, BossAttackTurn, CreatureFaction.Npc, bossAttack);

            LoadDeadEvent(++eventId, 1, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, SaintKolasId, (gameMain) => gameMain.OnGameOver());
            LoadDeadEvent(++eventId, BossId, bossDead);

            LoadReachPositionEvent(++eventId, 1, SwordTrigger, onSword);

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

        private static void AddDragonWarriors(GameMain gameMain, int[,] warriors)
        {
            for (int i = 0; i < warriors.GetLength(0); i++)
            {
                AddCreatureToMap(gameMain, CreatureFaction.Npc, warriors[i, 0], DragonWarriorDefinitionId,
                    FDPosition.At(warriors[i, 1], warriors[i, 2]));
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

            // The second host and the demon's guard hold their ground, as does the demon.
            AddEnemies(gameMain, SecondEnemies, AITypes.AIType_StandBy);
            AddEnemies(gameMain, ThirdEnemies, AITypes.AIType_StandBy);
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, BossId, BossDefinitionId, BossPost, 0,
                AITypes.AIType_StandBy);

            // Saint Kolas and his escort, facing the demon in the middle of the cave.
            AddCreatureToMap(gameMain, CreatureFaction.Friend, SaintKolasId, SaintKolasId, SaintKolasPost);
            AddDragonWarriors(gameMain, Escort);

            // Talking
            PushConversationsActivities(gameMain, 25, 1, 1, 23);

            gameMain.PushActivity((gameMain) =>
            {
                // Play background music
                gameMain.PlayBackgroundMusic();
            });
        };

        /// <summary>Turn 5: the second host comes down from the upper cave.</summary>
        private Action<GameMain> secondHostAttack = (gameMain) =>
        {
            SetAiType(gameMain, SecondEnemies, AITypes.AIType_Aggressive);
        };

        /// <summary>Turn 6: eight more dragon warriors arrive at the shrine.</summary>
        private Action<GameMain> reinforcement = (gameMain) =>
        {
            AddDragonWarriors(gameMain, Reinforcements);

            // Talking
            PushConversationsActivities(gameMain, 25, 2, 1, 2);
        };

        /// <summary>Turn 10: the demon and his guard attack.</summary>
        private Action<GameMain> bossAttack = (gameMain) =>
        {
            SetAiType(gameMain, ThirdEnemies, AITypes.AIType_Aggressive);
            SetCreatureAiType(gameMain, BossId, AITypes.AIType_Aggressive);
        };

        private Action<GameMain> bossDead = (gameMain) =>
        {
            // Talking
            PushConversationsActivities(gameMain, 25, 3, 1, 1);
        };

        /// <summary>Sol steps up to the sword in the rock and its guardian rises on it.</summary>
        private Action<GameMain> onSword = (gameMain) =>
        {
            AddCreatureToMap(gameMain, CreatureFaction.Enemy, SwordGuardianId, SwordGuardianDefinitionId,
                SwordTile, SwordItemId);
        };

        /// <summary>The last enemy falls: Azimegi comes out to join the party.</summary>
        private Action<GameMain> enemyClear = (gameMain) =>
        {
            AddCreatureToMap(gameMain, CreatureFaction.Friend, AzimegiId, AzimegiId, AzimegiEntry);

            // Talking
            PushConversationsActivities(gameMain, 25, 4, 1, 18);

            gameMain.OnGameWin();
        };
    }
}
