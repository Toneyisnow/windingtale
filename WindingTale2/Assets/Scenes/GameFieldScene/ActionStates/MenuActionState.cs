using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using UnityEditor.SceneManagement;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.UI.Dialogs;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
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

        private FDPosition targetPosition = null;

        private FDTreasure treasure = null;

        private ItemDefinition treasureItem = null;

        public MenuActionState(GameMain gameMain, FDCreature creature, FDPosition targetPos)
            : base(gameMain, targetPos, new ShowMoveRangeState(gameMain, creature))
        {
            this.creature = creature;
            this.targetPosition = targetPos;
            this.treasure = fdMap.GetTreasureAt(targetPos);

            // Magic
            this.SetMenu(0, MenuItemId.ActionMagic, IsMenuMagicEnabled(), () =>
            {
                gameMain.PushActivity(gameMain =>
                {
                    gameMain.gameCanvas.ShowCreatureDialog(creature, CreatureInfoType.SelectMagic, OnMagicSelected);
                });
                return this;
            });

            // Attack
            this.SetMenu(1, MenuItemId.ActionAttack, IsMenuAttackEnabled(), () =>
            {
                Debug.Log("Attack clicked");
                return new SelectAttackTargetState(gameMain, creature);
            });

            // Item
            this.SetMenu(2, MenuItemId.ActionItems, IsMenuItemEnabled(), () =>
            {
                return new MenuItemState(gameMain, creature);
            });

            // Rest
            this.SetMenu(3, MenuItemId.ActionRest, true, () =>
            {
                Debug.Log("Rest action.");
                IActionState nextState;
                if (treasure == null || !treasure.HasOpened)
                {
                    gameMain.creatureRest(creature);
                    nextState = new IdleState(gameMain);
                }
                else
                {
                    // 发现宝箱，需要打开吗
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 2);
                    gameMain.PushActivity(new TalkActivity(message, creature, (result) =>
                    {
                        if (result == 1)
                        {
                            // Open
                            // TODO: Open the treasure

                            FDMessage yes = FDMessage.Create(FDMessage.MessageTypes.Information, 3);
                            gameMain.InsertActivity(new TalkActivity(yes, creature));
                        }
                        else
                        {
                            // No
                            FDMessage no = FDMessage.Create(FDMessage.MessageTypes.Information, 2);
                            gameMain.InsertActivity(new TalkActivity(no, creature));
                        }

                    }));
                    nextState = this;
                }
                return nextState;
            });
        }

        #region Public Methods


        public override IActionState onUserCancelled()
        {
            return new ShowMoveRangeState(gameMain, creature);
        }

        #endregion

        private bool IsMenuAttackEnabled()
        {
            bool canAttack = this.creature.CanAttack();
            FDCreature target = fdMap.GetPreferredAttackTargetInRange(this.creature, this.targetPosition);
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
            Debug.Log("MenuActionState: OnMagicSelected. Index = " + index);
            if (index < 0 || index >= creature.Magics.Count)
            {
                // Cancelled
                // stateHandler.HandlePopState();
                return;
            }

            int magicId = creature.Magics[index];
            MagicDefinition magicDefinition = DefinitionStore.Instance.GetMagicDefinition(magicId);

            if (magicDefinition != null && creature.CanSpellMagic() && magicDefinition.MpCost <= creature.Mp)
            {
                // Enough MP to spell
                SelecteMagicTargetState magicTargetState = new SelecteMagicTargetState(gameMain, creature, magicDefinition);
                PlayerInterface.getDefault().onUpdateState(magicTargetState);

                //stateHandler.HandlePushState(magicTargetState);
            }
            else
            {
                // Go back to open magic info
                //ShowCreatureInfoActivity activity = new ShowCreatureInfoActivity(gameMain, creature, CreatureInfoType.SelectMagic, OnMagicSelected);
                //activityManager.Push(activity);
            }
        }

        private void OnPickTreasureConfirmed(int index)
        {
            //CallbackActivity completed = new CallbackActivity(() =>
            //{
            //    gameMain.CreatureRest(creature);
            //    stateHandler.HandleClearStates();
            //});

            if (index == 0)
            {
                // Put it back
                //TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 15), creature);
                //activityManager.Push(talk);

                //activityManager.Push(completed);
                return;
            }
            else
            {

                if (creature.IsItemsFull())
                {
                    // 身上的道具满了，需要交换吗
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 7);
                    //PromptActivity prompt = new PromptActivity(message, OnExchangeTreasureConfirmed, creature);
                    //activityManager.Push(prompt);
                }
                else
                {
                    // 获得了XXX
                //    TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 11), creature);
                //    activityManager.Push(talk);

                //    activityManager.Push(completed);
                }
            }
        }

        private void OnExchangeTreasureConfirmed(int index)
        {
            if (index == 0)
            {
                // Put it back
                //TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 15), creature);
                //activityManager.Push(talk);

                //CallbackActivity callback = new CallbackActivity(() =>
                //{
                //    gameMain.CreatureRest(creature);
                //    stateHandler.HandleClearStates();
                //});
                //activityManager.Push(callback);
                return;
            }

            //ShowCreatureInfoActivity show = new ShowCreatureInfoActivity(gameMain, creature, CreatureInfoType.SelectAllItem, OnExchangeTreasureSelected);
            //activityManager.Push(show);
        }

        private void OnExchangeTreasureSelected(int index)
        {
            if (index < 0)
            {
                // Cancelled, put it back
                //TalkActivity talk = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 15), creature);
                //activityManager.Push(talk);
            }
            else
            {
                // Picked up xxx, put back xxxx
                if (index >= 0 && index < creature.Items.Count)
                {
                    int exchangeItemId = creature.Items[index];
                    //TalkActivity talkPack = new TalkActivity(FDMessage.Create(FDMessage.MessageTypes.Information, 6, treasureItem.ItemId, exchangeItemId), creature);
                    //activityManager.Push(talkPack);

                    creature.RemoveItemAt(index);
                    creature.AddItem(treasureItem.ItemId);

                    // Add that item back to the treasure
                    treasure.UpdateItem(exchangeItemId);
                }
            }

            //CallbackActivity callback = new CallbackActivity(() =>
            //{
            //    gameMain.CreatureRest(creature);
            //    stateHandler.HandleClearStates();
            //});
            //activityManager.Push(callback);
        }

        #endregion
    

        public override void onEnter()
        {
            // Show the Menu Buttons
            Debug.Log("MenuActionState: onEnter");

            base.onEnter();
        }

        public override void onExit()
        {
            // Close the Menu Buttons
            base.onExit();

            
        }

    }
}