using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WindingTale.Chapters;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
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

        #endregion

        private int chapterId = 0;

        // Start is called before the first frame update
        void Start()
        {
            chapterId = 1;
            onStart();
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

        public void onStart()
        {

            gameMap.Initialize(chapterId);
            List<FDEvent> chapterEvents = ChapterLoader.LoadEvents(this, chapterId);
            eventHandler = new EventHandler(chapterEvents, this);

            this.gameMap.Map.TurnNo = 1;
            this.gameMap.Map.TurnType = Core.Definitions.CreatureFaction.Friend;

            eventHandler.notifyTurnEvents();
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
        }

        public void creatureMagic(FDCreature creature, FDPosition pos, int magicId)
        {
            Debug.Log("creatureMagic!!!");
        }

        public void creatureUseItem(FDCreature creature, int itemIndex, FDCreature target)
        {
            Debug.Log("creatureUseItem!!!");
        }

        public void creatureRest(FDCreature creature)
        {
            // Check Treasure

            onCreatureEndTurn();
        }

        public void endTurnForAll()
        {

        }

        #endregion


        #region Internal Functions

        public void ShowCreatureInfoDialog(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected)
        {
            gameCanvas.ShowDialog(creature, infoType, onSelected);
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


        #region Private Methods

        private void onNewTurn()
        {

        }

        private void onPlayerTurn()
        {

        }

        private void onNpcTurn()
        {

        }

        private void onEnemyTurn()
        {

        }

        private void onCreatureEndTurn()
        {
            // If creature not moved/actioned, recharge, may need activity

        }

        private void checkEvents()
        {
            eventHandler.notifyTurnEvents();
        }

        #endregion
    }

}