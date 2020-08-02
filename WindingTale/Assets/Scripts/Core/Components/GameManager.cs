using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    }
}