using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Legacy.Core.Components;
using WindingTale.UI.Activities;
using WindingTale.UI.Dialogs;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class MenuSystemState : MenuState
    {
        public enum SubState
        {
            ConfirmMatching = 1,
            ConfirmRestAll = 2,
        }

        private SubState subState = 0;

        public MenuSystemState(GameMain gameMain, IStateResultHandler stateHandler, FDPosition central) : base(gameMain, stateHandler, central)
        {
            // Matching
            this.SetMenu(0, MenuItemId.SystemMatching, false, () =>
            {
                subState = SubState.ConfirmMatching;
                
                Message message = Message.Create(Message.MessageTypes.Confirm, 1);
                TalkPack pack = new TalkPack(null, message);
                SendPack(pack);

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
                PromptActivity prompt = new PromptActivity((index) =>
                {
                    if (index == 0)
                    {
                        return;
                    }

                    gameMain.EndAllFriendsTurn();
                    stateHandler.HandleClearStates();
                });

                activityManager.Push(prompt);
            });
        }
    }
}