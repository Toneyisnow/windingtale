using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;

using WindingTale.Core.Objects;
using WindingTale.Core.Map;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

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

        protected FDMap gameField
        {
            get { return this.gameMain.gameMap.Map; }
        }

        public AIDelegate(GameMain gameAction, FDCreature c)
        {
            this.gameMain = gameAction;
            this.creature = c;
        }

        /// <summary>
        /// Decides what this creature does with its turn, expressed as activities pushed on
        /// the game queue. Every path through it must end the creature's turn (EndTurn, an
        /// attack, an item, a magic) or hand it on with PendAction -- the turn cannot
        /// advance while a creature is still flagged as not having acted.
        /// </summary>
        public abstract void TakeAction();

        public virtual bool NeedRecover()
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

        #region Turn Actions

        /// <summary>
        /// Ends the creature's turn where it stands.
        /// </summary>
        protected void EndTurn()
        {
            gameMain.creatureRest(this.creature);
        }

        /// <summary>
        /// Drinks a healing item, which ends the turn.
        /// </summary>
        protected void SelfRecover()
        {
            int itemIndex = this.getRecoverItem();
            if (itemIndex < 0)
            {
                EndTurn();
                return;
            }

            gameMain.PushActivity((game) => game.creatureUseItem(this.creature, itemIndex, this.creature));
        }

        /// <summary>
        /// Uses a healing item on someone else, which ends the turn.
        /// </summary>
        protected void RecoverTarget(FDCreature target)
        {
            int itemIndex = this.getRecoverItem();
            if (itemIndex < 0 || target == null)
            {
                EndTurn();
                return;
            }

            gameMain.PushActivity((game) => game.creatureUseItem(this.creature, itemIndex, target));
        }

        /// <summary>
        /// Walks as far towards targetPos as this turn's move points allow, and returns the
        /// tile the creature ends up on -- which is where any follow-up attack has to be
        /// measured from, since the walk itself only happens later, when the queued
        /// activity runs.
        /// </summary>
        protected FDPosition MoveToward(FDPosition targetPos)
        {
            FDMovePath movePath = this.DecidePositionAndPath(targetPos);
            FDPosition destination = movePath?.Desitination ?? this.creature.Position;

            Debug.Log(string.Format("AI: creature={0} position={1} heading={2} destination={3}",
                creature.Id, creature.Position, targetPos, destination));

            // Follow the walk with the cursor and camera, the way the player's own move does.
            gameMain.PushActivity(new SlideCursorActivity(destination));

            if (movePath != null)
            {
                gameMain.creatureMoveAsync(this.creature, movePath);
            }

            return destination;
        }

        /// <summary>
        /// Attacks the target if it is still worth attacking and stands within reach of
        /// fromPosition; otherwise the creature simply ends its turn.
        /// </summary>
        protected void AttackTargetOrEndTurn(FDCreature target, FDPosition fromPosition)
        {
            if (this.IsInAttackScope(target, fromPosition))
            {
                gameMain.PushActivity((game) => game.creatureAttackAsync(this.creature, target));
                return;
            }

            EndTurn();
        }

        /// <summary>
        /// Hands the rest of the turn back to the AI handler: this creature is skipped for
        /// now and picked up again after its team mates have acted.
        /// </summary>
        protected void PendAction()
        {
            gameMain.creaturePendAction(this.creature);
        }

        #endregion

        #region Target Search

        /// <summary>
        /// The creatures on the other side -- the ones this creature attacks.
        /// </summary>
        protected List<FDCreature> GetOppositeCreatures()
        {
            return this.gameField.GetOppositeCreatures(this.creature);
        }

        /// <summary>
        /// The creatures on this creature's own side, itself included.
        /// </summary>
        protected List<FDCreature> GetSameSideCreatures()
        {
            if (this.creature.Faction == CreatureFaction.Enemy)
            {
                return this.gameField.Enemies;
            }

            return this.gameField.Creatures.FindAll(
                c => c.Faction == CreatureFaction.Friend || c.Faction == CreatureFaction.Npc);
        }

        /// <summary>
        /// The opposite creature to go after: the nearest one this creature can actually
        /// hurt, measured by walking distance rather than in a straight line. When there is
        /// nobody it can hurt it still heads for one of them -- an unarmed NPC walks towards
        /// the fight rather than standing still all battle.
        ///
        /// Null only when the other side has been wiped out.
        /// </summary>
        protected FDCreature LookForAggressiveTarget()
        {
            List<FDCreature> candidates = this.GetOppositeCreatures()
                .FindAll(c => !(c is FDAICreature ai) || ai.IsNoticable());

            if (candidates.Count == 0)
            {
                return null;
            }

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

            DistanceResolver disResolver = new DistanceResolver(gameField.Field);
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

        /// <summary>
        /// The wounded team mate to go and heal: the nearest one, itself excluded. Null when
        /// nobody else is hurt.
        /// </summary>
        protected FDCreature LookForDefensiveTarget()
        {
            List<FDCreature> candidates = this.GetSameSideCreatures()
                .FindAll(c => c.Id != this.creature.Id && c.Hp < c.HpMax);

            if (candidates.Count == 0)
            {
                return null;
            }

            DistanceResolver disResolver = new DistanceResolver(gameField.Field);
            disResolver.ResolveDistanceFrom(this.creature.Position, candidates[0].Position);

            float minDistance = 999;
            FDCreature finalTarget = candidates[0];
            foreach (FDCreature c in candidates)
            {
                float distance = disResolver.GetDistanceTo(c.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    finalTarget = c;
                }
            }

            return finalTarget;
        }

        /// <summary>
        /// Whether the target can be attacked from fromPosition right now: it is still a
        /// creature worth attacking, and it stands within the reach of the equipped weapon.
        /// </summary>
        protected bool IsInAttackScope(FDCreature target, FDPosition fromPosition)
        {
            if (target == null || !this.creature.IsAbleToAttack(target))
            {
                return false;
            }

            FDSpan span = this.creature.GetAttackItem()?.AttackScope;

            return span != null && span.ContainsValue(GetDirectDistance(fromPosition, target.Position));
        }

        #endregion

        /// <summary>
        /// Picks the tile to walk to in order to deal with whatever is at targetPos, and the
        /// path there.
        ///
        /// When a creature is standing on targetPos the destination is the tile within the
        /// move range that puts the target in weapon reach, the farthest such tile preferred
        /// -- an archer keeps its distance. Failing that (and always, for an empty tile such
        /// as an escape point), it is the reachable tile closest to targetPos by walking
        /// distance, so the creature covers as much ground as it can this turn.
        /// </summary>
        protected FDMovePath DecidePositionAndPath(FDPosition targetPos)
        {
            DistanceResolver disResolver = new DistanceResolver(gameField.Field);
            disResolver.ResolveDistanceFrom(targetPos, this.creature.Position);

            FDPosition originalPos = this.creature.Position;

            float bestDistance = 999;
            int bestDistanceInUnit = -1;
            bool inAttackScope = false;

            MoveRangeFinder finder = new MoveRangeFinder(gameField, this.creature);
            FDMoveRange moveRange = finder.CalculateMoveRange();

            FDSpan span = null;
            FDPosition finalPos = originalPos;
            if (gameField.GetCreatureAt(targetPos) != null)
            {
                // Null when the creature carries no weapon: it has no attack scope to place
                // itself in, so it just walks as close as it can get.
                span = creature.GetAttackItem()?.AttackScope;
            }
            else
            {
                span = new FDSpan(0, 0);
            }

            foreach (FDPosition movePos in moveRange.ToList())
            {
                int distanceToTarget = GetDirectDistance(targetPos, movePos);
                if (span != null && span.ContainsValue(distanceToTarget))
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
