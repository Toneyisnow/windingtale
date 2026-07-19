using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;

using WindingTale.Core.Definitions;
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

            // "Has moved" must not be inferred from targetPos vs the creature's current
            // position alone: that only holds while the walk activity is still queued
            // (i.e. when ShowMoveRangeState builds this state). Once the walk has run,
            // Position == targetPos and the comparison silently reports "not moved" --
            // which is what happens when this state is rebuilt on the way back from the
            // item menu or a cancelled target selection. creature.HasMoved() is the
            // authoritative check afterwards (it compares PrePosition to Position).
            bool hasMoved = (targetPos != null && !targetPos.AreSame(creature.Position))
                || creature.HasMoved();

            // Magic
            this.SetMenu(0, MenuItemId.ActionMagic, IsMenuMagicEnabled(hasMoved), () =>
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
                // Hand ourselves over as the back state so cancelling out of the item
                // menu returns to THIS menu, keeping targetPosition and the enabled
                // flags that were computed for the post-move situation.
                return new MenuItemState(gameMain, creature, this);
            });

            // Rest
            this.SetMenu(3, MenuItemId.ActionRest, true, () =>
            {
                Debug.Log("Rest action.");
                IActionState nextState;
                if (treasure == null || treasure.HasOpened)
                {
                    // No chest underfoot, or one that has already been emptied: plain rest.
                    gameMain.creatureRest(creature);
                    nextState = new IdleState(gameMain);
                }
                else
                {
                    // Standing on an unopened chest: prompt to open the treasure or not
                    ItemDefinition treasureItem = DefinitionStore.Instance.GetItemDefinition(treasure.ItemId);

                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 2);
                    gameMain.PushActivity(new TalkActivity(message, creature, (result) =>
                    {
                        if (result == 1)
                        {
                            // TODO: Open the treasure, note: need to check whether item is full, if full, need to prompt for exchange
                            if (!creature.IsItemsFull())
                            {
                                creature.AddItem(treasure.ItemId);
                                treasure.Open();

                                FDMessage yes = FDMessage.Create(FDMessage.MessageTypes.Information, 3, strParam1: treasureItem.Name);
                                gameMain.InsertActivity(new TalkActivity(yes, creature));
                            }
                            else
                            {
                                // Item bag is full, need to prompt for exchange
                                FDMessage exchangeMessage = FDMessage.Create(FDMessage.MessageTypes.Confirm, 7);
                                gameMain.InsertActivity(new TalkActivity(exchangeMessage, creature, (confirmExchange) =>
                                {
                                    if (confirmExchange == 1)
                                    {
                                        gameMain.InsertActivity(gameMain =>
                                        {
                                            gameMain.gameCanvas.ShowCreatureDialog(creature, CreatureInfoType.SelectAllItem, (selectedIndex) =>
                                            {
                                                if (selectedIndex < 0 || selectedIndex >= creature.Items.Count)
                                                {
                                                    // Cancelled, no exchange
                                                    FDMessage noExchange = FDMessage.Create(FDMessage.MessageTypes.Information, 2);
                                                    gameMain.InsertActivity(new TalkActivity(noExchange, creature));

                                                    // No exchange, just rest
                                                    gameMain.creatureRest(creature);
                                                    this.playerInterface.onUpdateState(new IdleState(gameMain));
                                                }
                                                else
                                                {
                                                    // Exchange the selected item with the treasure item
                                                    int exchangeItemId = creature.Items[selectedIndex];
                                                    ItemDefinition exchangeItemDef = DefinitionStore.Instance.GetItemDefinition(exchangeItemId);
                                                    if (exchangeItemDef == null)
                                                    {
                                                        Debug.LogError("Exchange item definition not found for item ID: " + exchangeItemId);
                                                        return;
                                                    }
                                                    creature.RemoveItemAt(selectedIndex);
                                                    creature.AddItem(treasure.ItemId);
                                                    treasure.UpdateItem(exchangeItemId);

                                                    FDMessage yesExchange = FDMessage.Create(FDMessage.MessageTypes.Information, 6, strParam1: treasureItem.Name, strParam2: exchangeItemDef.Name);
                                                    gameMain.InsertActivity(new TalkActivity(yesExchange, creature));

                                                    // After exchange, just rest
                                                    gameMain.creatureRest(creature);
                                                    this.playerInterface.onUpdateState(new IdleState(gameMain));
                                                }
                                            });
                                        });
                                    }
                                    else
                                    {
                                        // No
                                        FDMessage no = FDMessage.Create(FDMessage.MessageTypes.Information, 2);
                                        gameMain.InsertActivity(new TalkActivity(no, creature));

                                        // No exchange, just rest
                                        gameMain.creatureRest(creature);
                                        this.playerInterface.onUpdateState(new IdleState(gameMain));
                                    }
                                }));
                            };
                        }
                        else
                        {
                            // No
                            FDMessage no = FDMessage.Create(FDMessage.MessageTypes.Information, 2);
                            gameMain.InsertActivity(new TalkActivity(no, creature));
                        }

                        // Regardless of the choice, return to the idle state after the dialog
                        gameMain.creatureRest(creature);
                        this.playerInterface.onUpdateState(new IdleState(gameMain));

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

        private bool IsMenuMagicEnabled(bool hasMoved)
        {
            return this.creature.CanSpellMagic() && (!hasMoved || this.creature.HasAfterMoveMagic());
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
                this.playerInterface.onUpdateState(magicTargetState);
            }
            else
            {
                // Go back to open magic info
                gameMain.gameCanvas.ShowCreatureDialog(creature, CreatureInfoType.SelectMagic, OnMagicSelected);
                
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
                    // ���ϵĵ������ˣ���Ҫ������
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Confirm, 7);
                    //PromptActivity prompt = new PromptActivity(message, OnExchangeTreasureConfirmed, creature);
                    //activityManager.Push(prompt);
                }
                else
                {
                    // �����XXX
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