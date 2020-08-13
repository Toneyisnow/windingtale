using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Components;

namespace WindingTale.Core.Components.ActionStates
{
    public class GameStateDispatcher
    {
        private Stack<ActionState> actionStateStack = null;

        private IGameAction gameAction = null;

        public GameStateDispatcher(IGameAction gameAction)
        {
            this.gameAction = gameAction;

            this.actionStateStack = new Stack<ActionState>();

            // Push Idle State for init
            var idle = new IdleState(gameAction);
            actionStateStack.Push(idle);
            

        }

        public ActionState CurrentState
        {
            get
            {
                if (actionStateStack == null || actionStateStack.Count == 0)
                {
                    return null;
                }

                return actionStateStack.Peek();
            }
        }

        public void OnSelectPosition(FDPosition position)
        {
            if (this.CurrentState == null)
            {
                throw new InvalidOperationException("current state is null.");
            }

            this.CurrentState.OnSelectPosition(position);


        }

        public void SelectCreature(int creatureId)
        {

        }
        
        public void SelectCreature(FDCreature creature)
        {

        }

        public void SelectMenu(int menuId)
        {

        }

        public void SelectMenu(FDMenu menu)
        {

        }


    }

}