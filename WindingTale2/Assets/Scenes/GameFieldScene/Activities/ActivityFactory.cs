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
        public static DurationActivity CreatureWalkActivity(FDCreature creature, FDMovePath movePath)
        {
            Action<GameMain> startAction = gameMain =>
            {
                // Save the current position
                creature.PrePosition = creature.Position;
                gameMain.gameMap.MoveCreature(creature, movePath);
            };

            Func<GameMain, bool> checkEnd = 
                gameMain =>
                {
                    Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                    return creatureObj.GetComponent<CreatureWalk>() == null;
                };
            Action<GameMain> endAction = gameMain =>
            {
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
                startTime = DateTime.Now;
            };

            Func<GameMain, bool> checkEnd = gameMain =>
            {
                DateTime nowTime = DateTime.Now;
                if (nowTime > startTime.AddSeconds(2))
                {
                    return true;
                }
                return false;
            };

            return new DurationActivity(startAction, checkEnd);
        }

        /// <summary>
        /// Show the animation of a creature resting and recovering
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="movePath"></param>
        /// <returns></returns>
        public static DurationActivity CreatureRestRecoverActivity(FDCreature creature)
        {
            DateTime startTime = DateTime.Now;
            Action<GameMain> startAction = gameMain =>
            {
                startTime = DateTime.Now;
            };

            Func<GameMain, bool> checkEnd = gameMain =>
            {
                DateTime nowTime = DateTime.Now;
                if (nowTime > startTime.AddSeconds(2))
                {
                    return true;
                }
                return false;
            };

            return new DurationActivity(startAction, checkEnd);
        }
    }

}