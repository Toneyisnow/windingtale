using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
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
        public SelectItemUseTargetState(IGameAction gameAction, int creatureId, int itemIndex) : base(gameAction)
        {
            this.CreatureId = creatureId;
            this.Creature = gameAction.GetCreature(creatureId);
            this.SelectedItemIndex = itemIndex;

        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (this.ItemRange == null)
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(this.gameAction.GetField(), this.Creature.Position, 1);
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

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            // Selecte position must be included in the range
            if (!this.ItemRange.Contains(position))
            {
                return StateOperationResult.Pop();
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = this.gameAction.GetCreatureAt(position);
            if(targetCreature == null || targetCreature.Faction == Definitions.CreatureFaction.Enemy)
            {
                return StateOperationResult.None();
            }

            gameAction.DoCreatureUseItem(this.CreatureId, this.SelectedItemIndex, targetCreature.CreatureId);
            return StateOperationResult.Clear();
        }
    }
}