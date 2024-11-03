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
                gameMain.creatureMove(creature, movePath);
            };

            Func<GameMain, bool> checkEnd = 
                gameMain =>
                {
                    Creature creatureObj = gameMain.gameMap.GetCreature(creature);
                    return creatureObj.GetComponent<CreatureWalk>() == null;
                };

            return new DurationActivity(startAction, checkEnd);
        }
    }

}