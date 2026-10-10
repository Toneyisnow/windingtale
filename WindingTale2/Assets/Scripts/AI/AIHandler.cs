using System;
using System.Collections.Generic;
using WindingTale.Core.Definitions;
using WindingTale.AI.Delegates;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;
using UnityEngine;

namespace WindingTale.AI
{

    public class AIHandler
    {

        private int lastOperatedCreatureId = 0;

        public CreatureFaction Faction
        {
            get; private set;
        }

        private GameMain gameMain = null;

        private List<FDCreature> creatures = null;

        public AIHandler(GameMain gameMain, CreatureFaction faction)
        {
            this.gameMain = gameMain;
            this.Faction = faction;


        }

        /// <summary>
        /// Notify the AI handler to run the AI delegate.
        /// 
        /// </summary>
        /// <returns>Returning false means there is nothing to handle </returns>
        public bool Notified()
        {
            List<FDCreature> creatures = null;
            if (this.Faction == CreatureFaction.Enemy)
            {
                creatures = gameMain.gameMap.Map.Enemies;
            }
            else if (this.Faction == CreatureFaction.Npc)
            {
                creatures = gameMain.gameMap.Map.Npcs;
            }

            // Creatures act in id order. A caster that deferred its turn (PendingAction) is
            // held back until everyone else has gone -- it wants to see where its team mates
            // end up before it commits to walking into the fight.
            FDCreature selectedCreature = SelectNextCreature(creatures, false);
            if (selectedCreature == null)
            {
                selectedCreature = SelectNextCreature(creatures, true);
            }

            if (selectedCreature == null)
            {
                // No creature can take action, end the turn
                return false;
            }


            if (selectedCreature is FDAICreature aiCreature)
            {
                RunAIDelegate(aiCreature);
            }
            else
            {
                // Not an AI creature at all, but it is on an AI team and the turn cannot end
                // until it has acted: pass its turn rather than stall the game.
                Debug.LogWarning("AIHandler: creature " + selectedCreature.Id + " is not an FDAICreature.");
                gameMain.creatureRest(selectedCreature);
            }

            return true;
        }

        /// <summary>
        /// The lowest-id creature still to act, among those that have deferred their turn or
        /// among those that have not, according to pending.
        /// </summary>
        /// <summary>Whether a living member of the party stands within distance tiles (|dx| + |dy|).</summary>
        private bool IsFriendWithin(FDCreature creature, int distance)
        {
            foreach (FDCreature friend in gameMain.gameMap.Map.Friends)
            {
                if (friend.IsDead() || friend.Position == null || creature.Position == null)
                {
                    continue;
                }

                int dx = Math.Abs(friend.Position.X - creature.Position.X);
                int dy = Math.Abs(friend.Position.Y - creature.Position.Y);
                if (dx + dy <= distance)
                {
                    return true;
                }
            }

            return false;
        }

        private static FDCreature SelectNextCreature(List<FDCreature> creatures, bool pending)
        {
            FDCreature selectedCreature = null;

            foreach (FDCreature creature in creatures)
            {
                if (!creature.CanTakeAction())
                {
                    continue;
                }

                bool isPending = (creature as FDAICreature)?.PendingAction ?? false;
                if (isPending != pending)
                {
                    continue;
                }

                if (selectedCreature == null || creature.Id < selectedCreature.Id)
                {
                    selectedCreature = creature;
                }
            }

            return selectedCreature;
        }

        /// <summary>
        /// How close (tiles, walking distance) one of the party may come to an enemy lying
        /// in wait before it notices and wakes, instead of waiting for its chapter's turn.
        /// </summary>
        private const int StandByWakeDistance = 2;

        private void RunAIDelegate(FDAICreature creature)
        {
            AIDelegate aiDelegate = null;

            if (creature.AIType == AITypes.AIType_StandBy && creature.Faction == CreatureFaction.Enemy
                && IsFriendWithin(creature, StandByWakeDistance))
            {
                creature.AIType = AITypes.AIType_Aggressive;
            }

            // A creature that only passes its turn is not worth panning the camera to --
            // and an UnNoticable one is a marker the player is not meant to be shown at all.
            bool isIdle = creature.AIType == AITypes.AIType_StandBy
                || creature.AIType == AITypes.AIType_UnNoticable;

            switch (creature.AIType)
            {
                case AITypes.AIType_Aggressive:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalAggressiveDelegate(gameMain, creature);
                    }
                    else
                    {
                        aiDelegate = new AIAggressiveDelegate(gameMain, creature);
                    }
                    break;
                case AITypes.AIType_Defensive:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalDefensiveDelegate(gameMain, creature);
                    }
                    else
                    {
                        aiDelegate = new AIDefensiveDelegate(gameMain, creature);
                    }
                    break;
                case AITypes.AIType_Guard:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalGuardDelegate(gameMain, creature);
                    }
                    else
                    {
                        aiDelegate = new AIGuardDelegate(gameMain, creature);
                    }
                    break;
                case AITypes.AIType_Escape:
                    aiDelegate = new AIEscapeDelegate(gameMain, creature);
                    break;
                case AITypes.AIType_Treasure:
                    aiDelegate = new AITreasureDelegate(gameMain, creature);
                    break;
                case AITypes.AIType_StandBy:
                case AITypes.AIType_UnNoticable:
                default:
                    // Lying in wait, or not really a creature at all: it passes its turn.
                    // Note this must still end the turn -- a creature left flagged as not
                    // having acted would stall the turn for good.
                    aiDelegate = new AIStandByDelegate(gameMain, creature);
                    break;
            }

            lastOperatedCreatureId = creature.Id;

            // A caster that is only going to defer its turn to the end of the round is not
            // visited now either: the cursor goes straight on to the next creature to act,
            // and comes to this one when it returns to act.
            bool defers = aiDelegate is AIMagicalDelegate magical && magical.WillDeferTurn();

            if (!isIdle && !defers)
            {
                // Before the AI operates on this creature, slide the cursor (and the
                // follow camera) to the tile under the creature, matching the same
                // framing used during conversations.
                gameMain.PushActivity(new SlideCursorActivity(creature.Position));
            }

            aiDelegate.TakeAction();
        }

    }
}
