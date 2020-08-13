using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class IdleState : ActionState
    {
        public IdleState(IGameAction action) : base(action)
        {

        }

        public override void OnEnter()
        {
            // Nothing
        }

        public override void OnExit()
        {
            // Nothing
        }



        public override void OnSelectPosition(FDPosition position)
        {

            FDCreature creature = gameAction.GetCreatureAt(position);
            if (creature == null)
            {
                // Empty space, show system menu

            }
            else if (creature.Faction == CreatureFaction.Friend)
            {

            }
        }


    }
}