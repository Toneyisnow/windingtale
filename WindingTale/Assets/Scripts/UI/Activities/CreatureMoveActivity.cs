using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class CreatureMoveActivity : ActivityBase
    {
        public static float CreatureMoveSpeed = 0.2f;

        protected FDCreature creature;

        protected FDMovePath movePath;

        protected GameObject creatureUI;


        private int targetPosIndex = 0;


        public CreatureMoveActivity(FDCreature creature, FDMovePath movePath)
        {
            this.creature = creature;
            this.movePath = movePath;

            this.targetPosIndex = 0;
        }

        public override void Start(GameObject gameInterface)
        {
            GameObject mapNode = gameInterface.GetComponent<GameFieldScene>().mapNode;

            string creatureName = string.Format("creature_{0}", StringUtils.Digit3(creature.Id));
            creatureUI = mapNode.transform.Find(creatureName).gameObject;

            CreatureWalk walk = creatureUI.AddComponent<CreatureWalk>();
            walk.Init(movePath);
        }

        public override void Update(GameObject gameInterface)
        {
            CreatureWalk walk = creatureUI.GetComponent<CreatureWalk>();
            if(walk == null || !walk.enabled)
            {
                this.HasFinished = true;

                // Update the object to the final position
                if (movePath.Vertexes.Count > 0)
                {
                    Creature creatureObj = creatureUI.GetComponent<Creature>();
                    creatureObj.creature.Position = movePath.Vertexes[movePath.Vertexes.Count - 1];

                    Debug.Log("CreatureMove: update position to " + creatureObj.creature.Position);

                    /*
                    FDCreature enemy = GameMain.Instance.GameMap.Enemies.Find((e) => e.Id == creatureObj.creature.Id);
                    if (enemy != null)
                    {
                        enemy.Position = creatureObj.creature.Position;
                    }
                    */
                }
            }
        }

    }
}
