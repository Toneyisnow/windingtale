using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using UnityEditor.SceneManagement;
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


            // Display the usage range on the UI.
            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.showActionTargetRange(this.Creature, itemRange);
            });
        }

        public override void onExit()
        {
            // Clear move range on UI
            gameMain.gameMap.clearAllIndicators();
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            // Selecte position must be included in the range
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
            this.gameMain.creatureUseItem(this.Creature, SelectedItemIndex, targetCreature);
            return new IdleState(gameMain);
        }

        public override IActionState onUserCancelled()
        {
            return new MenuItemState(gameMain, this.Creature);
        }
    }
}