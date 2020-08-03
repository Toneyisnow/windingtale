using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.ActionStates;
using WindingTale.Core.Components.Events;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Components;

namespace WindingTale.Core.Components
{
    /// <summary>
    /// 
    /// </summary>
    public class GameManager : IGameAction
    {
        private GameStateDispatcher dispatcher = null;

        private GameField gameField = null;

        private GameEventManager eventManager = null;

        private int turnId = 0;
        private CreatureFaction turnPhase = CreatureFaction.Friend;

        private List<FDCreature> Friends = null;
        private List<FDCreature> Enemies = null;
        private List<FDCreature> Npcs = null;

        private IGameCallback gameCallback = null;

        public GameManager(IGameCallback gameCallback)
        {
            this.gameCallback = gameCallback;

            // Load Creatures
            this.Friends = new List<FDCreature>();
            this.Enemies = new List<FDCreature>();
            this.Npcs = new List<FDCreature>();

            eventManager = new GameEventManager(this);

            dispatcher = new GameStateDispatcher(this, gameCallback);
        }

        public void StartGame(ChapterRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            InitializeChapter(record.ChapterId);

            turnId = 1;
            turnPhase = CreatureFaction.Friend;

            eventManager.NotifyTurnEvents(turnId, turnPhase);
        }

        public void LoadGame(BattleRecord battleRecord)
        {

        }

        public BattleRecord SaveGame()
        {
            return null;
        }

        public GameStateDispatcher GetDispatcher()
        {
            return dispatcher;
        }

        public GameField GetField()
        {
            return this.gameField;
        }

        public IGameCallback GetCallback()
        {
            return this.gameCallback;
        }

        private void InitializeChapter(int chapterId)
        {
            // Load Field Map
            ChapterDefinition chapter = DefinitionStore.Instance.LoadChapter(chapterId);
            gameField = new GameField(chapter);

            // Load Events
            EventLoader eventLoader = EventLoaderFactory.GetEventLoader(chapterId, eventManager);
            eventLoader.LoadEvents();

            

        }


        public void ComposeCreatureByDef(CreatureFaction faction, int creatureId, int definitionId, FDPosition position)
        {
            CreatureDefinition creatureDef = DefinitionStore.Instance.GetCreatureDefinition(definitionId);
            FDCreature creature = new FDCreature(creatureDef, position);

            switch(faction)
            {
                case CreatureFaction.Friend:
                    this.Friends.Add(creature);
                    break;
                case CreatureFaction.Enemy:
                    this.Enemies.Add(creature);
                    break;
                case CreatureFaction.Npc:
                    this.Npcs.Add(creature);
                    break;
                default:
                    break;
            }




        }

        /// <summary>
        /// The Walk is only used for animation, not used during the game playing
        /// </summary>
        /// <param name="moveAction"></param>
        public void CreatureWalk(List<SingleWalkAction> moveAction)
        {
            if (moveAction == null || moveAction.Count == 0)
            {
                return;
            }




        }

        #region Information

        public int TurnId()
        {
            return this.turnId;
        }

        public CreatureFaction TurnPhase()
        {
            return this.turnPhase;
        }

        #endregion

        #region Do Operations

        public void DoCreatureMove(int creatureId, FDMovePath movePath)
        {

        }

        public void DoCreatureMoveCancel(int creatureId)
        {

        }

        public void DoCreatureAttack(int creatureId, FDPosition targetPosition)
        {

        }

        public void DoCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition, FDPosition transportPosition = null)
        {

        }

        public void DoCreatureUseItem(int creatureId, int itemIndex)
        {

        }

        public void DoCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId)
        {

        }

        public void DoCreatureRest(int creatureId)
        {

        }


        public void DoCreatureAllRest()
        {

        }


        #endregion

        #region Can Operations


        public bool CanCreatureMove(int creatureId, FDPosition targetPosition)
        {
            return true;
        }

        public bool CanCreatureAttack(int creatureId, FDPosition targetPosition)
        {
            return true;
        }

        public bool CanCreatureSpellMagic(int creatureId, int magicId)
        {
            return true;
        }

        public bool CanCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition)
        {
            return true;
        }

        public bool CanCreatureUseItem(int creatureId, int itemIndex)
        {
            return true;
        }

        public bool CanCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId)
        {
            return true;
        }



        #endregion

        #region Private Methods

        private void postCreatureAction()
        {
            // Check all creatures are taken actions, and do startNewTurn

            // StartNewTurnPhase();
        }

        private void StartNewTurnPhase()
        {
            // Turn Id and Phase ++

            // Notify turn events


            // Show turn icon

        }

        #endregion

    }
}