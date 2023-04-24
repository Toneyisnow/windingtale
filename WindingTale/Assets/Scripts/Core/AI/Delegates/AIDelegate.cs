using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;
using WindingTale.Core.Definitions;
using WindingTale.Core.Components.Algorithms;

namespace WindingTale.AI.Delegates
{
    public abstract class AIDelegate
    {
        protected IGameAction GameAction
        {
            get; private set;
        }

        protected FDCreature Creature
        {
            get; private set;
        }

        public AIDelegate(IGameAction gameAction, FDCreature c)
        {
            this.GameAction = gameAction;
            this.Creature = c;
        }

        public abstract void TakeAction();

        public virtual void SetParameter(FDPosition pos)
        {

        }

        public bool NeedRecover()
        {
            if (this.Creature.Faction == CreatureFaction.Npc && this.Creature.Data.Hp < this.Creature.Data.HpMax)
            {
                return true;
            }

            if (this.Creature.Data.Hp < this.Creature.Data.HpMax / 2)
            {
                return true;
            }

            return false;
        }

        public bool CanRecover()
        {
            return this.getRecoverItem() >= 0;
        }

        public bool NeedAndCanRecover()
        {
            return this.NeedRecover() && this.CanRecover();
        }

        public int getRecoverItem()
        {
            for (int index = 0; index < this.Creature.Data.Items.Count; index++)
            {
                int itemId = this.Creature.Data.Items[index];
                if (itemId == 101 || itemId == 102 || itemId == 103 || itemId == 104 || itemId == 122)
                {
                    return index;
                }
            }

            return -1;
        }

        protected FDCreature LookForAggressiveTarget()
        {
            List<FDCreature> candidates = this.GameAction.GetOppositeCreatures(this.Creature);

            int candidateIndex = 0;
            while (candidateIndex < candidates.Count && !this.Creature.IsAbleToAttack(candidates[candidateIndex]))
            {
                candidateIndex++;
            }

            if (candidateIndex >= candidates.Count)
            {
                candidateIndex = 0;
            }

            FDCreature terminateCreature = candidates[candidateIndex];

            DistanceResolver disResolver = new DistanceResolver(this.GameAction.GetField());
            disResolver.ResolveDistanceFrom(this.Creature.Position, terminateCreature.Position);

            float minDistance = 999;
            FDCreature finalTarget = terminateCreature;
            foreach (FDCreature c in candidates)
            {
                float distance = disResolver.GetDistanceTo(c.Position);
                if (distance < minDistance && this.Creature.IsAbleToAttack(c))
                {
                    minDistance = distance;
                    finalTarget = c;
                }
            }

            return finalTarget;
        }

        protected FDMovePath DecidePositionAndPath(FDPosition targetPos)
        {
            DistanceResolver disResolver = new DistanceResolver(this.GameAction.GetField());
            disResolver.ResolveDistanceFrom(targetPos, this.Creature.Position);

            FDPosition originalPos = this.Creature.Position;

            float bestDistance = 999;
            int bestDistanceInUnit = -1;
            bool inAttackScope = false;

            MoveRangeFinder finder = new MoveRangeFinder(this.GameAction, this.Creature);
            FDMoveRange moveRange = finder.CalculateMoveRange();

            FDSpan span = null;
            FDPosition finalPos = originalPos;
            if(this.GameAction.GetCreatureAt(targetPos) != null)
            {
                span = Creature.Data.GetAttackItem()?.AttackScope;
            }
            else
            {
                span = new FDSpan(0, 0);
            }

            foreach (FDPosition movePos in moveRange.Positions)
            {
                int distanceToTarget = GetDirectDistance(targetPos, movePos);
                if (span.ContainsValue(distanceToTarget))
                {
                    inAttackScope = true;
                    if (distanceToTarget > bestDistanceInUnit)
                    {
                        bestDistanceInUnit = distanceToTarget;
                        finalPos = movePos;
                    }
                }   

                if (!inAttackScope)
                {
                    float distance = disResolver.GetDistanceTo(movePos);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        finalPos = movePos;
                    }
                }
            }

            return moveRange.GetPath(finalPos);
        }

        protected int GetDirectDistance(FDPosition pos1, FDPosition pos2)
        {
            return Mathf.Abs(pos1.X - pos2.X) + Mathf.Abs(pos1.Y - pos2.Y);
        }
    }
}
