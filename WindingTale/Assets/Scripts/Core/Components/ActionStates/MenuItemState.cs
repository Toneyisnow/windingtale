﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class MenuItemState : MenuState
    {
        private enum SubActionState
        {
            None,
            SelectExchangeItem,
            SelectEquipItem,
            SelectDiscardItem,
        }

        public FDCreature Creature
        {
            get; private set;
        }

        public int CreatureId
        {
            get; private set;
        }

        private SubActionState subState;

        public MenuItemState(IGameAction gameAction, int creatureId, FDPosition position) : base(gameAction, position)
        {
            this.CreatureId = creatureId;
            this.Creature = gameAction.GetCreature(creatureId);

            // Exchange
            this.SetMenu(0, MenuItemId.ItemExchange, IsMenuExchangeEnabled(), () =>
            {
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectAllItem);
                SendPack(pack);

                subState = SubActionState.SelectExchangeItem;
                return StateOperationResult.None();
            });

            // Use
            this.SetMenu(1, MenuItemId.ItemUse, IsMenuUseEnabled(), () =>
            {
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectUseItem);
                SendPack(pack);

                subState = SubActionState.SelectExchangeItem;
                return StateOperationResult.None();
            });

            // Equip
            this.SetMenu(2, MenuItemId.ItemUse, IsMenuEquipEnabled(), () =>
            {
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectEquipItem);
                SendPack(pack);

                subState = SubActionState.SelectEquipItem;
                return StateOperationResult.None();
            });

            // Discard
            this.SetMenu(3, MenuItemId.ItemDiscard, IsMenuDiscardEnabled(), () =>
            {
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectEquipItem);
                SendPack(pack);

                subState = SubActionState.SelectDiscardItem;
                return StateOperationResult.None();
            });
        }

        public override StateOperationResult OnSelectIndex(int index)
        {
            switch(this.subState)
            {
                case SubActionState.SelectExchangeItem:
                    return OnSelectedExchangeItem(index);
                case SubActionState.SelectEquipItem:
                    return OnSelectedEquipItem(index);
                case SubActionState.SelectDiscardItem:
                    return OnSelectedDiscardItem(index);
                default:
                    return StateOperationResult.Pop();
            }
        }

        private bool IsMenuExchangeEnabled()
        {
            if (this.Creature.Data.Items.Count == 0)
            {
                return false;
            }

            // Near a friend
            List<FDCreature> adjacentFriends = gameAction.GetAdjacentFriends(this.CreatureId);
            return (adjacentFriends != null && adjacentFriends.Count > 0);
        }

        private bool IsMenuUseEnabled()
        {
            return (this.Creature.Data.Items.Count == 0);
        }

        private bool IsMenuEquipEnabled()
        {
            return this.Creature.HasEquipItem();
        }

        private bool IsMenuDiscardEnabled()
        {
            return (this.Creature.Data.Items.Count > 0);
        }


        private StateOperationResult OnSelectedExchangeItem(int index)
        {
            // Selete Target Friend
            SelectItemExchangeTargetState exchangeTargetState = new SelectItemExchangeTargetState(gameAction, this.CreatureId, index);
            return StateOperationResult.Push(exchangeTargetState);
        }

        private StateOperationResult OnSelectedEquipItem(int index)
        {
            if (index < 0)
            {
                // Cancel the selection
                return StateOperationResult.None();
            }

            this.Creature.Data.EquipItemAt(index);

            // Reopen the item dialog
            ShowCreatureInfoPack pack = new ShowCreatureInfoPack(this.Creature.Data, CreatureInfoType.SelectEquipItem);
            SendPack(pack);

            subState = SubActionState.SelectEquipItem;
            return StateOperationResult.None();
        }

        private StateOperationResult OnSelectedDiscardItem(int index)
        {
            // Discard the item
            this.Creature.Data.RemoveItemAt(index);
            return StateOperationResult.Clear();
        }



    }
}