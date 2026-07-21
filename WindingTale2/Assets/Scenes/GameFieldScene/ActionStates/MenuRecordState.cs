using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Core.Common;
using WindingTale.Core.Files;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.UI.Dialogs;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class MenuRecordState : MenuState
    {
        public MenuRecordState(GameMain gameMain, FDPosition position) 
            : base(gameMain, position, new MenuSystemState(gameMain, position))
        {
            // Save Game. Only enabled at the start of the player's turn, before anyone has
            // moved -- a save records no per-creature turn state and a load replays the
            // saved turn from its start, so a mid-turn save would refund the moves already
            // made. This state is rebuilt every time the menu opens, so the flag is live.
            this.SetMenu(0, MenuItemId.RecordSave, fdMap.CanSaveGame(), () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 5);
                    var rawText = LocalizationManager.GetFDMessageString(message);
                    gameMain.gameCanvas.ShowTalkDialog(0, rawText, true, GameCanvas.DialogPosition.Bottom, OnSaveGameConfirmed);
                });

                //PromptActivity prompt = new PromptActivity(message, OnSaveGameConfirmed);
                //activityManager.Push(prompt);
                return this;
            });

            // Game Info
            this.SetMenu(1, MenuItemId.RecordInfo, true, () =>
            {
                //ShowGameInfoActivity info = new ShowGameInfoActivity(gameMain);
                //activityManager.Push(info);
                return this;
            });

            // Load Game. Needs a save belonging to *this* chapter. With no save at all,
            // loading would reload the scene, find nothing to read, and quietly drop the
            // player into a fresh chapter 1; with a save from another chapter it would
            // silently carry them off to that chapter's map. Neither is what "读取战场战况"
            // means here. Like Save above, the flag is re-evaluated on every menu open.
            this.SetMenu(2, MenuItemId.RecordLoad,
                GameMapRecordManager.HasSavedGame(GameMain.SaveRecordName, fdMap.ChapterId), () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 4);
                    var rawText = LocalizationManager.GetFDMessageString(message);
                    gameMain.gameCanvas.ShowTalkDialog(0, rawText, true, GameCanvas.DialogPosition.Bottom, OnContinueGameConfirmed);
                });

                return this;
            });

            // Quit Game
            this.SetMenu(3, MenuItemId.RecordQuit, true, () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 6);
                    var rawText = LocalizationManager.GetFDMessageString(message);
                    gameMain.gameCanvas.ShowTalkDialog(0, rawText, true, GameCanvas.DialogPosition.Bottom, OnQuitGameConfirmed);
                });
                return this;
            });
        }

        private void OnContinueGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Loading Continue Game...");

                // Load Game
                gameMain.ContinueGame();

                // Seems there is no Information-33
                // FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 33);
                // var rawText = LocalizationManager.GetFDMessageString(message);
                // gameMain.gameCanvas.ShowTalkDialog(0, rawText, false, GameCanvas.DialogPosition.Bottom, (confirm) => { });

                IdleState idleState = new IdleState(gameMain);
                PlayerInterface.getDefault().onUpdateState(idleState);
            }
        }

        private void OnSaveGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Saving Game...");

                // Save Game
                gameMain.SaveGame();
                gameMain.PushActivity(gameMain =>
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 9);
                    var rawText = LocalizationManager.GetFDMessageString(message);
                    gameMain.gameCanvas.ShowTalkDialog(0, rawText, false, GameCanvas.DialogPosition.Bottom, (confirm) => { });

                    IdleState idleState = new IdleState(gameMain);
                    PlayerInterface.getDefault().onUpdateState(idleState);
                });
            }
            else
            {
                gameMain.PushActivity(gameMain =>
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 2);
                    var rawText = LocalizationManager.GetFDMessageString(message);
                    gameMain.gameCanvas.ShowTalkDialog(0, rawText, false, GameCanvas.DialogPosition.Bottom, (confirm) => { });

                    IdleState idleState = new IdleState(gameMain);
                    PlayerInterface.getDefault().onUpdateState(idleState);
                });
            }
        }

        private void OnQuitGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Quiting Game...");

                // Quit Game
                gameMain.PushActivity(gameMain =>
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 8);
                    var rawText = LocalizationManager.GetFDMessageString(message);
                    gameMain.gameCanvas.ShowTalkDialog(0, rawText, false, GameCanvas.DialogPosition.Bottom, (confirm) => { });
                });

                gameMain.PushActivity(gameMain =>
                {
                    gameMain.OnQuit();
                });
            }
        }
    }
}

