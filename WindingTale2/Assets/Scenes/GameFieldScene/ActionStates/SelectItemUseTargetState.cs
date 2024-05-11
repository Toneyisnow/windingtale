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
            this.Creature = map.GetCreatureById(creatureId);
            this.SelectedItemIndex = itemIndex;
        }

        public override void onEnter()
        {
            if (itemRange == null)
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(map.Field, this.Creature.Position, 1);
                itemRange = rangeFinder.CalculateRange();
            }

            // Display the attack range on the UI.
            //ShowRangeActivity activity = new ShowRangeActivity(itemRange.ToList());
            //activityManager.Push(activity);
        }

        public override void onExit()
        {
            //ClearRangeActivity clear = new ClearRangeActivity();
            //activityManager.Push(clear);
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            // Selecte position must be included in the range
            if (!itemRange.Contains(position))
            {
                /// stateHandler.HandlePopState();
                return this;
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = map.GetCreatureAt(position);
            if (targetCreature == null || targetCreature.Faction == CreatureFaction.Enemy)
            {
                return this;
            }

            //gameMain.CreatureUseItem(this.Creature, this.SelectedItemIndex, position);
            //stateHandler.HandleClearStates();

            return this;
        }

        public override IActionState onUserCancelled()
        {
            throw new System.NotImplementedException();
        }
    }
}