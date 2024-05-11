using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Chapters;
using WindingTale.Core.Common;
using WindingTale.Core.Events;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;

namespace WindingTale.Scenes.GameFieldScene
{

    public class GameMain : MonoBehaviour, IGameActions
    {
        public GameObject mapObject;

        public GameMap gameMap
        {
            get { return mapObject.GetComponent<GameMap>(); }
        }

        private int chapterId = 0;
        private EventHandler eventHandler = null;



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
            GameMap mapComponent = this.gameMap.GetComponent<GameMap>();

            mapComponent.Initialize(chapterId);
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

        public void creatureMove(FDCreature creature, FDPosition pos)
        {

        }

        public void creatureAttack(FDCreature creature, FDCreature target)
        {

        }

        public void creatureMagic(FDCreature creature, FDPosition pos, int magicId)
        {

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