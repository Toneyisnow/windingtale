using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.Definitions;

namespace WindingTale.Core.ObjectModels
{
    /// <summary>
    /// Functions about a creature
    /// </summary>
    public class FDCreature
    {

        private bool hasActioned = false;

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
        }

        public FDCreature(int creatureId, CreatureFaction faction, CreatureDefinition creatureDefinition, FDPosition position)
        {
            this.Faction = faction;

            this.Data = new CreatureData();
            this.Data.CreatureId = creatureId;
            this.Data.DefinitionId = creatureDefinition.DefinitionId;

            this.Definition = creatureDefinition;
            this.Position = position;
        }

        public bool HasMoved()
        {
            return this.PreMovePosition.AreSame(this.Position);
        }

        public bool HasActioned()
        {
            return hasActioned;
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsActionable()
        {
            return this.Faction == CreatureFaction.Friend && !hasActioned && !this.Data.Effects.Contains(0);
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
            this.hasActioned = false;
            this.PreMovePosition = this.Position;
        }


        public FDCreature Clone()
        {
            FDCreature another = new FDCreature(this.CreatureId);
            another.Definition = this.Definition;
            another.Faction = this.Faction;
            another.Position = this.Position;

            another.Data = this.Data.Clone();

            return another;
        }

    }
}