using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.ActionStates
{
    public class MenuSettingsState : MenuState
    {
        public MenuSettingsState(IGameAction gameAction, FDPosition position) : base(gameAction, position)
        {

        }

        public override void OnEnter()
        {
            throw new System.NotImplementedException();
        }

        public override void OnExit()
        {
            throw new System.NotImplementedException();
        }

        public override StateResult OnSelectPosition(FDPosition position)
        {
            throw new System.NotImplementedException();
        }
    }
}