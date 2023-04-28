using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Objects;
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

        public int CreatureId
        {
            get; private set;
        }

        private SubActionState subState;

        public MenuItemState(GameMain gameMain, int creatureId, FDPosition position) : base(gameMain, position)
        {
            this.CreatureId = creatureId;
            this.Creature = gameMap.GetCreatureById(creatureId);

            // Exchange
            this.SetMenu(0, MenuItemId.ItemExchange, IsMenuExchangeEnabled(), () =>
            {
                ShowCreatureInfoDialog dialog = new ShowCreatureInfoDialog(this.Creature, CreatureInfoType.SelectAllItem, OnSelectedExchangeItem);
                activityManager.Push(dialog);

                return null;
            });

            // Use
            this.SetMenu(1, MenuItemId.ItemUse, IsMenuUseEnabled(), () =>
            {
                ShowCreatureInfoDialog dialog = new ShowCreatureInfoDialog(this.Creature, CreatureInfoType.SelectUseItem, OnSelectedUseItem);
                PushActivity(dialog);

                return null;
            });

            // Equip
            this.SetMenu(2, MenuItemId.ItemEquip, IsMenuEquipEnabled(), () =>
            {
                ShowCreatureInfoDialog dialog = new CreatureShowInfoPack(this.Creature, CreatureInfoType.SelectEquipItem, OnSelectedEquipItem);
                PushActivity(dialog);

                subState = SubActionState.SelectEquipItem;
                return null;
            });

            // Discard
            this.SetMenu(3, MenuItemId.ItemDiscard, IsMenuDiscardEnabled(), () =>
            {
                ShowCreatureInfoDialog dialog = new CreatureShowInfoPack(this.Creature, CreatureInfoType.SelectAllItem, OnSelectedDiscardItem);
                PushActivity(dialog);

                subState = SubActionState.SelectDiscardItem;
                return null;
            });
        }

        private bool IsMenuExchangeEnabled()
        {
            if (this.Creature.Items.Count == 0)
            {
                return false;
            }

            // Near a friend
            List<FDCreature> adjacentFriends = gameMap.GetAdjacentFriends(this.CreatureId);
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

        private StateResult OnSelectedExchangeItem(int index)
        {
            if (index < 0)
            {
                return StateResult.Pop();
            }

            // Selete Target Friend
            SelectItemExchangeTargetState exchangeTargetState = new SelectItemExchangeTargetState(gameMain, this.CreatureId, index);
            return StateResult.Push(exchangeTargetState);
        }

        private StateResult OnSelectedUseItem(int index)
        {
            int itemId = this.Creature.Data.GetItemAt(index);
            if (itemId < 0)
            {
                // Item not found, should not reach here
                return StateResult.Clear();
            }

            // Selete Target Friend
            SelectItemUseTargetState selectTargetState = new SelectItemUseTargetState(gameAction, this.CreatureId, index);
            return StateResult.Push(selectTargetState);
        }
        
        private StateResult OnSelectedEquipItem(int index)
        {
            if (index < 0)
            {
                // Cancel the selection
                return StateResult.None();
            }

            this.Creature.Data.EquipItemAt(index);

            // Reopen the item dialog
            CreatureShowInfoPack pack = new CreatureShowInfoPack(this.Creature, CreatureInfoType.SelectEquipItem);
            SendPack(pack);

            subState = SubActionState.SelectEquipItem;
            return StateResult.None();
        }

        private StateResult OnSelectedDiscardItem(int index)
        {
            // Discard the item
            this.Creature.Data.RemoveItemAt(index);
            return StateResult.None();
        }



    }
}