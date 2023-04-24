using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    /// <summary>
    /// 
    /// </summary>
    public class SelecteMagicTargetState : ActionState
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public MagicDefinition Magic
        {
            get; private set;
        }

        private FDRange magicRange = null;

        public SelecteMagicTargetState(IGameAction action, FDCreature creature, MagicDefinition magic) : base(action)
        {
            this.Creature = creature;
            this.Magic = magic;

            if (this.Creature == null)
            {
                throw new ArgumentNullException("creature");
            }

            if (this.Magic == null)
            {
                throw new ArgumentNullException("magic");
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (magicRange == null)
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(gameAction.GetField(), this.Creature.Position, this.Magic.EffectRange);
                magicRange = rangeFinder.CalculateRange();
            }

            ShowRangePack rangePack = new ShowRangePack(magicRange);
            SendPack(rangePack);
        }

        public override void OnExit()
        {
            base.OnExit();

            ClearRangePack clear = new ClearRangePack();
            SendPack(clear);
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {
            if (this.magicRange == null)
            {
                // should not happen
                return StateOperationResult.Clear();
            }

            if (this.magicRange.Contains(position))
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(gameAction.GetField(), position, this.Magic.EffectScope);
                FDRange magicScope = rangeFinder.CalculateRange();

                List<FDCreature> targets = gameAction.GetCreatureInRange(magicScope, CreatureFaction.Enemy);
                if (targets == null || targets.Count == 0)
                {
                    // Cannot spell on that position, do nothing
                    return StateOperationResult.None();
                }
                else
                {
                    gameAction.DoCreatureSpellMagic(this.Creature.CreatureId, this.Magic.MagicId, position);
                    return StateOperationResult.Clear();
                }
            }
            else
            {
                // Cancel the magic
                return StateOperationResult.Pop();
            }
        }
    }
}