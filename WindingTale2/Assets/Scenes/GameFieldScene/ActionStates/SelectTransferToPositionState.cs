using System;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    /// <summary>
    /// The second step of 传送术 (501): the creature to send has been picked
    /// (SelecteMagicTargetState), now the tile to send it to. No range is shown -- any tile
    /// on the map will do, as long as the creature could stand on it: free of creatures,
    /// not a gap, and for a creature that cannot fly not blocked either. Confirming a tile
    /// it cannot stand on does nothing; cancelling goes back to picking the creature.
    /// </summary>
    public class SelectTransferToPositionState : IActionState
    {
        public FDCreature Creature
        {
            get; private set;
        }

        public MagicDefinition Magic
        {
            get; private set;
        }

        public FDCreature Target
        {
            get; private set;
        }

        // Set once the spell is ordered, so the target stops flashing before it goes.
        private bool magicOrdered = false;

        public SelectTransferToPositionState(GameMain gameMain, FDCreature creature, MagicDefinition magic, FDCreature target) : base(gameMain)
        {
            this.Creature = creature ?? throw new ArgumentNullException("creature");
            this.Magic = magic ?? throw new ArgumentNullException("magic");
            this.Target = target ?? throw new ArgumentNullException("target");
        }

        public override void onEnter()
        {
        }

        public override void onExit()
        {
            gameMain.gameMap.SetBlinkTargets(null);
        }

        public override Color BlinkTint => Color.green;

        /// <summary>The creature being sent keeps flashing while its new tile is chosen.</summary>
        public override List<FDCreature> GetBlinkTargets()
        {
            return magicOrdered ? null : new List<FDCreature> { this.Target };
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            if (!CanStandOn(position))
            {
                return this;
            }

            magicOrdered = true;
            gameMain.gameMap.SetBlinkTargets(null);
            gameMain.creatureTransfer(this.Creature, this.Target, position, this.Magic.MagicId);
            return new IdleState(gameMain);
        }

        public override IActionState onUserCancelled()
        {
            return new SelecteMagicTargetState(gameMain, this.Creature, this.Magic, this.Target);
        }

        private bool CanStandOn(FDPosition position)
        {
            if (position == null || fdMap.GetCreatureAt(position) != null)
            {
                return false;
            }

            ShapeDefinition shape = fdMap.Field.GetShapeAt(position);
            if (shape == null || shape.Type == ShapeType.Gap)
            {
                return false;
            }

            bool canFly = this.Target.Definition != null && this.Target.Definition.CanFly();
            return canFly || shape.Type != ShapeType.Blocked;
        }
    }
}
