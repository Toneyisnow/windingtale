using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using WindingTale.Core.Common;
using WindingTale.Scenes.GameFieldScene.ActionStates;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.UI.Dialogs;
using WindingTale.Scenes.GameFieldScene.Activities;

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
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 1);
                var rawText = LocalizationManager.GetFDMessageString(message);
                        
                gameMain.gameCanvas.ShowTalkDialog(0, rawText, true, GameCanvas.DialogPosition.Top, (index) =>
                {
                    // The reply message cannot be shown from here. There is only one
                    // TalkDialog object, and TalkDialog.onConfirm/onCancel close it
                    // immediately *after* this callback returns -- anything opened inline
                    // is torn down in the same call stack, before it renders a frame.
                    // Queue it instead, so it opens once the confirm dialog is gone.
                    FDMessage reply = FDMessage.Create(FDMessage.MessageTypes.Information, index == 1 ? 1 : 2);
                    gameMain.PushActivity(new TalkActivity(reply));

                    if (index == 1)
                    {
                        // Everything endTurnForAll queues (the rest-recovery flashes, then
                        // the turn rollover) lands behind the reply, and TalkActivity holds
                        // the queue until the player dismisses it -- so the message is read
                        // first. The greyout it applies outside the queue is immediate.
                        gameMain.endTurnForAll();
                    }

                    PlayerInterface.getDefault().onUpdateState(new IdleState(gameMain));
                });

                return this;
            });
        }

    }
}

