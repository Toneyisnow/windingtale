using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.UI.Dialogs;

namespace WindingTale.Core.Components.ActionStates
{
    public class MenuSystemState : MenuState
    {

        public MenuSystemState(IGameAction gameAction, FDPosition central) : base(gameAction, central)
        {
            // Matching
            this.SetMenu(0, MenuItemId.SystemMatching, false, () =>
            {
                PromptPack pack = new PromptPack(1, "Good morning");
                SendPack(pack);

                return StateOperationResult.None();
            });

            // Record
            this.SetMenu(1, MenuItemId.SystemRecord, true, () =>
            {
                ActionState nextState = new MenuRecordState(gameAction, central);
                return StateOperationResult.Push(nextState);
            });

            // Settings
            this.SetMenu(2, MenuItemId.SystemSettings, true, () =>
            {
                ActionState nextState = new MenuSettingsState(gameAction, central);
                return StateOperationResult.Push(nextState);
            });

            // Rest All
            this.SetMenu(3, MenuItemId.SystemRestAll, true, () =>
            {
                return StateOperationResult.None();
            });


        }

        public override StateOperationResult OnSelectIndex(int index)
        {
            return StateOperationResult.Clear();
        }
    }
}