using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WindingTale.AI;
using WindingTale.Chapters;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene.Activities;
using WindingTale.UI.Dialogs;

namespace WindingTale.Scenes.GameFieldScene
{

    public class GameMain : MonoBehaviour, IGameActions
    {
        #region Properties

        public GameObject mapObject;

        public GameObject canvasObject;


        public static GameMain getDefault()
        {
            GameMain game = GameObject.Find("GameRoot").GetComponent<GameMain>();
            if (game == null)
            {
                throw new MissingComponentException("Cannot find component GameMain");
            }

            return game;
        }

        public GameMap gameMap
        {
            get { return mapObject.GetComponent<GameMap>(); }
        }

        public GameCanvas gameCanvas
        {
            get { return canvasObject.GetComponent<GameCanvas>(); }
        }

        public ActivityQueue activityQueue
        {
            get
            {
                return gameObject.GetComponent<ActivityQueue>();
            }
        }

        
        private EventHandler eventHandler = null;

        private AIHandler enemyAIHandler = null;

        private AIHandler npcAIHandler = null;



        #endregion

        private int chapterId = 0;

        // Start is called before the first frame update
        void Start()
        {
            chapterId = 1;
            onInitialize();
        }

        #region Game Cycles

        public void startNewGame()
        {
            // Map load from file
        }

        public void loadGame()
        {

        }

        public void continueGame()
        {

        }

        public void saveGame()
        {

        }


        public void onQuit()
        {

        }

        public void onGameOver()
        {

        }

        public void onGameWin()
        {

        }

        #endregion

        #region Creature Related Operation

        public void creatureMove(FDCreature creature, FDMovePath path)
        {
            gameMap.MoveCreature(creature, path.Desitination);
        }

        public void creatureAttack(FDCreature creature, FDCreature target)
        {
            Debug.Log("creatureAttack!!!");

            Creature c = gameMap.GetCreature(creature);
            c.SetActioned(true);

            onCreatureEndTurn(creature);
        }

        public void creatureMagic(FDCreature creature, FDPosition pos, int magicId)
        {
            Debug.Log("creatureMagic!!!");

            Creature c = gameMap.GetCreature(creature);
            c.SetActioned(true);
            onCreatureEndTurn(creature);
        }

        public void creatureUseItem(FDCreature creature, int itemIndex, FDCreature target)
        {
            Debug.Log("creatureUseItem!!!");

            Creature c = gameMap.GetCreature(creature);
            c.SetActioned(true);
            onCreatureEndTurn(creature);
        }

        public void creatureRest(FDCreature creature)
        {
            // Check Treasure

            onCreatureEndTurn(creature);
        }

        public void endTurnForAll()
        {

        }

        #endregion


        #region Internal Functions

        public void ShowCreatureInfoDialog(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected)
        {
            gameCanvas.ShowCreatureDialog(creature, infoType, onSelected);
        }

        public void ShowPromptDialog(FDCreature creature)
        {

        }

        public void PushActivity(Action<GameMain> action)
        {
            SimpleActivity activity = new SimpleActivity(action);
            activityQueue.Push(activity);
        }

        public void InsertActivity(Action<GameMain> action)
        {
            SimpleActivity activity = new SimpleActivity(action);
            activityQueue.Insert(activity);
        }

        public void InsertActivities(List<Action<GameMain>> actions)
        {
            List<ActivityBase> activities = actions.Select(action => { return new SimpleActivity(action); }).ToList<ActivityBase>();
            activityQueue.Insert(activities);
        }

        #endregion


        #region Game Loops Functions

        private void onInitialize()
        {

            gameMap.Initialize(chapterId);
            List<FDEvent> chapterEvents = ChapterLoader.LoadEvents(this, chapterId);
            eventHandler = new EventHandler(chapterEvents, this);
            enemyAIHandler = new AIHandler(this, CreatureFaction.Enemy);
            npcAIHandler = new AIHandler(this, CreatureFaction.Npc);


            this.gameMap.Map.TurnNo = 0;
            this.gameMap.Map.TurnType = CreatureFaction.Enemy;

            onStartNextTurn();
        }

        private void onStartNextTurn()
        {
            // Update the turn type and turn number
            switch(this.gameMap.Map.TurnType)
            {
                case CreatureFaction.Friend:
                    this.gameMap.Map.TurnType = CreatureFaction.Npc;
                    break;
                case CreatureFaction.Npc:
                    this.gameMap.Map.TurnType = CreatureFaction.Enemy;
                    break;
                case CreatureFaction.Enemy:
                    this.gameMap.Map.TurnNo++;
                    this.gameMap.Map.TurnType = CreatureFaction.Friend;
                    break;
            }

            //// Main entry to notify the turn events
            eventHandler.notifyTurnEvents();

            if (this.gameMap.Map.TurnType == CreatureFaction.Friend)
            {
                onPlayerTurn();
            }
            else if (this.gameMap.Map.TurnType == CreatureFaction.Npc)
            {
                onNpcTurn();
            }
            else if (this.gameMap.Map.TurnType == CreatureFaction.Enemy)
            {
                onEnemyTurn();
            }
        }

        private void onPlayerTurn()
        {
            // Reactivate all the creatures
            gameMap.Map.Creatures.ForEach(creature =>
            {
                Creature c = gameMap.GetCreature(creature);
                c.ResetTurnState();
            });

            // Show the Player turn dialog
            this.PushActivity((game) =>
            {
                //// gameCanvas.ShowNewTurnDialog(gameMap.Map.TurnNo, gameMap.Map.TurnType);
            });
        }

        private void onNpcTurn()
        {
            if (gameMap.Map.Npcs.Count > 0)
            {
                //// Start AI Handler to process the NPC turn
                npcAIHandler.Notified();
            }
            else
            {
                onStartNextTurn();
            }
        }

        private void onEnemyTurn()
        {
            // Reactivate all the creatures
            gameMap.Map.Creatures.ForEach(creature =>
            {
                Creature c = gameMap.GetCreature(creature);
                c.ResetTurnState();
            });

            // Show the Enemy turn dialog
            this.PushActivity((game) =>
            {
                //// gameCanvas.ShowNewTurnDialog(gameMap.Map.TurnNo, gameMap.Map.TurnType);
            });

            if (gameMap.Map.Enemies.Count > 0)
            {
                //// Start AI Handler to process the NPC turn
                enemyAIHandler.Notified();
            }
            else
            {
                // Not going to happen, since all enemies are eliminated, so game should be ended
                onStartNextTurn();
            }
        }

        private void onCreatureEndTurn(FDCreature creature)
        {
            // If creature not moved/actioned, recharge, may need activity
            if ( !creature.HasMoved() && !creature.HasActioned)
            {


            }

            Creature c = gameMap.GetCreature(creature);
            c.SetActioned(true);

            eventHandler.notifyTriggeredEvents();

            if (gameMap.Map.HasAllCreaturesActioned(creature.Faction))
            {
                this.PushActivity((game) =>
                {
                    game.onStartNextTurn();
                });

                return;
            }

            if (this.gameMap.Map.TurnType == CreatureFaction.Enemy)
            {
                enemyAIHandler.Notified();
            } else if (this.gameMap.Map.TurnType == CreatureFaction.Npc)
            {
                npcAIHandler.Notified();
            }
        }

        #endregion
    }

}