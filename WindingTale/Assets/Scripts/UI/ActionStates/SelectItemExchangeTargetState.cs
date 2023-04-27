using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
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

        private FDRange range = null;

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

            if (range == null)
            {
                DirectRangeFinder finder = new DirectRangeFinder(gameAction.GetField(), this.Creature.Position, 1, 1);
                range = finder.CalculateRange();
            }

            ShowRangePack rangePack = new ShowRangePack(range);
            SendPack(rangePack);
        }

        public override void OnExit()
        {
            base.OnExit();

            ClearRangePack clear = new ClearRangePack();
            SendPack(clear);
        }

        public override StateResult OnSelectPosition(FDPosition position)
        {
            if (range == null || !range.Contains(position))
            {
                return StateResult.Pop();
            }

            // No creature or not a friend/NPC
            FDCreature targetCreature = this.gameAction.GetCreatureAt(position);
            if (targetCreature == null || targetCreature.Faction == Definitions.CreatureFaction.Enemy)
            {
                return StateResult.None();
            }

            if(!targetCreature.Data.IsItemsFull())
            {
                gameAction.DoCreatureExchangeItem(this.CreatureId, this.SelectedItemIndex, targetCreature.CreatureId);
                return StateResult.Clear();
            }
            else
            {
                subState = SubState.SelectExchangeItem;
                this.TargetCreature = targetCreature;
                CreatureShowInfoPack pack = new CreatureShowInfoPack(targetCreature, CreatureInfoType.SelectAllItem);
                SendPack(pack);

                return StateResult.None();
            }
        }

        public override StateResult OnSelectIndex(int index)
        {
            if (subState == SubState.SelectExchangeItem)
            {
                if (index < 0)
                {
                    return StateResult.None();
                }

                int itemId = this.TargetCreature.Data.GetItemAt(index);
                if (itemId <= 0)
                {
                    return StateResult.None();
                }

                // Exchange the items
                gameAction.DoCreatureExchangeItem(this.CreatureId, this.SelectedItemIndex, TargetCreature.CreatureId, index);
                return StateResult.Clear();
            }

            return StateResult.None();
        }
    }
}