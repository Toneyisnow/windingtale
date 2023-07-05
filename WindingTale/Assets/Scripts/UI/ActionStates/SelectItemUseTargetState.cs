using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;
using WindingTale.UI.Activities;

namespace WindingTale.UI.ActionStates
{
    public class SelectItemUseTargetState : ActionState
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
        public SelectItemUseTargetState(GameMain gameMain, IStateResultHandler stateHandler, int creatureId, int itemIndex) : base(gameMain, stateHandler)
        {
            this.CreatureId = creatureId;
            this.Creature = gameMap.GetCreatureById(creatureId);
            this.SelectedItemIndex = itemIndex;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (itemRange == null)
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(gameMap.Field, this.Creature.Position, 1);
                itemRange = rangeFinder.CalculateRange();
            }

            // Display the attack range on the UI.
            ShowRangeActivity activity = new ShowRangeActivity(itemRange.ToList());
            activityManager.Push(activity);
        }

        public override void OnExit()
        {
            base.OnExit();
            ClearRangeActivity clear = new ClearRangeActivity();
            activityManager.Push(clear);
        }

        public override void OnSelectPosition(FDPosition position)
        {
            // Selecte position must be included in the range
            if (!itemRange.Contains(position))
            {
                stateHandler.HandlePopState();
                return;
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = gameMap.GetCreatureAt(position);
            if(targetCreature == null || targetCreature.Faction == CreatureFaction.Enemy)
            {
                return;
            }

            gameMain.CreatureUseItem(this.Creature, this.SelectedItemIndex, position);
            stateHandler.HandleClearStates();
        }
    }
}