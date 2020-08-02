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

        private IGameCallback gameCallback = null;

        public GameStateDispatcher(IGameAction gameAction, IGameCallback gameCallback)
        {
            this.gameAction = gameAction;
            this.gameCallback = gameCallback;

            this.actionStateStack = new Stack<ActionState>();

            // Push Idle State for init
            var idle = new IdleState(gameAction, gameCallback);
            actionStateStack.Push(idle);
            

        }


        public void ClickAtPosition(FDPosition position)
        {

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