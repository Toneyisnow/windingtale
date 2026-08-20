using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// The medic: it carries healing items and spends the battle keeping its side standing.
    /// It walks to the nearest wounded team mate and heals whoever next to it is worst off;
    /// with nobody else hurt it drinks its own potion, and with nothing to heal at all it
    /// just tags along with its team.
    /// </summary>
    public class AIDefensiveDelegate : AIDelegate
    {
        public AIDefensiveDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        /// <summary>
        /// A medic tops itself up at the first scratch rather than waiting to be half dead.
        /// </summary>
        public override bool NeedRecover()
        {
            return this.creature.Hp < this.creature.HpMax;
        }

        public override void TakeAction()
        {
            if (!this.CanRecover())
            {
                // Nothing to heal with: stay with the team.
                this.MakeMove();
                return;
            }

            FDCreature target = this.LookForDefensiveTarget();
            if (target != null)
            {
                // Walk up to the wounded team mate, then heal whoever ends up next to us.
                this.MoveToward(target.Position);
                gameMain.PushActivity((game) => this.RescueNearByTarget());
                return;
            }

            if (this.NeedRecover())
            {
                this.SelfRecover();
                return;
            }

            this.MakeMove();
        }

        /// <summary>
        /// Heals the worst wounded team mate standing next to the creature. Run after the
        /// walk, so it sees where the creature actually ended up.
        /// </summary>
        private void RescueNearByTarget()
        {
            // Its own side, not the player's -- FDMap.GetAdjacentFriends always means the
            // party, which is the wrong team for an enemy medic.
            FDCreature target = null;
            foreach (FDCreature c in this.GetSameSideCreatures())
            {
                if (c.Id == this.creature.Id || !c.Position.IsNextTo(this.creature.Position))
                {
                    continue;
                }

                if (c.Hp < c.HpMax && (target == null || c.Hp < target.Hp))
                {
                    target = c;
                }
            }

            if (target == null)
            {
                // The walk did not get us there this turn.
                this.EndTurn();
                return;
            }

            this.RecoverTarget(target);
        }

        /// <summary>
        /// Nothing to do but keep up with the team: head for one of them, picked at random so
        /// a group of medics does not pile onto the same creature.
        /// </summary>
        private void MakeMove()
        {
            List<FDCreature> candidates = this.GetSameSideCreatures()
                .FindAll(c => c.Id != this.creature.Id);

            if (candidates.Count == 0)
            {
                this.EndTurn();
                return;
            }

            FDCreature target = candidates[Random.Range(0, candidates.Count)];

            this.MoveToward(target.Position);
            this.EndTurn();
        }

    }
}
