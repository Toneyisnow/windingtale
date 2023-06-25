using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Core.Common;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class MenuSystemState : MenuState
    {
        public MenuSystemState(GameMain gameMain, IStateResultHandler stateHandler, FDPosition central) : base(gameMain, stateHandler, central)
        {
            // Matching
            this.SetMenu(0, MenuItemId.SystemMatching, false, () =>
            {
                // TODO
            });

            // Record
            this.SetMenu(1, MenuItemId.SystemRecord, true, () =>
            {
                ActionState nextState = new MenuRecordState(gameMain, stateHandler, central);
                stateHandler.HandlePushState(nextState);
            });

            // Settings
            this.SetMenu(2, MenuItemId.SystemSettings, true, () =>
            {
                ActionState nextState = new MenuSettingsState(gameMain, stateHandler, central);
                stateHandler.HandlePushState(nextState);
            });

            // Rest All
            this.SetMenu(3, MenuItemId.SystemRestAll, true, () =>
            {
                // 想要结束本回合吗
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 1);
                PromptActivity prompt = new PromptActivity(message, (index) =>
                {
                    if (index == 1)
                    {
                        gameMain.EndAllFriendsTurn();
                        stateHandler.HandleClearStates();
                    }
                });

                activityManager.Push(prompt);
            });
        }
    }
}