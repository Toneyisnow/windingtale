using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Files;
using WindingTale.UI.Audio;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class MenuSettingsState : MenuState
    {
        public MenuSettingsState(GameMain gameMain, FDPosition position) 
            : base(gameMain, position, new MenuSystemState(gameMain, position))
        {
            // Sound effects on/off. The menu stays open so the change can be heard (turning
            // sound back on answers with a confirm click).
            this.SetMenu(0, MenuItemId.SettingsSound, true, () =>
            {
                bool isOn = !UserSettingsManager.Current.IsSoundOn;
                UserSettingsManager.SetSoundOn(isOn);
                if (isOn)
                {
                    SoundEffects.Play(SoundEffect.DialogConfirm);
                }
                gameMain.gameMap.RefreshMenuOffMarks();
                return this;
            }, () => !UserSettingsManager.Current.IsSoundOn);

            // Background music on/off.
            this.SetMenu(1, MenuItemId.SettingsMusic, true, () =>
            {
                UserSettingsManager.SetMusicOn(!UserSettingsManager.Current.IsMusicOn);
                gameMain.gameMap.RefreshMenuOffMarks();
                return this;
            }, () => !UserSettingsManager.Current.IsMusicOn);

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

        // Opens on the Up item (211 / 221).
        protected override int PreferredDefaultIndex => 1;
    }
}