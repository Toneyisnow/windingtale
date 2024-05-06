using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

using WindingTale.Core.Objects;
using WindingTale.Core.Map;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    public abstract class AIDelegate
    {
        protected GameMain gameMain
        {
            get; private set;
        }

        protected FDCreature creature
        {
            get; private set;
        }

        public AIDelegate(GameMain gameAction, FDCreature c)
        {
            this.gameMain = gameAction;
            this.creature = c;
        }

        public abstract void TakeAction();

        public virtual void SetParameter(FDPosition pos)
        {

        }

        public bool NeedRecover()
        {
            if (this.creature.Faction == CreatureFaction.Npc && this.creature.Hp < this.creature.HpMax)
            {
                return true;
            }

            if (this.creature.Hp < this.creature.HpMax / 2)
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
            for (int index = 0; index < this.creature.Items.Count; index++)
            {
                int itemId = this.creature.Items[index];
                if (itemId == 101 || itemId == 102 || itemId == 103 || itemId == 104 || itemId == 122)
                {
                    return index;
                }
            }

            return -1;
        }

        protected FDCreature LookForAggressiveTarget()
        {
            List<FDCreature> candidates = this.gameMain.gameMap.Map.GetOppositeCreatures(this.creature);

            int candidateIndex = 0;
            while (candidateIndex < candidates.Count && !this.creature.IsAbleToAttack(candidates[candidateIndex]))
            {
                candidateIndex++;
            }

            if (candidateIndex >= candidates.Count)
            {
                candidateIndex = 0;
            }

            FDCreature terminateCreature = candidates[candidateIndex];

            DistanceResolver disResolver = new DistanceResolver(gameMain.gameMap.Map.Field);
            disResolver.ResolveDistanceFrom(this.creature.Position, terminateCreature.Position);

            float minDistance = 999;
            FDCreature finalTarget = terminateCreature;
            foreach (FDCreature c in candidates)
            {
                float distance = disResolver.GetDistanceTo(c.Position);
                if (distance < minDistance && this.creature.IsAbleToAttack(c))
                {
                    minDistance = distance;
                    finalTarget = c;
                }
            }

            return finalTarget;
        }

        protected FDMovePath DecidePositionAndPath(FDPosition targetPos)
        {
            DistanceResolver disResolver = new DistanceResolver(gameMain.gameMap.Map.Field);
            disResolver.ResolveDistanceFrom(targetPos, this.creature.Position);

            FDPosition originalPos = this.creature.Position;

            float bestDistance = 999;
            int bestDistanceInUnit = -1;
            bool inAttackScope = false;

            MoveRangeFinder finder = new MoveRangeFinder(gameMain.gameMap.Map, this.creature);
            FDMoveRange moveRange = finder.CalculateMoveRange();

            FDSpan span = null;
            FDPosition finalPos = originalPos;
            if(gameMain.gameMap.Map.GetCreatureAt(targetPos) != null)
            {
                span = creature.GetAttackItem()?.AttackScope;
            }
            else
            {
                span = new FDSpan(0, 0);
            }

            foreach (FDPosition movePos in moveRange.ToList())
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

            MovePathFinder pathFinder = new MovePathFinder(moveRange);
            return pathFinder.GetPath(finalPos);
        }

        protected int GetDirectDistance(FDPosition pos1, FDPosition pos2)
        {
            return Mathf.Abs(pos1.X - pos2.X) + Mathf.Abs(pos1.Y - pos2.Y);
        }
    }
}
