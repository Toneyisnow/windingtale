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
using System.Linq;
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

        // Where the cursor parks on entering, instead of the usual recommendation: the
        // creature 传送术 was about to send, when its tile choice was cancelled.
        private FDCreature preferredTarget = null;

        public SelecteMagicTargetState(GameMain gameMain, FDCreature creature, MagicDefinition magic, FDCreature preferredTarget = null) : base(gameMain)
        {
            this.Creature = creature;
            this.Magic = magic;
            this.preferredTarget = preferredTarget;

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
                magicRange = MagicAreaFinder.CastRange(fdMap.Field, this.Creature.Position, this.Magic);
            }

            //ShowRangeActivity activity = new ShowRangeActivity(magicRange.ToList());
            //activityManager.Push(activity);

            // A spell with a blast wider than one tile shows its outline as the cursor. A
            // cross spell's blast is a line, shown by its targets blinking instead.
            gameMain.gameMap.SetCursorScope(this.Magic.IsCross ? 0 : this.Magic.EffectScope);

            // Send magic range to UI, then park the cursor on the creature this magic is
            // most likely meant for (own tile when the range holds nobody suitable).
            gameMain.PushActivity((gameMain) =>
            {
                gameMain.gameMap.showActionTargetRange(this.Creature, magicRange);

                FDCreature target;
                if (preferredTarget != null)
                {
                    target = preferredTarget;
                }
                else if (this.Magic.IsStartAction || this.Magic.IsTransmit)
                {
                    // 行动术 only takes someone who has already acted, and neither it nor 传送术
                    // takes the caster: the lowest id of those it can take.
                    List<FDCreature> candidates = gameMain.getMagicTargets(this.Creature, this.Magic, magicRange);
                    target = candidates.OrderBy(c => c.Id).FirstOrDefault();
                }
                else
                {
                    target = IsTargetingEnemy
                        ? fdMap.GetPreferredAttackTargetInRange(this.Creature, magicRange)
                        : fdMap.GetPreferredFriendOrNpcTargetInRange(this.Creature, magicRange, true);
                }
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
                FDRange blinkScope = MagicAreaFinder.Scope(fdMap.Field, this.Creature.Position, cursor, this.Magic);
                blinkTargets = gameMain.getMagicTargets(this.Creature, this.Magic, blinkScope);
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
                FDRange magicScope = MagicAreaFinder.Scope(fdMap.Field, this.Creature.Position, position, this.Magic);

                // Whose side the spell lands on depends on its kind: attack and debuff magic
                // on the enemy, recovery and buffs on the caster's own side. Asking for
                // enemies only left every healing spell with "nobody to cast on".
                List<FDCreature> targets = gameMain.getMagicTargets(this.Creature, this.Magic, magicScope);
                if (targets == null || targets.Count == 0)
                {
                    // Cannot spell on that position, do nothing
                    return this;
                }
                else if (this.Magic.IsTransmit)
                {
                    // 传送术 is cast in two steps: this is the creature to send, the tile
                    // to send it to is picked next. Its blast is the one tile.
                    FDCreature target = targets.Find(t => t.Position.AreSame(position)) ?? targets[0];
                    return new SelectTransferToPositionState(gameMain, this.Creature, this.Magic, target);
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

