using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.ActionStates
{
    public class MenuSystemState : MenuState
    {

        public MenuSystemState(IGameAction gameAction, FDPosition central) : base(gameAction, central)
        {
            // Matching
            this.SetMenu(0, MenuItemId.SystemMatching, false, () =>
            {
                return StateOperationResult.None();
            });

            // Record
            this.SetMenu(1, MenuItemId.SystemRecord, true, () =>
            {
                return StateOperationResult.None();
            });

            // Settings
            this.SetMenu(2, MenuItemId.SystemSettings, true, () =>
            {
                return StateOperationResult.None();
            });

            // Rest All
            this.SetMenu(3, MenuItemId.SystemRestAll, true, () =>
            {
                return StateOperationResult.None();
            });


        }

    }
}