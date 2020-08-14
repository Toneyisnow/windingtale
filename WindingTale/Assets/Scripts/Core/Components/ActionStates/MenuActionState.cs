using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class MenuActionState : MenuState
    {
        private enum SubActionState
        {
            SelectMagic,
            ConfirmPickTreasure,
        }

        public FDCreature Creature
        {
            get; private set;
        }

        public int CreatureId
        {
            get; private set;
        }

        public FDPosition Position
        {
            get; private set;
        }

        private SubActionState subState = SubActionState.SelectMagic;

        private Action<int> selectIndexDelegate = null;

        public MenuActionState(IGameAction gameAction, int creatureId, FDPosition position)
            : base(gameAction, position)
        {
            this.CreatureId = creatureId;
            this.Creature = gameAction.GetCreature(creatureId);
            this.Position = position;

            this.SetMenu(0, MenuItemId.ActionMagic, () =>
            {
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectMagic);
                gameAction.GetCallback().OnCallback(pack);

                this.SelectIndexDelegate = selectMagicDelegate;
                return StateOperationResult.None();
            });

            this.SetMenu(0, MenuItemId.ActionItems, () =>
            {

                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectMagic);
                gameAction.GetCallback().OnCallback(pack);

                return StateOperationResult.None();
            });



        }

        public override void OnEnter()
        {
            // Show Action Menu
            bool[] enabled = new bool[4] { false, true, true, true };
            ShowMenuPack pack = new ShowMenuPack(MenuId.ActionMenu, enabled, this.Position);
            gameAction.GetCallback().OnCallback(pack);
        }

        public override void OnExit()
        {
            // Close Action Menu
        }

        public override StateOperationResult OnSelectIndex(int index)
        {
            if (this.subState == SubActionState.SelectMagic)
            {

            }

            return StateOperationResult.None();
        }

        /// <summary>
        /// Handle the result of selected magic
        /// </summary>
        private Func<int, StateOperationResult> selectMagicDelegate = (index) => {


            return StateOperationResult.None();
        };

    }
}
