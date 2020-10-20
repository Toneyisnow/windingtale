using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;

namespace WindingTale.AI.Delegates
{
    public class AIStandByDelegate : AIDelegate
    {
        public AIStandByDelegate(IGameAction gameAction, FDCreature c) : base(gameAction, c)
        {

        }

        public override void TakeAction()
        {

        }

    }
}
