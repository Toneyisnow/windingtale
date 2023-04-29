using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

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

        public FDRange ItemRange
        {
            get; private set;
        }

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

            if (this.ItemRange == null)
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(gameMap.Field, this.Creature.Position, 1);
                this.ItemRange = rangeFinder.CalculateRange();
            }

            // Display the attack range on the UI.
            ShowRangePack pack = new ShowRangePack(this.ItemRange);
            SendPack(pack);
        }

        public override void OnExit()
        {
            base.OnExit();
            ClearRangePack pack = new ClearRangePack();
            SendPack(pack);
        }

        public override void OnSelectPosition(FDPosition position)
        {
            // Selecte position must be included in the range
            if (!this.ItemRange.Contains(position))
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