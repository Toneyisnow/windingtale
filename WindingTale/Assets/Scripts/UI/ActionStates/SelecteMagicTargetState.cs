using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
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

        public SelecteMagicTargetState(GameMain gameMain, IStateResultHandler stateHandler, FDCreature creature, MagicDefinition magic) : base(gameMain, stateHandler)
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
                DirectRangeFinder rangeFinder = new DirectRangeFinder(gameMap.Field, this.Creature.Position, this.Magic.EffectRange);
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

        public override void OnSelectPosition(FDPosition position)
        {
            if (this.magicRange == null)
            {
                // should not happen
                stateHandler.HandlePopState();
            }

            if (this.magicRange.Contains(position))
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(gameMap.Field, position, this.Magic.EffectScope);
                FDRange magicScope = rangeFinder.CalculateRange();

                List<FDCreature> targets = gameMap.GetCreatureInRange(magicScope, CreatureFaction.Enemy);
                if (targets == null || targets.Count == 0)
                {
                    // Cannot spell on that position, do nothing
                }
                else
                {
                    gameMain.CreatureMagic(this.Creature, this.Magic.MagicId, position);
                    stateHandler.HandleClearStates();
                }
            }
            else
            {
                // Cancel the magic
                stateHandler.HandlePopState();
            }
        }
    }
}