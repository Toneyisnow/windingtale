using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class MenuItemState : MenuState
    {
        private enum SubActionState
        {
            None,
            SelectExchangeItem,
            SelectEquipItem,
            SelectUseItem,
            SelectDiscardItem,
        }

        public FDCreature Creature
        {
            get; private set;
        }


        private SubActionState subState;

        public MenuItemState(GameMain gameMain, IStateResultHandler stateHandler, FDCreature creature) : base(gameMain, stateHandler, creature.Position)
        {
            this.Creature = creature;

            // Exchange
            this.SetMenu(0, MenuItemId.ItemExchange, IsMenuExchangeEnabled(), () =>
            {
                ShowCreatureInfoActivity dialog = new ShowCreatureInfoActivity(gameMain, this.Creature, CreatureInfoType.SelectAllItem, OnSelectedExchangeItem);
                activityManager.Push(dialog);
            });

            // Use
            this.SetMenu(1, MenuItemId.ItemUse, IsMenuUseEnabled(), () =>
            {
                ShowCreatureInfoActivity dialog = new ShowCreatureInfoActivity(gameMain, this.Creature, CreatureInfoType.SelectUseItem, OnSelectedUseItem);
                activityManager.Push(dialog);
            });

            // Equip
            this.SetMenu(2, MenuItemId.ItemEquip, IsMenuEquipEnabled(), () =>
            {
                ShowCreatureInfoActivity dialog = new ShowCreatureInfoActivity(gameMain, this.Creature, CreatureInfoType.SelectEquipItem, OnSelectedEquipItem);
                activityManager.Push(dialog);
            });

            // Discard
            this.SetMenu(3, MenuItemId.ItemDiscard, IsMenuDiscardEnabled(), () =>
            {
                ShowCreatureInfoActivity dialog = new ShowCreatureInfoActivity(gameMain, this.Creature, CreatureInfoType.SelectAllItem, OnSelectedDiscardItem);
                activityManager.Push(dialog);
            });
        }

        private bool IsMenuExchangeEnabled()
        {
            if (this.Creature.Items.Count == 0)
            {
                return false;
            }

            // Near a friend
            List<FDCreature> adjacentFriends = gameMap.GetAdjacentFriends(this.Creature.Id);
            return (adjacentFriends != null && adjacentFriends.Count > 0);
        }

        private bool IsMenuUseEnabled()
        {
            return this.Creature.HasAnyItem();
        }

        private bool IsMenuEquipEnabled()
        {
            return this.Creature.HasEquipItem();
        }

        private bool IsMenuDiscardEnabled()
        {
            return this.Creature.HasAnyItem();
        }

        private void OnSelectedExchangeItem(int index)
        {
            if (index < 0)
            {
                // Cancel the selection
                return;
            }

            // Selete Target Friend
            SelectItemExchangeTargetState exchangeTargetState = new SelectItemExchangeTargetState(gameMain, stateHandler, this.Creature.Id, index);
            stateHandler.HandlePushState(exchangeTargetState);
        }

        private void OnSelectedUseItem(int index)
        {
            if (index < 0)
            {
                // Cancel the selection
                return;
            }

            int itemId = this.Creature.GetItemAt(index);
            if (itemId < 0)
            {
                // Item not found, should not reach here
                stateHandler.HandleClearStates();
            }

            // Selete Target Friend
            SelectItemUseTargetState selectTargetState = new SelectItemUseTargetState(gameMain, stateHandler, this.Creature.Id, index);
            stateHandler.HandlePushState(selectTargetState);
        }
        
        private void OnSelectedEquipItem(int index)
        {
            if (index < 0)
            {
                // Cancel the selection
                return;
            }

            this.Creature.EquipItemAt(index);

            // Reopen the item dialog
            ShowCreatureInfoActivity pack = new ShowCreatureInfoActivity(gameMain, this.Creature, CreatureInfoType.SelectEquipItem, OnSelectedEquipItem);
            activityManager.Push(pack);

        }

        private void OnSelectedDiscardItem(int index)
        {
            if (index < 0)
            {
                // Cancel the selection
                return;
            }

            // Discard the item
            this.Creature.RemoveItemAt(index);
        }

    }
}