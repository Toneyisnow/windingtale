using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.UI.Dialogs;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.Core.Definitions;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
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


        public MenuItemState(GameMain gameMain, FDCreature creature) 
            : base(gameMain, creature.Position, new MenuActionState(gameMain, creature, creature.Position))
        {
            this.Creature = creature;

            // Exchange
            this.SetMenu(0, MenuItemId.ItemExchange, IsMenuExchangeEnabled(), () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    gameMain.gameCanvas.ShowCreatureDialog(creature, CreatureInfoType.SelectAllItem, OnSelectedExchangeItem);
                });

                return this;
            });

            // Use
            this.SetMenu(1, MenuItemId.ItemUse, IsMenuUseEnabled(), () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    gameMain.gameCanvas.ShowCreatureDialog(creature, CreatureInfoType.SelectUseItem, OnSelectedUseItem);
                });
                return this;
            });

            // Equip
            this.SetMenu(2, MenuItemId.ItemEquip, IsMenuEquipEnabled(), () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    gameMain.gameCanvas.ShowCreatureDialog(creature, CreatureInfoType.SelectEquipItem, OnSelectedEquipItem);
                });
                return this;
            });

            // Discard
            this.SetMenu(3, MenuItemId.ItemDiscard, IsMenuDiscardEnabled(), () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    gameMain.gameCanvas.ShowCreatureDialog(creature, CreatureInfoType.SelectAllItem, OnSelectedDiscardItem);
                });
                return this;
            });
        }

        private bool IsMenuExchangeEnabled()
        {
            if (this.Creature.Items.Count == 0)
            {
                return false;
            }

            // Near a friend
            List<FDCreature> adjacentFriends = fdMap.GetAdjacentFriends(this.Creature);
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
            //SelectItemExchangeTargetState exchangeTargetState = new SelectItemExchangeTargetState(gameMain, stateHandler, this.Creature.Id, index);
            //stateHandler.HandlePushState(exchangeTargetState);
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
                //// stateHandler.HandleClearStates();
                return;
            }

            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (!item.IsUsable())
            {
                //// Item is not usable item, should not reach here
                return;
            }

            // Selete Target Friend
            SelectItemUseTargetState selectTargetState = new SelectItemUseTargetState(gameMain, this.Creature.Id, index);
            PlayerInterface.getDefault().onUpdateState(selectTargetState);

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
            gameMain.PushActivity(gameMain =>
            {
                gameMain.gameCanvas.ShowCreatureDialog(this.Creature, CreatureInfoType.SelectEquipItem, OnSelectedEquipItem);
            });
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

