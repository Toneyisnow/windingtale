using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.ActionStates;
using WindingTale.Core.Components.Events;
using WindingTale.Core.Components.Packs;
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

        public int ChapterId
        {
            get; private set;
        }

        private int turnId = 0;
        private CreatureFaction turnPhase = CreatureFaction.Friend;

        private List<FDCreature> Friends = null;
        private List<FDCreature> Enemies = null;
        private List<FDCreature> Npcs = null;
        private List<FDCreature> Deads = null;

        private IGameCallback gameCallback = null;

        public GameManager(IGameCallback gameCallback)
        {
            this.gameCallback = gameCallback;

            // Load Creatures
            this.Friends = new List<FDCreature>();
            this.Enemies = new List<FDCreature>();
            this.Npcs = new List<FDCreature>();
            this.Deads = new List<FDCreature>();

            eventManager = new GameEventManager(this);

            dispatcher = new GameStateDispatcher(this);
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

            eventManager.NotifyTurnEvents();
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
            this.ChapterId = chapterId;
            // Load Field Map
            ChapterDefinition chapter = DefinitionStore.Instance.LoadChapter(chapterId);
            gameField = new GameField(chapter);

            // Load Events
            EventLoader eventLoader = EventLoaderFactory.GetEventLoader(chapterId, eventManager);
            eventLoader.LoadEvents();

            

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

        public FDCreature GetCreature(int creatureId)
        {
            FDCreature cre = this.Friends.Find((c) => { return c.CreatureId == creatureId; });
            if (cre != null)
            {
                return cre;
            }

            cre = this.Enemies.Find((c) => { return c.CreatureId == creatureId; });
            if (cre != null)
            {
                return cre;
            }

            cre = this.Npcs.Find((c) => { return c.CreatureId == creatureId; });
            if (cre != null)
            {
                return cre;
            }

            return null;
        }

        public FDCreature GetDeadCreature(int creatureId)
        {
            return this.Deads.Find((c) => { return c.CreatureId == creatureId; });
        }

        public FDCreature GetCreatureAt(FDPosition position)
        {
            FDCreature creature = this.Friends.Find(c => { return c.Position.AreSame(position); });
            if (creature != null)
            {
                return creature;
            }

            creature = this.Enemies.Find(c => { return c.Position.AreSame(position); });
            if (creature != null)
            {
                return creature;
            }

            creature = this.Npcs.Find(c => { return c.Position.AreSame(position); });
            if (creature != null)
            {
                return creature;
            }

            return null;
        }

        #endregion

        #region Actions

        public void ComposeCreature(CreatureFaction faction, int creatureId, int definitionId, FDPosition position, int dropItem = 0)
        {
            CreatureDefinition creatureDef = DefinitionStore.Instance.GetCreatureDefinition(definitionId);
            FDCreature creature = new FDCreature(creatureId, faction, creatureDef, position);

            switch (faction)
            {
                case CreatureFaction.Friend:
                    this.Friends.Add(creature);
                    break;
                case CreatureFaction.Enemy:
                    if (dropItem > 0)
                    {
                        creature.Data.DropItem = dropItem;
                    }
                    this.Enemies.Add(creature);
                    break;
                case CreatureFaction.Npc:
                    this.Npcs.Add(creature);
                    break;
                default:
                    break;
            }

            ComposeCreaturePack pack = new ComposeCreaturePack(creatureId, creatureDef.AnimationId, position);
            gameCallback.OnReceivePack(pack);
        }

        public void DisposeCreature(int creatureId, bool disposeFromUI = true)
        {
            FDCreature creature = GetCreature(creatureId);
            if (creature != null)
            {
                for (int i = 0; i < this.Friends.Count; i++)
                {
                    if (this.Friends[i].CreatureId == creatureId)
                    {
                        this.Friends.RemoveAt(i);
                    }
                }
                for (int i = 0; i < this.Enemies.Count; i++)
                {
                    if (this.Enemies[i].CreatureId == creatureId)
                    {
                        this.Enemies.RemoveAt(i);
                    }
                }
                for (int i = 0; i < this.Npcs.Count; i++)
                {
                    if (this.Npcs[i].CreatureId == creatureId)
                    {
                        this.Npcs.RemoveAt(i);
                    }
                }

                if (disposeFromUI)
                {
                    DisposeCreaturePack pack = new DisposeCreaturePack(creature);
                    gameCallback.OnReceivePack(pack);
                }
            }
            else
            {
                creature = GetDeadCreature(creatureId);
                if (creature != null)
                {
                    this.Deads.Remove(creature);
                }
            }
        }

        public void SwitchCreature(int creatureId, CreatureFaction faction)
        {
            FDCreature creature = GetCreature(creatureId);
            if (creature != null)
            {
                DisposeCreature(creatureId, false);
                switch(faction)
                {
                    case CreatureFaction.Friend:
                        this.Friends.Add(creature);
                        break;
                    case CreatureFaction.Npc:
                        this.Npcs.Add(creature);
                        break;
                    case CreatureFaction.Enemy:
                        this.Enemies.Add(creature);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// The Walk is only used for animation, not used during the game playing
        /// </summary>
        /// <param name="moveAction"></param>
        public void CreatureWalks(List<SingleWalkAction> walkActions)
        {
            if (walkActions == null || walkActions.Count == 0)
            {
                return;
            }

            if (walkActions.Count == 1)
            {
                var movePack = HandleWalkAction(walkActions[0]);
                gameCallback.OnReceivePack(movePack);
            }
            else
            {
                BatchPack batch = new BatchPack();
                for(int i = 0; i < walkActions.Count; i++)
                {
                    var movePack = HandleWalkAction(walkActions[i]);
                    if (movePack != null)
                    {
                        batch.Add(movePack);
                    }
                }
                gameCallback.OnReceivePack(batch);
            }
        }

        public void CreatureWalk(SingleWalkAction walkAction)
        {
            var movePack = HandleWalkAction(walkAction);
            gameCallback.OnReceivePack(movePack);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creatureId"></param>
        /// <param name="sequenceId"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public void ShowTalk(int sequenceId, int startIndex, int endIndex)
        {
            for(int i = startIndex; i <= endIndex; i++)
            {
                string conversationId = string.Format(@"Conversation_{0}_{1}_{2}", this.ChapterId, sequenceId, i);
                TalkPack pack = new TalkPack(conversationId);
                gameCallback.OnReceivePack(pack);
            }

            
        }

        public void GameOver()
        {

        }

        #endregion

        #region Game Operation called from interface

        /// <summary>
        /// Game operation when user clicked on position
        /// </summary>
        /// <param name="position"></param>
        public void OnSelectPosition(FDPosition position)
        {
            dispatcher.OnSelectPosition(position);
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

        private PackBase HandleWalkAction(SingleWalkAction walkAction)
        {
            if (walkAction == null)
            {
                return null;
            }

            CreatureMovePack movePack = new CreatureMovePack(walkAction.CreatureId, walkAction.MovePath);

            FDCreature creature = GetCreature(walkAction.CreatureId);
            if (creature == null)
            {
                return null;
            }

            creature.MoveTo(walkAction.MovePath.Desitination);

            if (walkAction.DelayUnits > 0)
            {
                IdlePack idle = IdlePack.FromTimeUnit(walkAction.DelayUnits);
                return new SequencedPack(idle, movePack);
            }

            return movePack;
        }


        private void PostCreatureAction()
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