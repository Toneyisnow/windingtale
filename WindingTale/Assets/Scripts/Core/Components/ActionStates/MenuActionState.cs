using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class MenuActionState : MenuState
    {
        private enum SubActionState
        {
            None,
            SelectMagic,
            ConfirmPickTreasure,
            ConfirmExchangeTreature,
            ConfirmExchangeSelecting,
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

            // Magic
            this.SetMenu(0, MenuItemId.ActionMagic, () =>
            {
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectMagic);
                gameAction.GetCallback().OnCallback(pack);

                subState = SubActionState.SelectMagic;
                return StateOperationResult.None();
            });

            // Attack
            this.SetMenu(1, MenuItemId.ActionAttack, () =>
            {
                SelectAttackTargetState attackState = new SelectAttackTargetState(gameAction, this.Creature);
                return StateOperationResult.Push(attackState);
            });

            // Item
            this.SetMenu(2, MenuItemId.ActionItems, () =>
            {
                MenuItemState itemState = new MenuItemState(gameAction, this.Position);
                return StateOperationResult.Push(itemState);
            });

            // Rest
            this.SetMenu(1, MenuItemId.ActionRest, () =>
            {
                // Check Treasure
                if (this.Creature.Position == FDPosition.Invalid())
                {
                    subState = SubActionState.ConfirmPickTreasure;
                    return StateOperationResult.None();
                }

                gameAction.DoCreatureRest(this.CreatureId);
                return StateOperationResult.Clear();
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
            switch(this.subState)
            {
                case SubActionState.SelectMagic:
                    return OnMagicSelected(index);
                case SubActionState.ConfirmPickTreasure:
                    return OnPickTreasureConfirmed(index);
                case SubActionState.ConfirmExchangeTreature:
                    return OnExchangeTreasureConfirmed(index);
                case SubActionState.ConfirmExchangeSelecting:
                    return OnExchangeTreasureSelected(index);
                default:
                    return null;
            }
        }

        private StateOperationResult OnMagicSelected(int index)
        {
            Debug.Log("MenuActionState: OnMagicSelected.");
            if (index < 0 || index >= this.Creature.Data.Magics.Count)
            {
                // Cancelled
                return StateOperationResult.Pop();
            }

            int magicId = this.Creature.Data.Magics[index];
            MagicDefinition magicDefinition = DefinitionStore.Instance.GetMagicDefinition(magicId);

            if (magicDefinition != null && this.Creature.Data.CanSpellMagic() && magicDefinition.MpCost <= this.Creature.Data.Mp)
            {
                // Enough MP to spell
                SelecteMagicTargetState magicTargetState = new SelecteMagicTargetState(gameAction, this.Creature, magicDefinition);
                return StateOperationResult.Push(magicTargetState);
            }
            else
            {
                // Go back to open magic info
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectMagic);
                SendPack(pack);

                return StateOperationResult.None();
            }
        }

        private StateOperationResult OnPickTreasureConfirmed(int index)
        {
            if (index == 1)
            {
                TalkPack pack = new TalkPack("");
                SendPack(pack);

                if (!this.Creature.Data.IsItemsFull())
                {
                    // Pick up it
                    gameAction.PickTreasure(this.CreatureId);
                    gameAction.DoCreatureRest(this.CreatureId);
                    return StateOperationResult.Clear();
                }
                else
                {
                    // Confirm to exchange it
                    subState = SubActionState.ConfirmExchangeTreature;
                    PromptPack prompt = new PromptPack();
                    SendPack(prompt);

                    return StateOperationResult.None();
                }
            }
            else
            {
                // Then discard it
                TalkPack pack = new TalkPack("");
                SendPack(pack);

                gameAction.DoCreatureRest(this.CreatureId);
                return StateOperationResult.Clear();
            }
        }

        private StateOperationResult OnExchangeTreasureConfirmed(int index)
        {
            if (index == 0)
            {
                // Put it back
                TalkPack talkPack = new TalkPack("");
                SendPack(talkPack);

                gameAction.DoCreatureRest(this.CreatureId);
                return StateOperationResult.Clear();
            }

            subState = SubActionState.ConfirmExchangeSelecting;
            ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectAllItem);
            SendPack(pack);

            return StateOperationResult.None();

        }

        private StateOperationResult OnExchangeTreasureSelected(int index)
        {
            if (index < 0)
            {
                // Cancelled, put it back
                TalkPack talkPack = new TalkPack("");
                SendPack(talkPack);
            }
            else
            {
                // Picked up xxx, put back xxxx
                TalkPack talkPack = new TalkPack("");
                SendPack(talkPack);

                int itemId = 0;
                this.Creature.Data.RemoveItemAt(index);
                this.Creature.Data.AddItem(itemId);
            }

            gameAction.DoCreatureRest(this.CreatureId);
            return StateOperationResult.Clear();
        }
    }
}
