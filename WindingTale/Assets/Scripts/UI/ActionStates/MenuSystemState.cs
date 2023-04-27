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
        public enum SubState
        {
            ConfirmMatching = 1,
            ConfirmRestAll = 2,
        }

        private SubState subState = 0;

        public MenuSystemState(IGameAction gameAction, FDPosition central) : base(gameAction, central)
        {
            // Matching
            this.SetMenu(0, MenuItemId.SystemMatching, false, () =>
            {
                subState = SubState.ConfirmMatching;
                
                MessageId message = MessageId.Create(MessageId.MessageTypes.Confirm, 1);
                TalkPack pack = new TalkPack(null, message);
                SendPack(pack);

                return StateResult.None();
            });

            // Record
            this.SetMenu(1, MenuItemId.SystemRecord, true, () =>
            {
                ActionState nextState = new MenuRecordState(gameAction, central);
                return StateResult.Push(nextState);
            });

            // Settings
            this.SetMenu(2, MenuItemId.SystemSettings, true, () =>
            {
                ActionState nextState = new MenuSettingsState(gameAction, central);
                return StateResult.Push(nextState);
            });

            // Rest All
            this.SetMenu(3, MenuItemId.SystemRestAll, true, () =>
            {
                subState = SubState.ConfirmRestAll;

                MessageId message = MessageId.Create(MessageId.MessageTypes.Confirm, 1);
                TalkPack pack = new TalkPack(null, message);
                SendPack(pack);
                
                return StateResult.None();
            });
        }

        public override StateResult OnSelectIndex(int index)
        {
            switch(subState)
            {
                case SubState.ConfirmRestAll:
                    return OnConfrimRestAll(index);
                default:
                    break;
            }

            return StateResult.None();
        }

        private StateResult OnConfrimRestAll(int index)
        {
            if (index == 1)
            {
                gameAction.DoCreatureAllRest();
                return StateResult.Clear();
            }

            return StateResult.None();
        }
    }
}