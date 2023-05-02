using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class MenuSettingsState : MenuState
    {
        public MenuSettingsState(GameMain gameMain, IStateResultHandler stateHandler, FDPosition position) : base(gameMain, stateHandler, position)
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

        public override void OnSelectPosition(FDPosition position)
        {
        }
    }
}