using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
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

        public int CreatureId
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameAction"></param>
        public SelectItemExchangeTargetState(IGameAction gameAction, int creatureId, int itemIndex) : base(gameAction)
        {
            this.CreatureId = creatureId;
            this.Creature = gameAction.GetCreature(creatureId);
            this.SelectedItemIndex = itemIndex;

        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            // Selecte position must be next to current creature
            if (!this.Creature.Position.IsNextTo(position))
            {
                return StateOperationResult.None();
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = this.gameAction.GetCreatureAt(position);
            if (targetCreature == null || targetCreature.Faction == Definitions.CreatureFaction.Enemy)
            {
                return StateOperationResult.None();
            }

            if(!targetCreature.Data.IsItemsFull())
            {
                gameAction.DoCreatureExchangeItem(this.CreatureId, this.SelectedItemIndex, targetCreature.CreatureId);
                return StateOperationResult.Clear();
            }
            else
            {
                subState = SubState.SelectExchangeItem;
                this.TargetCreature = targetCreature;
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(targetCreature, CreatureInfoType.SelectAllItem);
                SendPack(pack);

                return StateOperationResult.None();
            }
        }

        public override StateOperationResult OnSelectIndex(int index)
        {
            if (subState == SubState.SelectExchangeItem)
            {
                if (index < 0)
                {
                    return StateOperationResult.None();
                }

                int itemId = this.TargetCreature.Data.GetItemAt(index);
                if (itemId <= 0)
                {
                    return StateOperationResult.None();
                }

                // Exchange the items
                gameAction.DoCreatureExchangeItem(this.CreatureId, this.SelectedItemIndex, TargetCreature.CreatureId, index);
                return StateOperationResult.Clear();
            }

            return StateOperationResult.None();
        }
    }
}