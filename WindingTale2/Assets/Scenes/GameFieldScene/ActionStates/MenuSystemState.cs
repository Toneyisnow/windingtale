using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using WindingTale.Core.Common;
using WindingTale.Scenes.GameFieldScene.ActionStates;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.UI.Dialogs;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class MenuSystemState : MenuState
    {
        public MenuSystemState(GameMain gameMain, FDPosition central)
            : base(gameMain, central, new IdleState(gameMain))
        {
            // Matching
            this.SetMenu(0, MenuItemId.SystemMatching, false, () =>
            {
                // TODO
                return this;
            });

            // Record
            this.SetMenu(1, MenuItemId.SystemRecord, true, () =>
            {
                IActionState nextState = new MenuRecordState(gameMain, central);
                return nextState;
            });

            // Settings
            this.SetMenu(2, MenuItemId.SystemSettings, true, () =>
            {
                IActionState nextState = new MenuSettingsState(gameMain, central);
                return nextState;
            });

            // Rest All
            this.SetMenu(3, MenuItemId.SystemRestAll, true, () =>
            {
                // 想要结束本回合吗
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 1);
                //PromptActivity prompt = new PromptActivity(message, (index) =>
                //{
                //    if (index == 1)
                //    {
                //        gameMain.EndAllFriendsTurn();
                //        stateHandler.HandleClearStates();
                //    }
                //});

                return this;
                //activityManager.Push(prompt);
            });
        }
    }
}

