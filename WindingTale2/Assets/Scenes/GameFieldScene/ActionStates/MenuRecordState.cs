using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using UnityEditor.SceneManagement;
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
            // Save Game
            this.SetMenu(0, MenuItemId.RecordSave, fdMap.CanSaveGame(), () =>
            {
                // 是否保存当前游戏？
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

            // Load Game
            this.SetMenu(2, MenuItemId.RecordLoad, true, () =>
            {
                // 要读取战况吗？
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
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 6);
                //PromptActivity prompt = new PromptActivity(message, OnQuitGameConfirmed);
                //activityManager.Push(prompt);
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

                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 33);
                var rawText = LocalizationManager.GetFDMessageString(message);
                gameMain.gameCanvas.ShowTalkDialog(0, rawText, true, GameCanvas.DialogPosition.Bottom, (confirm) => { });

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
                    gameMain.gameCanvas.ShowTalkDialog(0, rawText, true, GameCanvas.DialogPosition.Bottom, (confirm) => { });

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

                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 16);
                //TalkActivity prompt = new TalkActivity(message);
                //activityManager.Push(prompt);

                //CallbackActivity callback = new CallbackActivity(() => gameMain.OnGameQuit());
                //activityManager.Push(callback);

                //stateHandler.HandleClearStates();
            }
        }
    }
}

