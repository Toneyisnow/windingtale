using System;
using System.Collections.Generic;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using static System.Collections.Specialized.BitVector32;
using static WindingTale.Core.Objects.FDNpc;

namespace WindingTale.Core.Objects
{
    /// <summary>
    /// All the effects in the game
    /// </summary>
    public enum CreatureEffects
    {
        EnhancedAp = 1,
        EnhancedDp = 2,
        EnhancedDx = 3,
        Forbidden = 4,
        Frozen = 5,
        Poisoned = 6,
    }

    /// <summary>
    /// Should only apply for Enemy and Npc
    /// </summary>
    public enum AITypes
    {
        AIType_Aggressive = 0,
        AIType_Defensive = 1,
        AIType_Guard = 2,
        AIType_Escape = 3,
        AIType_StandBy = 4,
        AIType_Treasure = 5,
        AIType_UnNoticable = 6,     // This one is special, that is set on AI target objects
    }

    public class FDAICreature: FDCreature
    {
        /// <summary>
        /// Settable, because a chapter script may switch a creature's behaviour part way
        /// through the battle (the sleeping guards that wake up on a trigger), and because
        /// a StandBy creature that gets hit turns aggressive -- see WakeUpByAttack.
        /// </summary>
        /// <remarks>
        /// A monk or priest is never Aggressive: whatever a chapter script or a wake-up
        /// asks for, it stays Defensive and keeps to its own side instead of charging.
        /// </remarks>
        public AITypes AIType
        {
            get
            {
                if (aiType == AITypes.AIType_Aggressive && Definition != null && Definition.IsHealer())
                {
                    return AITypes.AIType_Defensive;
                }
                return aiType;
            }
            set { aiType = value; }
        }

        private AITypes aiType;

        /// <summary>
        /// Where an Escape (or Treasure) AI is heading for. Null for every other AI type.
        /// </summary>
        public FDPosition EscapePosition { get; set; }

        /// <summary>
        /// The chest a Treasure AI is going after. Once it is opened -- by this creature or
        /// by anyone else -- the creature heads for EscapePosition instead.
        /// </summary>
        public FDPosition TreasurePosition { get; set; }

        /// <summary>
        /// A magical creature that found no magic worth spelling defers the rest of its turn:
        /// it is put back in the queue and picked up again once every other creature of its
        /// faction has acted, this time to move and attack (see AIMagicalDelegate).
        /// Cleared at the start of each turn.
        /// </summary>
        public bool PendingAction { get; set; }

        /// <summary>
        /// The item an enemy leaves to whoever kills it (the original's dropItemId); 0 for
        /// none. A money item goes into the purse instead of a bag. See GameMain.grantDropItems.
        /// </summary>
        public int DropItemId { get; set; }

        protected FDAICreature(int creatureId, CreatureFaction faction) : base(creatureId, faction)
        {
        }

        public FDAICreature(int creatureId, CreatureDefinition definition, CreatureFaction faction, AITypes aiType) : base(creatureId, definition, faction)
        {
            this.AIType = aiType;
        }

        /// <summary>
        /// A creature lying in wait stops waiting once it is attacked.
        /// </summary>
        public void WakeUpByAttack()
        {
            if (this.AIType == AITypes.AIType_StandBy)
            {
                this.AIType = AITypes.AIType_Aggressive;
            }
        }

        /// <summary>
        /// AI targets (the ones an event uses as a marker) are invisible to the AI: nobody
        /// walks up to them and nobody attacks them.
        /// </summary>
        public bool IsNoticable()
        {
            return this.AIType != AITypes.AIType_UnNoticable;
        }
    }

    /// <summary>
    /// Main class for all the creatures in the game
    /// </summary>
    public class FDCreature : FDObject
    {

        #region Properties

        public CreatureFaction Faction { get; private set; }

        public bool HasActioned { get; set; }

        /// <summary>
        /// True for a party member who fell in an earlier chapter and has not been revived
        /// yet. They sit in FDMap.DeadCreatures alongside the ones who died in this battle
        /// -- both are "in the party but not on the field", and both are needed there so a
        /// conversation can still find their portrait -- but they never took the field at
        /// all, so anything that reacts to a death (CreatureDeadEvent) has to tell them
        /// apart. Carried through the save in CreatureMapRecord.
        /// </summary>
        public bool IsUnrevived { get; set; }

        /// <summary>
        /// The position before the creature moves, it is used to restore the position 
        /// when the player cancels the move
        /// </summary>
        public FDPosition PrePosition { get; set; }


        public CreatureDefinition Definition
        {
            get; set;
        }

        public int AttackItemIndex
        {
            get; private set;
        }

        public int DefendItemIndex
        {
            get; private set;
        }

        public int Level
        {
            get; set;
        }

        public int Hp
        {
            get; set;
        }

        public int Mp
        {
            get; set;
        }

        public int HpMax
        {
            get; set;
        }

        public int MpMax
        {
            get; set;
        }

        public int Ap
        {
            get; set;
        }

        public int Dp
        {
            get; set;
        }

        public int Dx
        {
            get; set;
        }

        public int Mv
        {
            get; set;
        }

        public int Exp
        {
            get; set;
        }

        public List<int> Items
        {
            get; set;
        }

        public List<int> Magics
        {
            get; set;
        }

        public HashSet<CreatureEffects> Effects
        {
            get; private set;
        }

        /// <summary>
        /// Rounds left on each effect in <see cref="Effects"/>, as in the original's
        /// statusFrozen / statusPoisoned / ... counters: an effect starts at the N its magic
        /// (or weapon) rolled, loses 1 at the end of every round (see <see cref="UpdateEffectsAtRoundEnd"/>)
        /// and is gone when it reaches 0. An effect in <see cref="Effects"/> with no entry here
        /// never wears off -- a chapter script's bound captive, or a record saved before the
        /// counters existed.
        /// </summary>
        public Dictionary<CreatureEffects, int> EffectTurns
        {
            get; private set;
        }



        #endregion


        #region Calculated Properties

        public int CalculatedAp
        {
            get
            {
                int delta = 0;
                AttackItemDefinition attackItem = this.GetAttackItem();
                if (attackItem != null)
                {
                    delta = attackItem.Ap;
                }

                int total = this.Ap + delta;
                if (this.Effects.Contains(CreatureEffects.EnhancedAp))
                {
                    total = (int)(total * 1.15f);
                }

                return total;
            }
        }

        public int CalculatedDp
        {
            get
            {
                int delta = 0;
                AttackItemDefinition attackItem = this.GetAttackItem();
                if (attackItem != null)
                {
                    delta = attackItem.Dp;
                }

                DefendItemDefinition defendItem = this.GetDefendItem();
                if (defendItem != null)
                {
                    delta = defendItem.Dp;
                }

                int total = this.Dp + delta;
                if (this.Effects.Contains(CreatureEffects.EnhancedDp))
                {
                    total = (int)(total * 1.15f);
                }

                return total;
            }
        }

        public int CalculatedMv
        {
            get
            {
                return this.Mv;
            }
        }

        public int CalculatedEv
        {
            get
            {
                int delta = 0;
                AttackItemDefinition attackItem = this.GetAttackItem();
                if (attackItem != null)
                {
                    delta = attackItem.Ev;
                }

                DefendItemDefinition defendItem = this.GetDefendItem();
                if (defendItem != null)
                {
                    delta = defendItem.Ev;
                }

                int total = this.Dx + delta;
                if (this.Effects.Contains(CreatureEffects.EnhancedDx))
                {
                    total = (int)(total * 1.15f);
                }

                return total;
            }
        }

        public int CalculatedHit
        {
            get
            {
                AttackItemDefinition attackItem = this.GetAttackItem();
                int total = this.Dx + attackItem.Hit;
                if (this.Effects.Contains(CreatureEffects.EnhancedDx))
                {
                    total = (int)(total * 1.15f);
                }

                return total;
            }
        }

        #endregion

        #region Constructors

        public FDCreature(int id, CreatureFaction faction) : base(id, ObjectType.Creature)
        {
            this.Faction = faction;

            // TEST
            this.Definition = new CreatureDefinition();
            this.Definition.DefinitionId = id;
            this.Definition.AnimationId = id;

            this.Items = new List<int>();
            this.Magics = new List<int>();
            this.Effects = new HashSet<CreatureEffects>();
            this.EffectTurns = new Dictionary<CreatureEffects, int>();

            this.AttackItemIndex = -1;
            this.DefendItemIndex = -1;
        }

        public FDCreature(int creatureId, CreatureDefinition definition, CreatureFaction faction): this(creatureId, faction)
        {
            this.Id = creatureId;
            this.Definition = definition;

            this.Level = definition.InitialLevel;
            this.Hp = this.HpMax = definition.InitialHp;
            this.Mp = this.MpMax = definition.InitialMp;

            this.Ap = definition.InitialAp;
            this.Dp = definition.InitialDp;
            this.Dx = definition.InitialDx;
            this.Mv = definition.InitialMv;
            this.Exp = definition.InitialEx;

            // Copies: the definition's lists are shared by every creature made from it, so
            // giving one of them an item (Chapter 15's thieves each carry their own
            // treasure) or a magic would hand it to all of them.
            this.Items = definition.Items != null ? new List<int>(definition.Items) : new List<int>();
            this.Magics = definition.Magics != null ? new List<int>(definition.Magics) : new List<int>();

            // Get Equiped items
            this.AttackItemIndex = -1;
            this.DefendItemIndex = -1;
            for (int i = 0; i < this.Items.Count; i++)
            {
                int itemId = this.Items[i];
                ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
                if (item != null && item is AttackItemDefinition)
                {
                    this.AttackItemIndex = i;
                }
                if (item != null && item is DefendItemDefinition)
                {
                    this.DefendItemIndex = i;
                }
            }

            this.Effects = new HashSet<CreatureEffects>();
            this.EffectTurns = new Dictionary<CreatureEffects, int>();
        }

        #endregion

        #region Public Methods

        public bool CanTakeAction()
        {
            if (this.Hp <= 0)
            {
                return false;
            }

            if (this.HasActioned)
            {
                return false;
            }

            if (this.Effects.Contains(CreatureEffects.Frozen))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check whether this creature has Attack item, and not frozen
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            if (this.Hp <= 0)
            {
                return false;
            }

            if (this.Effects.Contains(CreatureEffects.Frozen))
            {
                return false;
            }

            if (this.AttackItemIndex < 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Has some equip items to equip, other than the current equip items
        /// </summary>
        /// <returns></returns>
        public bool HasEquipItem()
        {
            return true;
        }

        public bool IsItemsFull()
        {
            return this.Items != null && this.Items.Count >= 8;
        }

        /// <summary>
        /// Removes an item and keeps the equipped-item indices pointing at the same
        /// items: the removed slot unequips, and anything after it shifts down by one.
        /// Without this the indices silently address the wrong slot after a removal --
        /// GetAttackItem / GetDefendItem then return an unrelated item, or null when the
        /// item at the stale index is of the other kind.
        /// </summary>
        /// <param name="itemIndex"></param>
        public void RemoveItemAt(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= this.Items.Count)
            {
                return;
            }

            this.Items.RemoveAt(itemIndex);

            this.AttackItemIndex = AdjustEquipIndexAfterRemoval(this.AttackItemIndex, itemIndex);
            this.DefendItemIndex = AdjustEquipIndexAfterRemoval(this.DefendItemIndex, itemIndex);
        }

        private static int AdjustEquipIndexAfterRemoval(int equipIndex, int removedIndex)
        {
            if (equipIndex == removedIndex)
            {
                // The equipped item itself was removed: nothing is equipped now.
                return -1;
            }

            return equipIndex > removedIndex ? equipIndex - 1 : equipIndex;
        }

        public int GetItemAt(int itemIndex)
        {
            if (this.Items == null || itemIndex < 0 || itemIndex >= this.Items.Count)
            {
                return -1;
            }

            return this.Items[itemIndex];
        }

        public void AddItem(int itemId)
        {
            if (IsItemsFull())
            {
                return;
            }

            this.Items.Add(itemId);
        }

        public bool HasAnyItem()
        {
            return this.Items != null && this.Items.Count > 0;
        }

        public bool HasItem(int itemId)
        {
            return this.HasAnyItem() && this.Items.Contains(itemId);
        }

        public void EquipItemAt(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= this.Items.Count)
            {
                return;
            }

            int itemId = this.Items[itemIndex];
            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);

            if (item == null || !item.IsEquipment())
            {
                return;
            }

            if (item is AttackItemDefinition)
            {
                // Attack Item
                this.AttackItemIndex = itemIndex;
            }
            else if (item is DefendItemDefinition)
            {
                // Defend Item
                this.DefendItemIndex = itemIndex;
            }

        }

        public void UpdateHp(int deltaHp)
        {
            this.Hp += deltaHp;

            this.Hp = Math.Max(this.Hp, 0);
            this.Hp = Math.Min(this.Hp, this.HpMax);
        }

        public void UpdateMp(int deltaMp)
        {
            this.Mp += deltaMp;

            this.Mp = Math.Max(this.Mp, 0);
            this.Mp = Math.Min(this.Mp, this.MpMax);
        }

        public AttackItemDefinition GetAttackItem()
        {
            int itemId = this.GetItemAt(this.AttackItemIndex);

            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (item == null)
            {
                return null;
            }

            return item as AttackItemDefinition;
        }

        public DefendItemDefinition GetDefendItem()
        {
            int itemId = this.GetItemAt(this.DefendItemIndex);

            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (item == null)
            {
                return null;
            }

            return item as DefendItemDefinition;
        }

        public int GetMagicAt(int magicIndex)
        {
            if (this.Magics != null || magicIndex < 0 || magicIndex >= this.Magics.Count)
            {
                return 0;
            }

            return this.Magics[magicIndex];
        }

        public bool CanSpellMagic()
        {
            // If the current creature is not having "Forbidden" effect
            return this.HasMagic() && !this.Effects.Contains(CreatureEffects.Forbidden);
        }

        public bool HasMagic()
        {
            return this.Magics != null && this.Magics.Count > 0;
        }

        public bool HasAfterMoveMagic()
        {
            if (!HasMagic())
            {
                return false;
            }

            foreach (int magicId in this.Magics)
            {
                MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);
                if (magic.AllowAfterMove)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddMagic(int magicId)
        {
            if (this.Magics == null)
            {
                this.Magics = new List<int>();
            }

            if (!this.Magics.Contains(magicId))
            {
                this.Magics.Add(magicId);
            }
        }

        public bool HasMoved()
        {
            return this.PrePosition != null && !this.PrePosition.AreSame(this.Position);
        }

        /// <summary>
        /// Used for dying event.
        /// After the creature is dead, it will be moved to Dead Creatures list.
        /// </summary>
        /// <returns></returns>
        public bool IsDead()
        {
            return this.Hp <= 0;
        }

        public bool IsOppositeFaction(FDCreature target)
        {
            if (this.Faction == CreatureFaction.Friend || this.Faction == CreatureFaction.Npc)
            {
                return target.Faction == CreatureFaction.Enemy;
            }
            else
            {
                return target.Faction == CreatureFaction.Friend || target.Faction == CreatureFaction.Npc;
            }
        }

        public bool HasEffect(CreatureEffects effect)
        {
            return this.Effects.Contains(effect);
        }

        /// <summary>
        /// During player 's turn, if the player cancelled current move, the creature should be reset to the previous position.
        /// </summary>
        public void ResetPosition()
        {
            if (this.PrePosition != null)
            {
                this.Position = this.PrePosition;
                //// this.PrePosition = null;
            }
        }

        public void ApplyDamage(DamageResult damage)
        {
            this.Hp = damage.HpAfter;
        }

        public void ApplyEffect(EffectResult effect)
        {
            switch (effect.Type)
            {
                case EffectType.EnhancedAp:
                    this.AddEffect(CreatureEffects.EnhancedAp, effect.RoundCount);
                    break;
                case EffectType.EnhancedDp:
                    this.AddEffect(CreatureEffects.EnhancedDp, effect.RoundCount);
                    break;
                case EffectType.EnhancedDx:
                    this.AddEffect(CreatureEffects.EnhancedDx, effect.RoundCount);
                    break;
                case EffectType.Poison:
                    this.AddEffect(CreatureEffects.Poisoned, effect.RoundCount);
                    break;
                case EffectType.Forbidden:
                    this.AddEffect(CreatureEffects.Forbidden, effect.RoundCount);
                    break;
                case EffectType.Freezing:
                    this.AddEffect(CreatureEffects.Frozen, effect.RoundCount);
                    break;
                case EffectType.AntiPoison:
                    this.RemoveEffect(CreatureEffects.Poisoned);
                    break;
                case EffectType.AntiFreeze:
                    this.RemoveEffect(CreatureEffects.Frozen);
                    break;
                case EffectType.StartAction:
                    // Being given a fresh action is a turn-state reset that also has to grey
                    // the icon back in, so it is done by the caller on the map (see
                    // GameMain.creatureMagic), not here.
                    break;
                case EffectType.Multi:
                    // A spell that lands several effects at once (the all-round buff).
                    if (effect is MultiEffectResult multi && multi.Effects != null)
                    {
                        foreach (EffectResult inner in multi.Effects)
                        {
                            this.ApplyEffect(inner);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Starts (or restarts) an effect with <paramref name="turns"/> rounds on it. Like the
        /// original, a second cast replaces the count rather than adding to it. A count of 0
        /// or less is taken as 1, so the effect still lasts the round it was cast in.
        /// </summary>
        public void AddEffect(CreatureEffects effect, int turns)
        {
            this.Effects.Add(effect);
            this.EffectTurns[effect] = Math.Max(turns, 1);
        }

        public void RemoveEffect(CreatureEffects effect)
        {
            this.Effects.Remove(effect);
            this.EffectTurns.Remove(effect);
        }

        /// <summary>
        /// The end of a round (after the enemy phase, as the original's updateStatusInTurn):
        /// every counted effect loses one round, and those that reach 0 are removed.
        /// Uncounted effects (see <see cref="EffectTurns"/>) are left alone.
        /// </summary>
        /// <returns>The effects that wore off.</returns>
        public List<CreatureEffects> UpdateEffectsAtRoundEnd()
        {
            List<CreatureEffects> expired = new List<CreatureEffects>();
            foreach (CreatureEffects effect in new List<CreatureEffects>(this.EffectTurns.Keys))
            {
                int left = this.EffectTurns[effect] - 1;
                if (left <= 0)
                {
                    expired.Add(effect);
                }
                else
                {
                    this.EffectTurns[effect] = left;
                }
            }

            foreach (CreatureEffects effect in expired)
            {
                this.RemoveEffect(effect);
            }

            return expired;
        }

        /// <summary>
        /// Called by AI Agent, make sure the creature is able to attack the target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsAbleToAttack(FDCreature target)
        {
            if (target == null)
            {
                return false;
            }

            if (!this.CanAttack())
            {
                return false;
            }

            if (this.CalculatedAp <= target.CalculatedDp)
            {
                return false;
            }

            // An event marker is not a creature as far as the AI is concerned.
            if (target is FDAICreature aiTarget && !aiTarget.IsNoticable())
            {
                return false;
            }

            return true;
        }


        #endregion

    }
}