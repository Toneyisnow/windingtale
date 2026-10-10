using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;

namespace WindingTale.UI.Audio
{
    /// <summary>The fixed sound effect hooks; SoundEffectTable says what each one plays.</summary>
    public enum SoundEffect
    {
        // ---- Battle animation (GameBattleScene) ----

        /// <summary>A strike lands, on the attack's hit frame (AttackRunner).</summary>
        BattleHit,
        /// <summary>A strike misses, on the attack's hit frame (AttackRunner).</summary>
        BattleMiss,
        /// <summary>The attacker's wind-up: the attack animation reaching its second frame (FightBody).</summary>
        BattleWindUp,

        // ---- Battle field ----

        /// <summary>A creature's death explosion (CreatureDying).</summary>
        CreatureDeath,

        // ---- Shop ----

        /// <summary>An item bought: the money paid (ShoppingScene.ExecutePurchase).</summary>
        ShopPurchase,
        /// <summary>A fallen party member brought back at the church (ShoppingScene.ExecuteRevive).</summary>
        Revive,

        // ---- Controls ----

        /// <summary>The map cursor moves one tile (PlayerInterface).</summary>
        MapCursorMove,
        /// <summary>A map menu pops up (GameMap.ShowMenu).</summary>
        MapMenuOpen,
        /// <summary>A map menu closes without another opening in its place (GameMap.CloseMenu).</summary>
        MapMenuClose,
        /// <summary>The creature detail dialog opens (CreatureInfoDialog.Init).</summary>
        CreatureDialogOpen,
        /// <summary>The creature detail dialog closes (CreatureInfoDialog).</summary>
        CreatureDialogClose,
        /// <summary>A character typed out in a talk dialog (TalkDialog).</summary>
        TalkTyping,
        /// <summary>The highlight moves inside any dialog, title-screen record list included.</summary>
        DialogCursorMove,
        /// <summary>Confirm / select inside any dialog.</summary>
        DialogConfirm,
        /// <summary>Cancel / close inside any dialog.</summary>
        DialogCancel,
    }

    /// <summary>What a walking creature's footsteps sound like.</summary>
    public enum WalkSurface
    {
        Normal,
        Snow,
        Stone,
        Marsh,
        Fly,
    }

    /// <summary>The three kinds of item use: 补给型, 增强型, 破坏型.</summary>
    public enum ItemUseKind
    {
        Supply,
        Enhance,
        Destroy,
    }

    /// <summary>
    /// Plays the one-shot sound effects. Every hook in the game calls in here; what each
    /// one sounds like is set in SoundEffectTable. Lives on one object that survives scene
    /// changes, made the first time a sound is played.
    /// </summary>
    public class SoundEffects : MonoBehaviour
    {
        private const float Volume = 0.8f;

        private static SoundEffects instance = null;

        private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> missingClips = new HashSet<string>();

        private AudioSource audioSource = null;

        // A menu close waiting out MenuCloseGraceSeconds; a menu open in the meantime drops it.
        private float pendingMenuCloseAt = -1f;

        private float lastTypingTime = -1f;

        /// <summary>Plays one of the fixed hooks.</summary>
        public static void Play(SoundEffect effect)
        {
            switch (effect)
            {
                case SoundEffect.MapMenuClose:
                    // Held back: if another menu opens right away, only the open is heard.
                    GetInstance()?.DeferMenuClose();
                    return;

                case SoundEffect.MapMenuOpen:
                    if (instance != null)
                    {
                        instance.pendingMenuCloseAt = -1f;
                    }
                    break;

                case SoundEffect.TalkTyping:
                    SoundEffects player = GetInstance();
                    if (player == null || Time.unscaledTime - player.lastTypingTime < SoundEffectTable.TalkTypingMinInterval)
                    {
                        return;
                    }
                    player.lastTypingTime = Time.unscaledTime;
                    break;
            }

            SoundEffectTable.Effects.TryGetValue(effect, out string clipName);
            PlayClip(clipName);
        }

        /// <summary>
        /// A strike that lands: the striker's own clip by fight animation id, else its weapon's
        /// own, else the weapon category's, else the common BattleHit.
        /// </summary>
        public static void PlayBattleHit(int strikerAnimationId, AttackItemDefinition weapon)
        {
            if (SoundEffectTable.HitByAnimationId.TryGetValue(strikerAnimationId, out string ownClip))
            {
                PlayClip(ownClip);
                return;
            }

            if (weapon != null
                && (SoundEffectTable.HitByItemId.TryGetValue(weapon.ItemId, out string clipName)
                    || SoundEffectTable.HitByWeaponCategory.TryGetValue((int)weapon.Category, out clipName)))
            {
                PlayClip(clipName);
                return;
            }

            Play(SoundEffect.BattleHit);
        }

        /// <summary>The attacker's wind-up: its own clip by fight animation id, else the common one.</summary>
        public static void PlayBattleWindUp(int animationId)
        {
            if (SoundEffectTable.WindUpByAnimationId.TryGetValue(animationId, out WindUpSound own))
            {
                PlayClip(own.Clip);
                return;
            }

            Play(SoundEffect.BattleWindUp);
        }

        /// <summary>
        /// The attack animation frame the wind-up starts on, counted from 0 (the clip's first
        /// frame): the attacker's own (WindUpByAnimationId), else DefaultWindUpFrame.
        /// </summary>
        public static int GetBattleWindUpFrameIndex(int animationId)
        {
            int frame = SoundEffectTable.WindUpByAnimationId.TryGetValue(animationId, out WindUpSound own)
                ? own.Frame
                : SoundEffectTable.DefaultWindUpFrame;
            return Mathf.Max(0, frame - 1);
        }

        /// <summary>The start of a spell's battle animation: the spell's own clip, else its kind's.</summary>
        public static void PlayMagicStart(MagicDefinition magic)
        {
            if (magic == null)
            {
                return;
            }

            if (!SoundEffectTable.MagicStartById.TryGetValue(magic.MagicId, out string clipName))
            {
                SoundEffectTable.MagicStartByType.TryGetValue(magic.Type, out clipName);
            }

            PlayClip(clipName);
        }

        /// <summary>A spell cast on the map with no battle animation.</summary>
        public static void PlayFieldMagic(int magicId)
        {
            if (!SoundEffectTable.FieldMagicById.TryGetValue(magicId, out string clipName))
            {
                clipName = SoundEffectTable.FieldMagicDefault;
            }

            PlayClip(clipName);
        }

        /// <summary>A creature using an item on the map.</summary>
        public static void PlayItemUse(ItemUseKind kind)
        {
            SoundEffectTable.ItemUses.TryGetValue(kind, out string clipName);
            PlayClip(clipName);
        }

        /// <summary>
        /// The walking loop for a surface, or null when none is set. A surface left empty in
        /// the table uses the normal footsteps.
        /// </summary>
        public static AudioClip GetWalkClip(WalkSurface surface)
        {
            if (!SoundEffectTable.Walks.TryGetValue(surface, out string clipName) || string.IsNullOrEmpty(clipName))
            {
                SoundEffectTable.Walks.TryGetValue(WalkSurface.Normal, out clipName);
            }

            return LoadClip(clipName);
        }

        /// <summary>What a creature's feet are on: flyers always fly; else the tile decides.</summary>
        public static WalkSurface GetWalkSurface(CreatureDefinition definition, ShapeDefinition shape)
        {
            if (definition != null && definition.CanFly())
            {
                return WalkSurface.Fly;
            }

            if (shape == null)
            {
                return WalkSurface.Normal;
            }

            if (shape.Type == ShapeType.Marsh)
            {
                return WalkSurface.Marsh;
            }

            if (SoundEffectTable.SnowBackgroundIds.Contains(shape.BackgroundId))
            {
                return WalkSurface.Snow;
            }

            if (SoundEffectTable.StoneBackgroundIds.Contains(shape.BackgroundId))
            {
                return WalkSurface.Stone;
            }

            return WalkSurface.Normal;
        }

        private static void PlayClip(string clipName)
        {
            AudioClip clip = LoadClip(clipName);
            if (clip == null)
            {
                return;
            }

            SoundEffects player = GetInstance();
            if (player != null)
            {
                player.audioSource.PlayOneShot(clip, Volume);
            }
        }

        private static AudioClip LoadClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName) || missingClips.Contains(clipName))
            {
                return null;
            }

            if (!clips.TryGetValue(clipName, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>(SoundEffectTable.ClipFolder + clipName);
                if (clip == null)
                {
                    // Said once per name, then silent.
                    Debug.LogWarning("Sound effect clip not found: " + SoundEffectTable.ClipFolder + clipName);
                    missingClips.Add(clipName);
                    return null;
                }

                clips[clipName] = clip;
            }

            return clip;
        }

        /// <summary>
        /// Loads every clip the table names as soon as the game starts, so no hook pays for
        /// loading and decoding its clip on its first play (that was heard as a delay).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void WarmUp()
        {
            if (GetInstance() == null)
            {
                return;
            }

            List<string> names = new List<string>();
            names.AddRange(SoundEffectTable.Effects.Values);
            names.AddRange(SoundEffectTable.Walks.Values);
            foreach (WindUpSound windUp in SoundEffectTable.WindUpByAnimationId.Values)
            {
                names.Add(windUp.Clip);
            }
            names.AddRange(SoundEffectTable.HitByWeaponCategory.Values);
            names.AddRange(SoundEffectTable.HitByItemId.Values);
            names.AddRange(SoundEffectTable.HitByAnimationId.Values);
            names.AddRange(SoundEffectTable.MagicStartByType.Values);
            names.AddRange(SoundEffectTable.MagicStartById.Values);
            names.AddRange(SoundEffectTable.FieldMagicById.Values);
            names.AddRange(SoundEffectTable.ItemUses.Values);
            names.Add(SoundEffectTable.FieldMagicDefault);

            foreach (string clipName in names)
            {
                AudioClip clip = LoadClip(clipName);
                if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                {
                    clip.LoadAudioData();
                }
            }
        }

        private static SoundEffects GetInstance()
        {
            if (instance == null)
            {
                if (!Application.isPlaying)
                {
                    return null;
                }

                GameObject holder = new GameObject("SoundEffects");
                DontDestroyOnLoad(holder);
                instance = holder.AddComponent<SoundEffects>();
                instance.audioSource = holder.AddComponent<AudioSource>();
                instance.audioSource.playOnAwake = false;
                instance.audioSource.spatialBlend = 0f;
            }

            return instance;
        }

        private void DeferMenuClose()
        {
            pendingMenuCloseAt = Time.unscaledTime + SoundEffectTable.MenuCloseGraceSeconds;
        }

        void Update()
        {
            if (pendingMenuCloseAt >= 0f && Time.unscaledTime >= pendingMenuCloseAt)
            {
                pendingMenuCloseAt = -1f;
                SoundEffectTable.Effects.TryGetValue(SoundEffect.MapMenuClose, out string clipName);
                PlayClip(clipName);
            }
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
