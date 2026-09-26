using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene.ActionStates;
using WindingTale.Scenes.GameFieldScene;
using static UnityEngine.GraphicsBuffer;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class SelectItemUseTargetState : IActionState
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public int CreatureId
        {
            get; private set;
        }

        public int SelectedItemIndex
        {
            get; private set;
        }

        private FDRange itemRange = null;

        // Set once the item is used, so the target stops flashing before it takes effect.
        private bool itemUsed = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameAction"></param>
        public SelectItemUseTargetState(GameMain gameMain, int creatureId, int itemIndex) : base(gameMain)
        {
            this.CreatureId = creatureId;
            this.Creature = fdMap.GetCreatureById(creatureId);
            this.SelectedItemIndex = itemIndex;
        }

        public override void onEnter()
        {
            if (itemRange == null)
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(fdMap.Field, this.Creature.Position, 1);
                itemRange = rangeFinder.CalculateRange();
            }


            // Display the usage range on the UI, then park the cursor on the lowest-id
            // friend / NPC in it, self included (own tile when the range holds none).
            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.showActionTargetRange(this.Creature, itemRange);

                FDCreature target = fdMap.GetPreferredFriendOrNpcTargetInRange(this.Creature, itemRange, true);
                SlideCursorToTarget(this.Creature, target);
            });
        }

        public override void onExit()
        {
            // Clear move range on UI
            gameMain.gameMap.clearAllIndicators();
            gameMain.gameMap.SetBlinkTargets(null);
        }

        // Items are only ever used on one's own side.
        public override Color BlinkTint => Color.green;

        /// <summary>
        /// The friend / NPC under the cursor, when it is inside the usage range -- the one
        /// the next confirm would use the item on.
        /// </summary>
        public override List<FDCreature> GetBlinkTargets()
        {
            if (itemUsed || itemRange == null)
            {
                return null;
            }

            FDPosition cursor = gameMain.gameMap.GetCursorPosition();
            if (cursor == null || !itemRange.Contains(cursor))
            {
                return null;
            }

            FDCreature target = fdMap.GetCreatureAt(cursor);
            if (target == null || target.Faction == CreatureFaction.Enemy)
            {
                return null;
            }

            return new List<FDCreature> { target };
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            // Selecte position must be included in the range. Outside it: ignored.
            // Leaving the state is the cancel key's job, so a stray click does nothing.
            if (!itemRange.Contains(position))
            {
                return this;
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = fdMap.GetCreatureAt(position);
            if (targetCreature == null || targetCreature.Faction == CreatureFaction.Enemy)
            {
                return this;
            }

            // Do the use item action
            itemUsed = true;
            gameMain.gameMap.SetBlinkTargets(null);
            this.gameMain.creatureUseItem(this.Creature, SelectedItemIndex, targetCreature);
            return new IdleState(gameMain);
        }

        public override IActionState onUserCancelled()
        {
            return new MenuItemState(gameMain, this.Creature);
        }
    }
}