using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.UI.Components.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class MenuRecordState : MenuState
    {
        public enum SubRecordState
        {
            SaveGame = 1,
            LoadGame = 2,
            QuitGame = 3,
        }

        private SubRecordState subState;


        public MenuRecordState(GameMain gameMain, FDPosition position) : base(gameMain, position)
        {
            // Save Game
            this.SetMenu(0, MenuItemId.RecordSave, gameMap.CanSaveGame(), () =>
            {
                PromptActivity prompt = new PromptActivity(0, "", OnSaveGameConfirmed);
                SendPack(prompt);
                this.subState = SubRecordState.SaveGame;

                return null;
            });

            // Game Info
            this.SetMenu(1, MenuItemId.RecordInfo, true, () =>
            {
                int turnId = gameMain.TurnNo;
                int chapterId = gameMap.ChapterId;
                ShowBriefPack pack = new ShowBriefPack();
                SendPack(pack);

                return null;
            });

            // Load Game
            this.SetMenu(2, MenuItemId.RecordLoad, true, () =>
            {
                PromptActivity prompt = new PromptActivity(0, "", OnLoadGameConfirmed);
                SendPack(prompt);
                this.subState = SubRecordState.LoadGame;

                return null;
            });

            // Quit Game
            this.SetMenu(3, MenuItemId.RecordQuit, true, () =>
            {
                PromptActivity prompt = new PromptActivity(0, "", OnQuitGameConfirmed);
                SendPack(prompt);
                this.subState = SubRecordState.QuitGame;

                return null;
            });
        }


        private StateResult OnLoadGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Loading Game...");
                
                // Load Game
                // gameAction.LoadGame();
                return StateResult.Clear();
            }
            else
            {
                return null;
            }
        }

        private StateResult OnSaveGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Saving Game...");

                // Save Game
                gameMain.SaveMapRecord();

                TalkActivity talk = new TalkActivity(0, "");
                return StateResult.Clear();
            }
            else
            {
                return null;
            }
        }

        private StateResult OnQuitGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Quiting Game...");

                // Quit Game
                gameMain.OnGameQuit();

                return StateResult.Clear();
            }
            else
            {
                return null;
            }
        }
    }
}