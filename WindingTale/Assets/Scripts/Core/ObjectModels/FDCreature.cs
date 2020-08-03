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


        public FDPosition PreMovePosition
        {
            get; set;
        }

        public FDPosition Position
        {
            get; private set;
        }

        public FDCreature()
        {

        }

        public FDCreature(CreatureDefinition creatureDefinition, FDPosition position)
        {

        }

        public bool HasMoved()
        {
            return this.PreMovePosition.AreSame(this.Position);
        }


    }
}