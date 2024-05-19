using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class MenuSettingsState : MenuState
    {
        public MenuSettingsState(GameMain gameMain, FDPosition position) 
            : base(gameMain, position, new MenuSystemState(gameMain, position))
        {

        }


    }
}