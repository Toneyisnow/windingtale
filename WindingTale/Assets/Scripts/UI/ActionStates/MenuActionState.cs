using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
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

        private FDCreature creature;

        public int CreatureId
        {
            get; private set;
        }

        private ItemDefinition treasureItem = null;
        private SubActionState subState = SubActionState.SelectMagic;

        public MenuActionState(GameMain gameMain, IStateResultHandler stateHandler, int creatureId, FDPosition position)
            : base(gameMain, stateHandler, position)
        {
            this.CreatureId = creatureId;
            this.creature = gameMap.GetCreatureById(creatureId);

            // Magic
            this.SetMenu(0, MenuItemId.ActionMagic, IsMenuMagicEnabled(), () =>
            {
                CreatureShowInfoActivity activity = new CreatureShowInfoActivity(this.creature, CreatureInfoType.SelectMagic, OnMagicSelected);
                
                subState = SubActionState.SelectMagic;
            });

            // Attack
            this.SetMenu(1, MenuItemId.ActionAttack, IsMenuAttackEnabled(), () =>
            {
                SelectAttackTargetState attackState = new SelectAttackTargetState(gameMain, creature);
                stateHandler.HandlePushState(attackState);
            });

            // Item
            this.SetMenu(2, MenuItemId.ActionItems, IsMenuItemEnabled(), () =>
            {
                MenuItemState itemState = new MenuItemState(gameMain, this.CreatureId, this.Central);
                stateHandler.HandlePushState(itemState);
            });

            // Rest
            this.SetMenu(3, MenuItemId.ActionRest, true, () =>
            {
                gameMain.CreatureRest(creature);
                stateHandler.HandleClearStates();
            });

        }

        public override StateResult OnSelectCallback(int index)
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

        private bool IsMenuAttackEnabled()
        {
            bool canAttack = this.creature.CanAttack();
            FDCreature target = gameAction.GetPreferredAttackTargetInRange(this.creature.CreatureId);
            return canAttack && (target != null);
        }

        private bool IsMenuMagicEnabled()
        {
            return this.creature.Data.CanSpellMagic() && (!this.creature.HasMoved() || this.creature.Data.HasAfterMoveMagic());
        }

        private bool IsMenuItemEnabled()
        {
            return this.creature.Items.Count > 0;
        }

        #region Callback Index Methods

        private void OnMagicSelected(int index)
        {
            Debug.Log("MenuActionState: OnMagicSelected.");
            if (index < 0 || index >= this.creature.Magics.Count)
            {
                // Cancelled
                return StateResult.Pop();
            }

            int magicId = this.creature.Magics[index];
            MagicDefinition magicDefinition = DefinitionStore.Instance.GetMagicDefinition(magicId);

            if (magicDefinition != null && this.creature.CanSpellMagic() && magicDefinition.MpCost <= this.creature.Data.Mp)
            {
                // Enough MP to spell
                SelecteMagicTargetState magicTargetState = new SelecteMagicTargetState(gameAction, this.creature, magicDefinition);
                return StateResult.Push(magicTargetState);
            }
            else
            {
                // Go back to open magic info
                CreatureShowInfoPack pack = new CreatureShowInfoPack(this.creature, CreatureInfoType.SelectMagic);
                SendPack(pack);

                return StateResult.None();
            }
        }

        private void OnPickTreasureConfirmed(int index)
        {
            if (treasureItem == null)
            {
                stateHandler.HandleClearStates();
            }

            if (index == 1)
            {
                TalkPack pack = new TalkPack(this.creature.Clone(), Message.Create(Message.MessageTypes.Information, 3, treasureItem.ItemId));
                SendPack(pack);

                if (!this.creature.Data.IsItemsFull())
                {
                    // Pick up it
                    this.creature.Data.Items.Add(treasureItem.ItemId);
                    gameAction.PickTreasure(this.creature.Position);
                    gameAction.DoCreatureRest(this.CreatureId);
                    stateHandler.HandleClearStates();
                }
                else
                {
                    // Confirm to exchange it
                    subState = SubActionState.ConfirmExchangeTreature;
                    ////PromptPack prompt = new PromptPack(this.Creature.Definition.AnimationId, "");
                    ////SendPack(prompt);
                    TalkPack prompt = new TalkPack(this.creature.Clone(), Message.Create(Message.MessageTypes.Confirm, 7));
                    SendPack(prompt);
                }
            }
            else
            {
                // Then discard it
                TalkPack pack = new TalkPack(this.creature, Message.Create(Message.MessageTypes.Information, 2));
                SendPack(pack);

                gameAction.DoCreatureRest(this.CreatureId);
                stateHandler.HandleClearStates();
            }
        }

        private StateResult OnExchangeTreasureConfirmed(int index)
        {
            if (index == 0)
            {
                // Put it back
                TalkPack talkPack = new TalkPack(this.creature.Clone(), Message.Create(Message.MessageTypes.Information, 7));
                SendPack(talkPack);

                gameAction.DoCreatureRest(this.CreatureId);
                return StateResult.Clear();
            }

            subState = SubActionState.ConfirmExchangeSelecting;
            CreatureShowInfoPack pack = new CreatureShowInfoPack(this.creature, CreatureInfoType.SelectAllItem);
            SendPack(pack);

            return StateResult.None();

        }

        private StateResult OnExchangeTreasureSelected(int index)
        {
            if (index < 0)
            {
                // Cancelled, put it back
                TalkPack talkPack = new TalkPack(this.creature.Clone(), Message.Create(Message.MessageTypes.Information, 7));
                SendPack(talkPack);
            }
            else
            {
                // Picked up xxx, put back xxxx
                if (index >= 0 && index < this.creature.Data.Items.Count)
                {
                    int exchangeItemId = this.creature.Data.Items[index];
                    TalkPack talkPack = new TalkPack(this.creature.Clone(), Message.Create(Message.MessageTypes.Information, 6, treasureItem.ItemId, exchangeItemId));
                    SendPack(talkPack);

                    this.creature.Data.RemoveItemAt(index);
                    this.creature.Data.AddItem(treasureItem.ItemId);

                    // Add that item back to the treasure
                    gameAction.UpdateTreasure(this.creature.Position, exchangeItemId);
                }
            }

            gameAction.DoCreatureRest(this.CreatureId);
            return StateResult.Clear();
        }

        #endregion
    }
}
