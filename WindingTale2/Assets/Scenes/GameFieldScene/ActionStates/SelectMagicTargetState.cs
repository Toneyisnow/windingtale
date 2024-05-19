using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using UnityEditor.SceneManagement;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using System;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    /// <summary>
    /// 
    /// </summary>
    public class SelecteMagicTargetState : IActionState
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

        public SelecteMagicTargetState(GameMain gameMain, FDCreature creature, MagicDefinition magic) : base(gameMain)
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
        public override void onEnter()
        {
            if (magicRange == null)
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(fdMap.Field, this.Creature.Position, this.Magic.EffectRange);
                magicRange = rangeFinder.CalculateRange();
            }

            //ShowRangeActivity activity = new ShowRangeActivity(magicRange.ToList());
            //activityManager.Push(activity);
        }

        public override void onExit()
        {

            //ClearRangeActivity activity = new ClearRangeActivity();
            //activityManager.Push(activity);
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            if (this.magicRange == null)
            {
                // should not happen
                // stateHandler.HandlePopState();
            }

            if (this.magicRange.Contains(position))
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(fdMap.Field, position, this.Magic.EffectScope);
                FDRange magicScope = rangeFinder.CalculateRange();

                List<FDCreature> targets = fdMap.GetCreaturesInRange(magicScope.ToList(), CreatureFaction.Enemy);
                if (targets == null || targets.Count == 0)
                {
                    // Cannot spell on that position, do nothing
                }
                else
                {
                    gameMain.creatureMagic(this.Creature, position, this.Magic.MagicId);
                    //// stateHandler.HandleClearStates();
                }
            }
            else
            {
                // Cancel the magic
                //// stateHandler.HandlePopState();
            }

            return this;
        }

        public override IActionState onUserCancelled()
        {
            throw new NotImplementedException();
        }
    }
}

