using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.ActionStates
{
    public abstract class MenuState : ActionState
    {
        public FDPosition Central
        {
            get; private set;
        }

        public FDPosition[] MenuItemPositions
        {
            get; private set;
        }

        public MenuItemId[] MenuItemIds
        {
            get; private set;
        }

        public Func<StateOperationResult>[] MenuActions
        {
            get; private set;
        }

        public MenuState(IGameAction gameAction, FDPosition central) : base(gameAction)
        {
            this.Central = central;

            this.MenuItemPositions = new FDPosition[4]
            {
                FDPosition.At(Central.X - 1, Central.Y),
                FDPosition.At(Central.X, Central.Y - 1),
                FDPosition.At(Central.X + 1, Central.Y),
                FDPosition.At(Central.X, Central.Y + 1),
            };

            this.MenuItemIds = new MenuItemId[4];
            this.MenuActions = new Func<StateOperationResult>[4];
        }

        public void SetMenu(int index, MenuItemId menuItemId, Func<StateOperationResult> action)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }

            this.MenuItemIds[index] = menuItemId;
            this.MenuActions[index] = action;
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            return StateOperationResult.None();
        }

        public override StateOperationResult OnSelectIndex(int index)
        {
            return StateOperationResult.None();
        }

    }
}