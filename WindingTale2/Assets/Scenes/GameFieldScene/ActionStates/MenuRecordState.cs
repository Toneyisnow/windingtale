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

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class MenuRecordState : MenuState
    {
        public enum SubRecordState
        {
            SaveGame = 1,
            LoadGame = 2,
            QuitGame = 3,
        }

        public MenuRecordState(GameMain gameMain, FDPosition position) 
            : base(gameMain, position, new MenuSystemState(gameMain, position))
        {
            // Save Game
            this.SetMenu(0, MenuItemId.RecordSave, fdMap.CanSaveGame(), () =>
            {
                // �Ƿ񱣴浱ǰ��Ϸ��
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 5);
                //PromptActivity prompt = new PromptActivity(message, OnSaveGameConfirmed);
                //activityManager.Push(prompt);
            });

            // Game Info
            this.SetMenu(1, MenuItemId.RecordInfo, true, () =>
            {
                //ShowGameInfoActivity info = new ShowGameInfoActivity(gameMain);
                //activityManager.Push(info);
            });

            // Load Game
            this.SetMenu(2, MenuItemId.RecordLoad, true, () =>
            {
                // Ҫ��ȡս����
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 4);
                //PromptActivity prompt = new PromptActivity(message, OnLoadGameConfirmed);
                //activityManager.Push(prompt);
            });

            // Quit Game
            this.SetMenu(3, MenuItemId.RecordQuit, true, () =>
            {
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 6);
                //PromptActivity prompt = new PromptActivity(message, OnQuitGameConfirmed);
                //activityManager.Push(prompt);
            });
        }

        private void OnLoadGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Loading Game...");

                // Load Game
                // gameAction.LoadGame();
                //// stateHandler.HandleClearStates();
            }
        }

        private void OnSaveGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Saving Game...");

                // Save Game
                //// gameMain.SaveMapRecord();

                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 33);
                //TalkActivity talk = new TalkActivity(message);
                //stateHandler.HandleClearStates();
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

