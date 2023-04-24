using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Data;
using WindingTale.Core.Definitions;

namespace WindingTale.Legacy.Core.ObjectModels
{
    /// <summary>
    /// Functions about a creature
    /// </summary>
    public class FDCreature
    {

        public CreatureFaction Faction
        {
            get; private set;
        }

        public CreatureData Data
        {
            get; private set;
        }

        public int CreatureId
        {
            get
            {
                return (this.Data != null ? this.Data.CreatureId : 0);
            }
        }

        public int DefinitionId
        {
            get
            {
                return this.Definition != null ? this.Definition.DefinitionId : 0;
            }
        }
        public CreatureDefinition Definition
        {
            get; private set;
        }

        public FDPosition PreMovePosition
        {
            get; set;
        }

        public FDPosition Position
        {
            get; private set;
        }

        public FDCreature(int creatureId)
        {
            this.Data = new CreatureData();
            this.Data.CreatureId = creatureId;

            this.Data.LastGainedExperience = 0;
            this.Data.AIType = CreatureData.AITypes.AIType_Aggressive;
        }

        public FDCreature(int creatureId, CreatureFaction faction, CreatureDefinition definition, FDPosition position)
        {
            this.Faction = faction;

            this.Data = new CreatureData();
            this.Data = CreatureData.FromDefinition(creatureId, definition);

            this.Definition = definition;
            this.Position = position;
            this.PreMovePosition = position;

            this.Data.LastGainedExperience = 0;

            if (this.Definition.Occupation == 154 || this.Definition.Occupation == 155 || this.Definition.DefinitionId == 747)
            {
                this.Data.AIType = CreatureData.AITypes.AIType_Defensive;
            }
            else
            {
                this.Data.AIType = CreatureData.AITypes.AIType_Aggressive;
            }
        }

        public bool HasMoved()
        {
            return !this.PreMovePosition.AreSame(this.Position);
        }

        public bool HasActioned
        {
            get; set;
        }

        public bool HasEquipItem()
        {
            return this.Data.Items.Exists(itemId => {
                ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
                return item.IsEquipment();
            });
        }

        public bool HasUsableItem()
        {
            return this.Data.Items.Exists(itemId => {
                ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
                return item.IsUsable();
            });
        }

        public bool IsOppositeFaction(FDCreature other)
        {
            return ((this.Faction == CreatureFaction.Friend || this.Faction == CreatureFaction.Npc) && other.Faction == CreatureFaction.Enemy)
                || ((other.Faction == CreatureFaction.Friend || other.Faction == CreatureFaction.Npc) && this.Faction == CreatureFaction.Enemy);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsActionable()
        {
            return this.Faction == CreatureFaction.Friend && !this.HasActioned 
                && this.Data.Effects != null && !this.Data.Effects.Contains(0);
        }

        public void SetMoveTo(FDPosition position)
        {
            this.PreMovePosition = this.Position;
            this.Position = position;
        }

        public void ResetPosition()
        {
            this.Position = this.PreMovePosition;
        }

        public void OnTurnStart()
        {
            this.HasActioned = false;
            this.PreMovePosition = this.Position;
        }

        public LevelUpInfo UpdateExpAndLevelUp()
        {
            if (this.Data.Level >= this.Definition.GetMaxLevel())
            {
                return null;
            }

            this.Data.Exp += this.Data.LastGainedExperience;
            this.Data.LastGainedExperience = 0;

            if(this.Data.Exp >= 100)
            {
                return this.Data.LevelUp();
            }

            return null;
        }

        public bool IsFrozen()
        {
            return this.Data.Effects.Contains(CreatureData.CreatureEffects.Frozen);
        }

        public bool IsAbleToAttack(FDCreature target)
        {
            if (target == null)
            {
                return false;
            }

            return this.Data.CanAttack()
                && this.Data.CalculatedAp > target.Data.CalculatedDp
                && target.Data.AIType != CreatureData.AITypes.AIType_UnNoticable;


        }

        public FDCreature Clone()
        {
            FDCreature another = new FDCreature(this.CreatureId);
            another.Definition = this.Definition;
            another.Faction = this.Faction;
            another.Position = this.Position;
            another.PreMovePosition = this.PreMovePosition;
            another.HasActioned = this.HasActioned;

            another.Data = this.Data.Clone();

            return another;
        }
    }
}