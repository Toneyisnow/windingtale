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

        // Set once the spell is ordered, so its targets stop flashing before it lands.
        private bool magicOrdered = false;

        // The blink targets last worked out, and the tile they were for. The cursor rests
        // on one tile for many frames; the blast only has to be recomputed when it moves.
        private FDPosition blinkCursor = null;
        private List<FDCreature> blinkTargets = null;

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

            // A spell with a blast wider than one tile shows its outline as the cursor.
            gameMain.gameMap.SetCursorScope(this.Magic.EffectScope);

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
            gameMain.gameMap.SetBlinkTargets(null);
            gameMain.gameMap.SetCursorScope(0);
        }

        // Attack and debuff magic hurts (red); recovery, buffs and transmit help (green).
        public override Color BlinkTint => IsTargetingEnemy ? Color.red : Color.green;

        /// <summary>
        /// Everyone the spell would land on if it were cast at the cursor: the valid targets
        /// (enemy or own side, as the spell demands) inside the blast around that tile. For a
        /// wide spell that is the whole crowd, not only the creature under the cursor.
        /// </summary>
        public override List<FDCreature> GetBlinkTargets()
        {
            if (magicOrdered || magicRange == null)
            {
                return null;
            }

            FDPosition cursor = gameMain.gameMap.GetCursorPosition();
            if (cursor == null || !magicRange.Contains(cursor))
            {
                return null;
            }

            if (blinkCursor == null || !blinkCursor.AreSame(cursor))
            {
                DirectRangeFinder rangeFinder = new DirectRangeFinder(fdMap.Field, cursor, this.Magic.EffectScope);
                blinkTargets = gameMain.getMagicTargets(this.Creature, this.Magic, rangeFinder.CalculateRange());
                blinkCursor = FDPosition.At(cursor.X, cursor.Y);
            }

            return blinkTargets;
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

                // Whose side the spell lands on depends on its kind: attack and debuff magic
                // on the enemy, recovery and buffs on the caster's own side. Asking for
                // enemies only left every healing spell with "nobody to cast on".
                List<FDCreature> targets = gameMain.getMagicTargets(this.Creature, this.Magic, magicScope);
                if (targets == null || targets.Count == 0)
                {
                    // Cannot spell on that position, do nothing
                    return this;
                }
                else
                {
                    magicOrdered = true;
                    gameMain.gameMap.SetBlinkTargets(null);
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

