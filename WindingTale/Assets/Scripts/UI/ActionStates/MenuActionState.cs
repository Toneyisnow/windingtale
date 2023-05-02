﻿using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
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

        private FDCreature creature = null;

        private FDTreasure treasure = null;

        private ItemDefinition treasureItem = null;
        private SubActionState subState = SubActionState.SelectMagic;

        public MenuActionState(GameMain gameMain, IStateResultHandler stateHandler, FDCreature creature)
            : base(gameMain, stateHandler, creature.Position)
        {
            this.creature = creature;

            // Magic
            this.SetMenu(0, MenuItemId.ActionMagic, IsMenuMagicEnabled(), () =>
            {
                ShowCreatureInfoActivity activity = new ShowCreatureInfoActivity(gameMain, creature, CreatureInfoType.SelectMagic, OnMagicSelected);
                activityManager.Push(activity);
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
                MenuItemState itemState = new MenuItemState(gameMain, stateHandler, creature);
                stateHandler.HandlePushState(itemState);
            });

            // Rest
            this.SetMenu(3, MenuItemId.ActionRest, true, () =>
            {
                FDTreasure treature = gameMap.GetTreatureAt(creature.Position);
                if (treasure == null)
                {
                    gameMain.CreatureRest(creature);
                    stateHandler.HandleClearStates();
                }
                else
                {
                    // 发现宝箱，需要打开吗
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 2);
                    PromptActivity prompt = new PromptActivity(message, OnPickTreasureConfirmed, creature);
                    activityManager.Push(prompt);
                }
                
                
            });
        }

        private bool IsMenuAttackEnabled()
        {
            bool canAttack = this.creature.CanAttack();
            FDCreature target = gameMap.GetPreferredAttackTargetInRange(this.creature);
            return canAttack && (target != null);
        }

        private bool IsMenuMagicEnabled()
        {
            return this.creature.CanSpellMagic() && (!this.creature.HasMoved() || this.creature.HasAfterMoveMagic());
        }

        private bool IsMenuItemEnabled()
        {
            return this.creature.Items.Count > 0;
        }

        #region Callback Index Methods

        private void OnMagicSelected(int index)
        {
            Debug.Log("MenuActionState: OnMagicSelected.");
            if (index < 0 || index >= creature.Magics.Count)
            {
                // Cancelled
                stateHandler.HandlePopState();
                return;
            }

            int magicId = creature.Magics[index];
            MagicDefinition magicDefinition = DefinitionStore.Instance.GetMagicDefinition(magicId);

            if (magicDefinition != null && creature.CanSpellMagic() && magicDefinition.MpCost <= creature.Mp)
            {
                // Enough MP to spell
                SelecteMagicTargetState magicTargetState = new SelecteMagicTargetState(gameMain, creature, magicDefinition);
                stateHandler.HandlePushState(magicTargetState);
            }
            else
            {
                // Go back to open magic info
                ShowCreatureInfoActivity activity = new ShowCreatureInfoActivity(gameMain, creature, CreatureInfoType.SelectMagic, OnMagicSelected);
                activityManager.Push(activity);
            }
        }

        private void OnPickTreasureConfirmed(int index)
        {
            CallbackActivity completed = new CallbackActivity(() =>
            {
                gameMain.CreatureRest(creature);
                stateHandler.HandleClearStates();
            });
            
            if (index == 0)
            {
                // Put it back
                TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 15), creature);
                activityManager.Push(talk);

                activityManager.Push(completed);
                return;
            }
            else
            {
                if (creature.IsItemsFull())
                {
                    // 身上的道具满了，需要交换吗
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 7);
                    PromptActivity prompt = new PromptActivity(message, OnExchangeTreasureConfirmed, creature);
                    activityManager.Push(prompt);
                }
                else
                {
                    // 获得了XXX
                    TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 11), creature);
                    activityManager.Push(talk);

                    activityManager.Push(completed);
                }
            }
        }

        private void OnExchangeTreasureConfirmed(int index)
        {
            if (index == 0)
            {
                // Put it back
                TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 15), creature);
                activityManager.Push(talk);

                CallbackActivity callback = new CallbackActivity(() =>
                {
                    gameMain.CreatureRest(creature);
                    stateHandler.HandleClearStates();
                });
                activityManager.Push(callback);

                return;
            }

            ShowCreatureInfoActivity show = new ShowCreatureInfoActivity(gameMain, creature, CreatureInfoType.SelectAllItem, OnExchangeTreasureSelected);
            activityManager.Push(show);
        }

        private void OnExchangeTreasureSelected(int index)
        {
            if (index < 0)
            {
                // Cancelled, put it back
                TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 15), creature);
                activityManager.Push(talk);
            }
            else
            {
                // Picked up xxx, put back xxxx
                if (index >= 0 && index < creature.Items.Count)
                {
                    int exchangeItemId = creature.Items[index];
                    TalkActivity talkPack = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 6, treasureItem.ItemId, exchangeItemId), creature);
                    activityManager.Push(talkPack);

                    creature.RemoveItemAt(index);
                    creature.AddItem(treasureItem.ItemId);

                    // Add that item back to the treasure
                    gameMain.UpdateTreasure(creature.Position, exchangeItemId);
                }
            }

            CallbackActivity callback = new CallbackActivity(() =>
            {
                gameMain.CreatureRest(creature);
                stateHandler.HandleClearStates();
            });
            activityManager.Push(callback);
        }

        #endregion
    }
}
