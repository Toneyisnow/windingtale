using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using System;
using WindingTale.MapObjects.CreatureIcon;

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

            // Send magic range to UI, then park the cursor on the creature this magic is
            // most likely meant for (own tile when the range holds nobody suitable).
            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.showActionTargetRange(this.Creature, magicRange);

                FDCreature target = IsTargetingEnemy
                    ? fdMap.GetPreferredAttackTargetInRange(this.Creature, magicRange)
                    : fdMap.GetPreferredFriendOrNpcTargetInRange(this.Creature, magicRange, true);
                SlideCursorToTarget(this.Creature, target);
            });
        }

        /// <summary>
        /// Whether this magic is cast at the enemy. Attack magic damages them, and Offensive
        /// magic lands its debuffs (forbidden / poison / freezing) on them as well. The rest
        /// -- Recover, the Defensive buffs, and Transmit -- are cast on one's own side, self
        /// included.
        /// </summary>
        private bool IsTargetingEnemy
        {
            get
            {
                return this.Magic.Type == MagicType.Attack || this.Magic.Type == MagicType.Offensive;
            }
        }

        public override void onExit()
        {
            // Clear move range on UI
            gameMain.gameMap.clearAllIndicators();
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
                    return this;
                }
                else
                {
                    gameMain.creatureMagic(this.Creature, position, this.Magic.MagicId);
                    return new IdleState(gameMain);
                }
            }
            else
            {
                // Outside the magic range: ignored. Leaving the state is the cancel key's
                // job, so a stray click on the map has no effect at all.
                return this;
            }

        }

        public override IActionState onUserCancelled()
        {
            // Roll back to the action menu the magic was picked from, the same way the
            // attack target state does. The menu is keyed off where the creature actually
            // stands: it looks up the treasure underfoot and decides whether the creature
            // has already moved from that position.
            return new MenuActionState(gameMain, this.Creature, this.Creature.Position);
        }
    }
}

