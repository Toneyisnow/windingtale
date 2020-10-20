using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;

namespace WindingTale.AI.Delegates
{
    public abstract class AIMagicalDelegate : AIDelegate
    {
        public AIMagicalDelegate(IGameAction gameAction, FDCreature c) : base(gameAction, c)
        {

        }


    }
}
