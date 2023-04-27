using System.Collections;
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

        public MenuItemState(IGameAction gameAction, int creatureId, FDPosition position) : base(gameAction, position)
        {
            this.CreatureId = creatureId;
            this.Creature = gameAction.GetCreature(creatureId);

            // Exchange
            this.SetMenu(0, MenuItemId.ItemExchange, IsMenuExchangeEnabled(), () =>
            {
                CreatureShowInfoPack pack = new CreatureShowInfoPack(this.Creature, CreatureInfoType.SelectAllItem);
                SendPack(pack);

                subState = SubActionState.SelectExchangeItem;
                return StateResult.None();
            });

            // Use
            this.SetMenu(1, MenuItemId.ItemUse, IsMenuUseEnabled(), () =>
            {
                CreatureShowInfoPack pack = new CreatureShowInfoPack(this.Creature, CreatureInfoType.SelectUseItem);
                SendPack(pack);

                subState = SubActionState.SelectUseItem;
                return StateResult.None();
            });

            // Equip
            this.SetMenu(2, MenuItemId.ItemEquip, IsMenuEquipEnabled(), () =>
            {
                CreatureShowInfoPack pack = new CreatureShowInfoPack(this.Creature, CreatureInfoType.SelectEquipItem);
                SendPack(pack);

                subState = SubActionState.SelectEquipItem;
                return StateResult.None();
            });

            // Discard
            this.SetMenu(3, MenuItemId.ItemDiscard, IsMenuDiscardEnabled(), () =>
            {
                CreatureShowInfoPack pack = new CreatureShowInfoPack(this.Creature, CreatureInfoType.SelectAllItem);
                SendPack(pack);

                subState = SubActionState.SelectDiscardItem;
                return StateResult.None();
            });
        }

        public override StateResult OnSelectIndex(int index)
        {
            if (index < 0)
            {
                // Cancel
                return StateResult.None();
            }

            switch (this.subState)
            {
                case SubActionState.SelectExchangeItem:
                    return OnSelectedExchangeItem(index);
                case SubActionState.SelectEquipItem:
                    return OnSelectedEquipItem(index);
                case SubActionState.SelectUseItem:
                    return OnSelectedUseItem(index);
                case SubActionState.SelectDiscardItem:
                    return OnSelectedDiscardItem(index);
                default:
                    return StateResult.Pop();
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
            return this.Creature.Data.HasItem();
        }

        private bool IsMenuEquipEnabled()
        {
            return this.Creature.HasEquipItem();
        }

        private bool IsMenuDiscardEnabled()
        {
            return this.Creature.Data.HasItem();
        }

        private StateResult OnSelectedExchangeItem(int index)
        {
            if (index < 0)
            {
                return StateResult.Pop();
            }

            // Selete Target Friend
            SelectItemExchangeTargetState exchangeTargetState = new SelectItemExchangeTargetState(gameAction, this.CreatureId, index);
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