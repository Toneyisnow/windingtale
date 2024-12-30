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
        public AITypes AIType { get; private set; }

        protected FDAICreature(int creatureId, CreatureFaction faction) : base(creatureId, faction)
        {
        }

        public FDAICreature(int creatureId, CreatureDefinition definition, CreatureFaction faction, AITypes aiType) : base(creatureId, definition, faction)
        {
            this.AIType = aiType;
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
            get; private set;
        }

        public List<int> Magics
        {
            get; private set;
        }

        public HashSet<CreatureEffects> Effects
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

                return this.Dx + delta;
            }
        }

        public int CalculatedHit
        {
            get
            {
                AttackItemDefinition attackItem = this.GetAttackItem();
                return this.Dx + attackItem.Hit;
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

            this.Items = definition.Items;
            this.Magics = definition.Magics;

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
        /// Note: need to update attack/defense item index after this call
        /// </summary>
        /// <param name="itemIndex"></param>
        public void RemoveItemAt(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= this.Items.Count)
            {
                return;
            }

            this.Items.RemoveAt(itemIndex);
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
            int itemId = this.Items[itemIndex];
            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);

            if (!item.IsEquipment())
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
            return this.PrePosition != null && this.PrePosition.AreSame(this.Position);
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
                    this.Effects.Add(CreatureEffects.EnhancedAp);
                    break;
                case EffectType.EnhancedDp:
                    this.Effects.Add(CreatureEffects.EnhancedDp);
                    break;
                case EffectType.EnhancedDx:
                    this.Effects.Add(CreatureEffects.EnhancedDx);
                    break;
                case EffectType.Poison:
                    this.Effects.Add(CreatureEffects.Poisoned);
                    break;
                case EffectType.Forbidden:
                    this.Effects.Add(CreatureEffects.Forbidden);
                    break;
                case EffectType.Freezing:
                    this.Effects.Add(CreatureEffects.Frozen);
                    break;
                case EffectType.AntiPoison:
                    this.Effects.Remove(CreatureEffects.Poisoned);
                    break;
                case EffectType.AntiFreeze:
                    this.Effects.Remove(CreatureEffects.Frozen);
                    break;
                case EffectType.StartAction:
                    // Start new action
                    break;
                case EffectType.Multi:
                    // TODO: Multi effect
                    break;
                default:
                    break;
            }
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

            return true;
        }


        #endregion

    }
}