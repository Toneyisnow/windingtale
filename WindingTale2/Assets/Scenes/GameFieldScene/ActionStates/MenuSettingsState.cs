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
            // Sound
            this.SetMenu(0, MenuItemId.SettingsSound, true, () =>
            {
                return this;
            });

            // Music
            this.SetMenu(1, MenuItemId.SettingsMusic, true, () =>
            {
                return this;
            });

            // Fight Animation On/Off
            this.SetMenu(2, MenuItemId.SettingsFight, true, () =>
            {
                return this;
            });

            // Information
            this.SetMenu(3, MenuItemId.SettingsInfo, true, () =>
            {
                return this;
            });

        }


    }
}