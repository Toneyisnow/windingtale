using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Data;
using WindingTale.Core.Components.Packs;

namespace WindingTale.Core.Components.ActionStates
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


        public MenuRecordState(IGameAction gameAction, FDPosition position) : base(gameAction, position)
        {
            // Save Game
            this.SetMenu(0, MenuItemId.RecordSave, gameAction.CanSaveGame(), () =>
            {
                PromptPack prompt = new PromptPack(0, "");
                SendPack(prompt);
                this.subState = SubRecordState.SaveGame;

                return StateOperationResult.None();
            });

            // Game Info
            this.SetMenu(1, MenuItemId.RecordInfo, true, () =>
            {
                int turnId = gameAction.TurnId();
                int chapterId = gameAction.ChapterId();
                ShowBriefPack pack = new ShowBriefPack();
                SendPack(pack);

                return StateOperationResult.None();
            });

            // Load Game
            this.SetMenu(2, MenuItemId.RecordLoad, true, () =>
            {
                PromptPack prompt = new PromptPack(0, "");
                SendPack(prompt);
                this.subState = SubRecordState.LoadGame;

                return StateOperationResult.None();
            });

            // Quit Game
            this.SetMenu(3, MenuItemId.RecordQuit, true, () =>
            {
                PromptPack prompt = new PromptPack(0, "");
                SendPack(prompt);
                this.subState = SubRecordState.QuitGame;

                return StateOperationResult.None();
            });


        }

        public override StateOperationResult OnSelectIndex(int index)
        {
            switch (this.subState)
            {
                case SubRecordState.LoadGame:
                    return OnLoadGameConfirmed(index);
                case SubRecordState.SaveGame:
                    return OnSaveGameConfirmed(index);
                case SubRecordState.QuitGame:
                    return OnQuitGameConfirmed(index);
                default:
                    return null;
            }
        }

        private StateOperationResult OnLoadGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Loading Game...");
                
                // Load Game
                // gameAction.LoadGame();
                return StateOperationResult.Clear();
            }
            else
            {
                return StateOperationResult.None();
            }
        }

        private StateOperationResult OnSaveGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Saving Game...");

                // Save Game
                gameAction.SaveGame();
                return StateOperationResult.Clear();
            }
            else
            {
                return StateOperationResult.None();
            }
        }

        private StateOperationResult OnQuitGameConfirmed(int index)
        {
            if (index == 1)
            {
                Debug.Log("Quiting Game...");

                // Quit Game
                SendPack(new SystemPack(SystemPack.Command.Quit));

                return StateOperationResult.Clear();
            }
            else
            {
                return StateOperationResult.None();
            }
        }
    }
}