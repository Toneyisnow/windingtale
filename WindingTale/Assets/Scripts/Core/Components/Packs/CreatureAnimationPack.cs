using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class CreatureAnimationPack : PackBase
    {
        public enum AnimationType
        {
            Rest = 1,
            Recover = 2,

        }

        public FDCreature Creature
        {
            get; private set;
        }

        public AnimationType Type
        {
            get; private set;
        }

        public CreatureAnimationPack(FDCreature creature, AnimationType type)
        {
            this.Creature = creature;
            this.Type = type;
        }
    }
}