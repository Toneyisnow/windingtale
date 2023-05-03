using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.TestTools;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;
using WindingTale.UI.Activities;

namespace WindingTale.UI.ActionStates
{
    public class SelectItemExchangeTargetState : ActionState
    {
        public enum SubState
        {
            SelectExchangeItem = 1,
        }

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

        private SubState subState = 0;

        private FDRange range = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameAction"></param>
        public SelectItemExchangeTargetState(GameMain gameMain, IStateResultHandler stateHandler, int creatureId, int itemIndex) : base(gameMain, stateHandler)
        {
            this.Creature = gameMap.GetCreatureById(creatureId);
            this.SelectedItemIndex = itemIndex;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (range == null)
            {
                DirectRangeFinder finder = new DirectRangeFinder(gameMap.Field, this.Creature.Position, 1, 1);
                range = finder.CalculateRange();
            }
            ShowRangeActivity showRange = new ShowRangeActivity(gameMain, range.ToList());
            activityManager.Push(showRange);
        }

        public override void OnExit()
        {
            base.OnExit();

            ClearRangeActivity clear = new ClearRangeActivity();
            activityManager.Push(clear);
        }

        public override void OnSelectPosition(FDPosition position)
        {
            if (range == null || !range.Contains(position))
            {
                stateHandler.HandlePopState();
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = gameMap.GetCreatureAt(position);
            if (targetCreature == null || targetCreature.Faction != CreatureFaction.Friend)
            {
                return;
            }

            if (!targetCreature.IsItemsFull())
            {
                gameMain.CreatureExchangeItem(this.Creature, this.SelectedItemIndex, targetCreature);
                stateHandler.HandleClearStates();
            }
            else
            {
                subState = SubState.SelectExchangeItem;
                this.TargetCreature = targetCreature;

                ShowCreatureInfoActivity dialog = new ShowCreatureInfoActivity(gameMain, this.Creature, CreatureInfoType.SelectAllItem, OnSelectBackItem);
                activityManager.Push(dialog);
            }
        }

        private void OnSelectBackItem(int index)
        {
            if (index < 0)
            {
                return;
            }

            int itemId = this.TargetCreature.GetItemAt(index);
            if (itemId <= 0)
            {
                return;
            }

            // Exchange the items
            gameMain.CreatureExchangeItem(this.Creature, this.SelectedItemIndex, TargetCreature, index);
            stateHandler.HandleClearStates();
        }
    }
}