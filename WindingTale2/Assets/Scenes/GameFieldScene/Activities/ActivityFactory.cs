using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.Scenes.GameFieldScene.Activities
{
    public class ActivityFactory
    {
        /// <summary>
        /// Walks a creature along a path, finishing when the walk animation does.
        ///
        /// A creature that is not on the map is walked over in silence: a chapter script
        /// spawns and walks the party in one breath, and a friend who fell in an earlier
        /// chapter is never spawned (see ChapterEvents.AddCreatureToMap), so the walk its
        /// script queued has nobody to move.
        /// </summary>
        public static DurationActivity CreatureWalkActivity(int creatureId, FDMovePath movePath)
        {
            Action<GameMain> startAction = gameMain =>
            {
                FDCreature creature = gameMain.gameMap.Map.GetCreatureById(creatureId);
                if (creature == null)
                {
                    return;
                }

                // Save the current position
                creature.PrePosition = creature.Position;
                gameMain.gameMap.MoveCreature(creature, movePath);
            };

            Func<GameMain, bool> checkEnd =
                gameMain =>
                {
                    FDCreature creature = gameMain.gameMap.Map.GetCreatureById(creatureId);
                    if (creature == null)
                    {
                        return true;
                    }

                    Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                    return creatureObj.GetComponent<CreatureWalk>() == null;
                };
            Action<GameMain> endAction = gameMain =>
            {
                FDCreature creature = gameMain.gameMap.Map.GetCreatureById(creatureId);
                if (creature == null)
                {
                    return;
                }

                creature.Position = movePath.Desitination;
            };


            return new DurationActivity(startAction, checkEnd, endAction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="movePath"></param>
        /// <returns></returns>
        public static DurationActivity CreatureDyingActivity(FDCreature creature)
        {
            DateTime startTime = DateTime.Now;
            Action<GameMain> startAction = gameMain =>
            {
                Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                creatureObj.gameObject.AddComponent<CreatureDying>();
            };

            Func<GameMain, bool> checkEnd = gameMain =>
            {
                Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                return creatureObj == null || creatureObj.GetComponent<CreatureDying>() == null;
            };
            Action<GameMain> endAction = gameMain =>
            {
                gameMain.gameMap.RemoveCreature(creature.Id);
                gameMain.gameMap.Map.DeadCreatures.Add(creature);
            };

            return new DurationActivity(startAction, checkEnd, endAction);
        }

        /// <summary>
        /// A creature resting in place recovers HP: the HP is applied up front and the
        /// creature flashes white for the length of the flash, after which it greys out
        /// as an actioned creature. Only ever queued when recoveredHp > 0 -- a creature
        /// already at full HP rests without any animation.
        ///
        /// The activity finishes when CreatureRecovering removes itself, so a batch of
        /// resting creatures (endTurnForAll) flashes strictly one at a time.
        /// </summary>
        public static DurationActivity CreatureRecoverActivity(FDCreature creature, int recoveredHp)
        {
            Action<GameMain> startAction = gameMain =>
            {
                creature.Hp = Math.Min(creature.Hp + recoveredHp, creature.HpMax);

                Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                if (creatureObj != null && creatureObj.GetComponent<CreatureRecovering>() == null)
                {
                    creatureObj.gameObject.AddComponent<CreatureRecovering>();
                }
            };

            Func<GameMain, bool> checkEnd = gameMain =>
            {
                Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                return creatureObj == null || creatureObj.GetComponent<CreatureRecovering>() == null;
            };

            Action<GameMain> endAction = gameMain =>
            {
                // Greying out is deferred to here so the flash reads against the
                // creature's normal colours, not against the grey "already acted" look.
                Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                creatureObj?.SetGreyout(true);
            };

            return new DurationActivity(startAction, checkEnd, endAction);
        }
    }

}