using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Packs
{
    public class ComposeCreaturePack : PackBase
    {
        public int CreatureId
        {
            get; private set;
        }

        public int AnimationId
        {
            get; private set;
        }

        public FDPosition Position
        {
            get; private set;
        }

        public ComposeCreaturePack(int creatureId, int animationId, FDPosition position)
        {
            this.Type = PackType.ComposeCreature;
            this.CreatureId = creatureId;
            this.AnimationId = animationId;
            this.Position = position;
        }
    }
}