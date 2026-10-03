using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.UI.Dialogs;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    /// <summary>
    /// Exchange Item: the item to hand over is picked, now the player picks who gets it. The
    /// four tiles next to the holder are shown (range 1..1), and a friend or NPC under the
    /// cursor flashes green. Confirming one with room in their bag gives the item; with a full
    /// bag the target's item dialog opens to pick what comes back, and the two are swapped.
    /// Either way -- and on backing out of that dialog -- the holder's item menu comes back,
    /// as in the original's ExchangeItemTargetState.
    /// </summary>
    public class SelectItemExchangeTargetState : IActionState
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public int SelectedItemIndex
        {
            get; private set;
        }

        public FDCreature TargetCreature
        {
            get; private set;
        }

        private FDRange range = null;

        // The state the item menu was opened from, handed back to it when it is rebuilt, so
        // cancelling out of it still returns to the action menu with the move remembered.
        private IActionState itemMenuBackState = null;

        // Set once a target is picked, so it stops flashing while the item changes hands.
        private bool targetChosen = false;

        public SelectItemExchangeTargetState(GameMain gameMain, int creatureId, int itemIndex, IActionState itemMenuBackState = null) : base(gameMain)
        {
            this.Creature = fdMap.GetCreatureById(creatureId);
            this.SelectedItemIndex = itemIndex;
            this.itemMenuBackState = itemMenuBackState;
        }

        public override void onEnter()
        {
            if (range == null)
            {
                DirectRangeFinder finder = new DirectRangeFinder(fdMap.Field, this.Creature.Position, 1, 1);
                range = finder.CalculateRange();
            }

            // Show the four tiles around the holder, then park the cursor on the lowest-id
            // friend / NPC to hand the item to. An exchange needs a second party, so the
            // holder is not a candidate; with nobody in range the cursor falls back to their
            // own tile.
            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.showActionTargetRange(this.Creature, range);

                FDCreature target = fdMap.GetPreferredFriendOrNpcTargetInRange(this.Creature, range, false);
                SlideCursorToTarget(this.Creature, target);
            });
        }

        public override void onExit()
        {
            gameMain.gameMap.clearAllIndicators();
            gameMain.gameMap.SetBlinkTargets(null);
        }

        // Handing an item over is a help, never a hurt.
        public override Color BlinkTint => Color.green;

        /// <summary>
        /// The friend / NPC under the cursor, when it stands next to the holder -- the one the
        /// next confirm would hand the item to.
        /// </summary>
        public override List<FDCreature> GetBlinkTargets()
        {
            if (targetChosen || range == null)
            {
                return null;
            }

            FDPosition cursor = gameMain.gameMap.GetCursorPosition();
            if (cursor == null || !range.Contains(cursor))
            {
                return null;
            }

            FDCreature target = GetExchangeTargetAt(cursor);
            return target != null ? new List<FDCreature> { target } : null;
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            // Outside the range: ignored. Leaving the state is the cancel key's job, so a
            // stray click on the map has no effect at all.
            if (targetChosen || range == null || !range.Contains(position))
            {
                return this;
            }

            FDCreature targetCreature = GetExchangeTargetAt(position);
            if (targetCreature == null)
            {
                return this;
            }

            targetChosen = true;
            gameMain.gameMap.SetBlinkTargets(null);
            this.TargetCreature = targetCreature;

            if (!targetCreature.IsItemsFull())
            {
                gameMain.creatureGiveItem(this.Creature, this.SelectedItemIndex, targetCreature);
                return BackToItemMenu();
            }

            // A full bag: pick which of the target's items comes back in exchange.
            gameMain.PushActivity(gameMain =>
            {
                gameMain.gameCanvas.ShowCreatureDialog(targetCreature, CreatureInfoType.SelectAllItem, OnSelectBackItem);
            });

            return this;
        }

        public override IActionState onUserCancelled()
        {
            if (targetChosen)
            {
                // The target's item dialog is up and answers the cancel itself.
                return this;
            }

            return BackToItemMenu();
        }

        /// <summary>
        /// The target's item dialog has closed. An item picked swaps with the one being handed
        /// over; a cancel exchanges nothing. Either way the holder's item menu comes back.
        /// </summary>
        private void OnSelectBackItem(int index)
        {
            if (index >= 0 && this.TargetCreature.GetItemAt(index) > 0)
            {
                gameMain.creatureExchangeItem(this.Creature, this.SelectedItemIndex, this.TargetCreature, index);
            }

            PlayerInterface.getDefault().onUpdateState(BackToItemMenu());
        }

        private IActionState BackToItemMenu()
        {
            return new MenuItemState(gameMain, this.Creature, itemMenuBackState);
        }

        /// <summary>The friend or NPC standing at <paramref name="position"/>, other than the holder.</summary>
        private FDCreature GetExchangeTargetAt(FDPosition position)
        {
            FDCreature target = fdMap.GetCreatureAt(position);
            if (target == null || target == this.Creature)
            {
                return null;
            }

            return (target.Faction == CreatureFaction.Friend || target.Faction == CreatureFaction.Npc) ? target : null;
        }
    }
}
