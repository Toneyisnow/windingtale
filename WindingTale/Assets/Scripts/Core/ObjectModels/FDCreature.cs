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
        private CreatureData data = null;

        private bool hasActioned = false;

        public int CreatureId
        {
            get; private set;
        }

        public CreatureData Data
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
            this.CreatureId = creatureId;
        }

        public FDCreature(int creatureId, CreatureDefinition creatureDefinition, FDPosition position)
        {
            this.CreatureId = creatureId;
        }

        public bool HasMoved()
        {
            return this.PreMovePosition.AreSame(this.Position);
        }

        public bool HasActioned()
        {
            return hasActioned;
        }

        public void MoveTo(FDPosition position)
        {
            this.PreMovePosition = position;
            this.Position = position;
        }

        public void OnTurnStart()
        {
            this.hasActioned = false;
            this.PreMovePosition = this.Position;
        }
    }
}