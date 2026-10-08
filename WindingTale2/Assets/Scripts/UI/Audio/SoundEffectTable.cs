using System.Collections.Generic;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;

namespace WindingTale.UI.Audio
{
    /// <summary>
    /// Every sound effect hook in the game, and which clip it plays. This is the one file to
    /// fill in: each value is the name of a clip under Resources/Audios/Effects, without the
    /// extension ("sfx_control_cursor"). A hook left at null -- or naming a clip that does not
    /// exist -- is simply silent, so hooks can be filled in one at a time.
    /// </summary>
    public static class SoundEffectTable
    {
        /// <summary>Where the clips live, under Resources.</summary>
        public const string ClipFolder = "Audios/Effects/";

        /// <summary>The fixed hooks. See SoundEffect for where each one fires.</summary>
        public static readonly Dictionary<SoundEffect, string> Effects = new Dictionary<SoundEffect, string>
        {
            // Battle animation
            { SoundEffect.BattleHit, "sfx_fight_hit_all" },
            { SoundEffect.BattleMiss, "sfx_fight_miss_all" },
            { SoundEffect.BattleWindUp, null },

            // Battle field
            { SoundEffect.CreatureDeath, "sfx_fight_dead_final" },

            // Shop
            { SoundEffect.ShopPurchase, "sfx_control_money" },
            { SoundEffect.Revive, "sfx_effect_revive" },

            // Controls
            { SoundEffect.MapCursorMove, "sfx_control_blip" },
            { SoundEffect.MapMenuOpen, "sfx_control_popup" },
            { SoundEffect.MapMenuClose, "sfx_control_close" },
            { SoundEffect.CreatureDialogOpen, "sfx_menu_snap" },
            { SoundEffect.CreatureDialogClose, "sfx_control_close" },
            { SoundEffect.TalkTyping, "sfx_control_typing" },
            { SoundEffect.DialogCursorMove, "sfx_control_cursor_2" },
            { SoundEffect.DialogConfirm, "sfx_control_confirm" },
            { SoundEffect.DialogCancel, "sfx_control_cancel" },
        };

        /// <summary>
        /// A landed strike by the attacker's weapon: the weapon's own clip (HitByItemId), else
        /// its category's (HitByWeaponCategory, AttackItemDefinition.Category), else
        /// SoundEffect.BattleHit -- also what an attacker with no weapon gets.
        /// </summary>
        public static readonly Dictionary<int, string> HitByWeaponCategory = new Dictionary<int, string>
        {
            { 1, "sfx_fight_hit_sword" },    // 剑、刀、匕首
            { 2, "sfx_fight_hit_axe" },      // 斧、锤
            { 3, "sfx_fight_hit_ji" },       // 矛、枪、戟
            { 4, "sfx_fight_hit_bow" },      // 弓
            { 5, "sfx_fight_hit_staff" },    // 棍、杖、锤、链枷
            { 6, "sfx_fight_hit_all_2" },    // 爪、指套、指环
            { 7, "sfx_fight_hit_staff_2" },  // 机器人手臂
            { 8, "sfx_fight_hit_all_3" },    // 怪物自身的攻击（拳、波、炎、触手...）
        };

        public static readonly Dictionary<int, string> HitByItemId = new Dictionary<int, string>
        {
            // Category 8 holds some that are not like the rest of it.
            { 282, "sfx_fight_hit_staff_2" },  // 拳头
            { 283, "sfx_fight_hit_shoot" },    // 激水炮
            { 285, "sfx_fight_hit_shoot" },    // 光束枪
            { 286, "sfx_fight_hit_shoot" },    // 狙击枪
            { 288, "sfx_fight_hit_shoot" },    // 光束炮
            { 293, "sfx_fight_hit_staff_2" },  // 巨岩手臂
            { 295, "sfx_fight_hit_sword" },    // 光束剑
            { 296, "sfx_fight_hit_all_2" },    // 利爪
        };

        /// <summary>
        /// One attacker's own wind-up, by its fight animation id (CreatureDefinition.AnimationId),
        /// overriding SoundEffect.BattleWindUp.
        /// </summary>
        public static readonly Dictionary<int, string> WindUpByAnimationId = new Dictionary<int, string>
        {
            // The whip-wielding enemy (chapter 21).
            { 751, "sfx_fight_whip" },
        };

        /// <summary>The footsteps looped while a creature walks, by what it walks on.</summary>
        public static readonly Dictionary<WalkSurface, string> Walks = new Dictionary<WalkSurface, string>
        {
            { WalkSurface.Normal, "sfx_move_normal" },
            { WalkSurface.Snow, null },
            { WalkSurface.Stone, null },
            { WalkSurface.Marsh, "sfx_control_bubble" },
            { WalkSurface.Fly, "sfx_move_fly" },
        };

        /// <summary>
        /// Ground counted as snow / stone for the footsteps, by the tile's battle background
        /// id ("bg" in Chapter_NN.json's Shapes, ShapeDefinition.BackgroundId) -- the same
        /// id across chapters for the same kind of ground. Marsh comes from the tile type,
        /// and anything not listed is Normal. A surface whose clip is null falls back to Normal.
        /// </summary>
        public static readonly HashSet<int> SnowBackgroundIds = new HashSet<int>
        {
        };

        public static readonly HashSet<int> StoneBackgroundIds = new HashSet<int>
        {
        };

        /// <summary>
        /// The start of a spell's battle animation, by the kind of magic. Only attack spells
        /// (101-109, 113-119) have a battle scene -- every other kind is cast on the map and
        /// sounds through FieldMagicById -- and those already play their own clip on the hit
        /// (MagicEffectDefinition.HitSound), so Attack is left silent here.
        /// </summary>
        public static readonly Dictionary<MagicType, string> MagicStartByType = new Dictionary<MagicType, string>
        {
            { MagicType.Attack, null },
            { MagicType.Recover, null },
            { MagicType.Offensive, null },
            { MagicType.Defensive, null },
            { MagicType.Transmit, null },
        };

        /// <summary>One particular spell's start sound, overriding MagicStartByType: magic id -> clip.</summary>
        public static readonly Dictionary<int, string> MagicStartById = new Dictionary<int, string>
        {
            // The attack spells with no MagicEffectDefinition (so no hit sound of their own):
            // 破龙击、凄惶斩、炽炎刀、音速刃、咒杀术、炽天使、震荡波.
            { 113, "sfx_magic_fireaura" },
            { 114, "sfx_magic_shock" },
            { 115, "sfx_magic_flame" },
            { 116, "sfx_magic_shock" },
            { 117, "sfx_effect_stone" },
            { 118, "sfx_magic_holythunder" },
            { 119, "sfx_magic_earthquake" },
        };

        /// <summary>
        /// A spell cast on the map with no battle animation (碎岩术、地震术、裂地术 ...): magic id
        /// -> clip. A spell not listed plays FieldMagicDefault.
        /// </summary>
        public static readonly Dictionary<int, string> FieldMagicById = new Dictionary<int, string>
        {
            // 碎岩术、地震术、裂地术
            { 110, "sfx_magic_earthquake" },
            { 111, "sfx_magic_earthquake" },
            { 112, "sfx_magic_earthquake" },

            // 治疗术、回复术、再生术、神恩术、风妖精
            { 201, "sfx_effect_heal" },
            { 202, "sfx_effect_heal" },
            { 203, "sfx_effect_heal" },
            { 204, "sfx_effect_heal" },
            { 205, "sfx_effect_heal" },

            // 封咒术、毒击术、麻痹术
            { 301, "sfx_effect_stone" },
            { 302, "sfx_effect_stone" },
            { 303, "sfx_effect_stone" },

            // 魔刃术、魔铠术、风行术、行动术
            { 401, "sfx_effect_enhance" },
            { 402, "sfx_effect_enhance" },
            { 403, "sfx_effect_enhance" },
            { 406, "sfx_effect_enhance" },

            // 解毒术、祛麻术
            { 404, "sfx_effect_heal" },
            { 405, "sfx_effect_heal" },
        };

        public static string FieldMagicDefault = null;

        /// <summary>A creature using an item on the map, by the kind of item (ItemUseKinds).</summary>
        public static readonly Dictionary<ItemUseKind, string> ItemUses = new Dictionary<ItemUseKind, string>
        {
            { ItemUseKind.Supply, "sfx_effect_heal" },
            { ItemUseKind.Enhance, "sfx_effect_enhance" },
            { ItemUseKind.Destroy, null },
        };

        /// <summary>
        /// Which kind each consumable is: 补给型 restores, 增强型 raises a stat for good,
        /// 破坏型 is everything else (the eyes).
        /// </summary>
        public static ItemUseKind KindOf(ItemUseType useType)
        {
            switch (useType)
            {
                case ItemUseType.Hp:
                case ItemUseType.Mp:
                case ItemUseType.AntiFreeze:
                case ItemUseType.AntiPoison:
                    return ItemUseKind.Supply;

                case ItemUseType.HpMax:
                case ItemUseType.MpMax:
                case ItemUseType.Ap:
                case ItemUseType.Dp:
                case ItemUseType.Mv:
                case ItemUseType.Dx:
                    return ItemUseKind.Enhance;

                default:
                    return ItemUseKind.Destroy;
            }
        }

        /// <summary>
        /// The typing "嘟嘟" ticks at most this often (seconds), however fast the text runs;
        /// spaces and line breaks never tick.
        /// </summary>
        public static float TalkTypingMinInterval = 0.16f;

        /// <summary>
        /// A menu closing sounds only if no menu opens within this many seconds: closing one
        /// menu to open the next plays just the open.
        /// </summary>
        public static float MenuCloseGraceSeconds = 0.15f;
    }
}
