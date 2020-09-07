using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.ActionStates;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Components.Data;
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
    public class GameManager : IGameAction, IGameHandler
    {
        private GameStateDispatcher dispatcher = null;

        private GameField gameField = null;

        private GameEventManager eventManager = null;

        private int chapterId;

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

            turnId = 0;

            StartNewTurnPhase();
        }

        public void LoadGame(BattleRecord battleRecord)
        {

        }

        public bool CanSaveGame()
        {
            return false;
        }

        public void SaveGame()
        {
            
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
            this.chapterId = chapterId;
            // Load Field Map
            ChapterDefinition chapter = DefinitionStore.Instance.LoadChapter(chapterId);
            gameField = new GameField(chapter);

            // Load Events
            EventLoader eventLoader = EventLoaderFactory.GetEventLoader(chapterId, eventManager);
            eventLoader.LoadEvents();

            

        }


        #region Informative

        public int TurnId()
        {
            return this.turnId;
        }

        public int ChapterId()
        {
            return this.chapterId;
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

        public List<FDCreature> GetAdjacentFriends(int creatureId)
        {
            return null;
        }


        public FDCreature GetPreferredAttackTargetInRange(int creatureId)
        {
            FDCreature creature = this.GetCreature(creatureId);
            AttackRangeFinder finder = new AttackRangeFinder(this);
            FDRange range = finder.FindRange(creature);

            // Get a preferred target in range
            foreach(FDPosition position in range.Positions)
            {
                FDCreature target = this.GetCreatureAt(position);
                if (target != null && target.Faction == CreatureFaction.Enemy)
                {
                    return target;
                }
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
            gameCallback.OnHandlePack(pack);
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
                    gameCallback.OnHandlePack(pack);
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
                gameCallback.OnHandlePack(movePack);
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
                gameCallback.OnHandlePack(batch);
            }
        }

        public void CreatureWalk(SingleWalkAction walkAction)
        {
            var movePack = HandleWalkAction(walkAction);
            gameCallback.OnHandlePack(movePack);
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
                string conversationId = string.Format(@"Conversation_{0}_{1}_{2}", this.ChapterId(), sequenceId, i);
                TalkPack pack = new TalkPack(conversationId);
                gameCallback.OnHandlePack(pack);
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
        public void HandleOperation(FDPosition position)
        {
            dispatcher.OnSelectPosition(position);
        }

        public void HandleOperation(int index)
        {
            dispatcher.OnSelectIndex(index);
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
            FDCreature creature = this.GetCreature(creatureId);
            PostCreatureAction(creature);
        }

        public void DoCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition, FDPosition transportPosition = null)
        {
            FDCreature creature = this.GetCreature(creatureId);
            PostCreatureAction(creature);
        }

        public void DoCreatureUseItem(int creatureId, int itemIndex)
        {
            FDCreature creature = this.GetCreature(creatureId);
            PostCreatureAction(creature);
        }

        public void DoCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId)
        {
            FDCreature creature = this.GetCreature(creatureId);
            PostCreatureAction(creature);
        }

        public void DoCreatureRest(int creatureId)
        {
            FDCreature creature = this.GetCreature(creatureId);
            if (creature == null)
            {
                return;
            }

            if (!creature.HasMoved() && creature.Data.Hp < creature.Data.HpMax)
            {
                // Recover HP and do animation
                creature.Data.Hp = CreatureCalculator.GetRestRecoveredHp(creature.Data.Hp, creature.Data.HpMax);
                CreatureAnimationPack pack = new CreatureAnimationPack(creature, CreatureAnimationPack.AnimationType.Rest);
                gameCallback.OnHandlePack(pack);
            }

            PostCreatureAction(creature);
        }

        /// <summary>
        /// To pick up the treature at the current position. The ItemIndex is the exchanged item if item list is full.
        /// </summary>
        /// <param name="creatureId"></param>
        /// <param name="itemIndex"></param>
        public void PickTreasure(int creatureId, int itemIndex = -1)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void DoCreatureAllRest()
        {
            foreach (FDCreature creature in this.Friends)
            {
                if (creature.HasActioned)
                {
                    continue;
                }

                if (creature.Data.Hp < creature.Data.HpMax)
                {
                    // Recover HP and do animation
                    creature.Data.Hp = CreatureCalculator.GetRestRecoveredHp(creature.Data.Hp, creature.Data.HpMax);
                    CreatureAnimationPack pack = new CreatureAnimationPack(creature, CreatureAnimationPack.AnimationType.Rest);
                    gameCallback.OnHandlePack(pack);
                }

                // Set creature status
                creature.HasActioned = true;
                gameCallback.OnHandlePack(new RefreshCreaturePack(creature));
            }

            // Do startNewTurn
            StartNewTurnPhase();
            StartNewTurnPhase();
            StartNewTurnPhase();
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

            creature.SetMoveTo(walkAction.MovePath.Desitination);

            if (walkAction.DelayUnits > 0)
            {
                IdlePack idle = IdlePack.FromTimeUnit(walkAction.DelayUnits);
                return new SequencedPack(idle, movePack);
            }

            return movePack;
        }


        private void PostCreatureAction(FDCreature creature)
        {
            // Set creature status
            creature.HasActioned = true;
            gameCallback.OnHandlePack(new RefreshCreaturePack(creature));

            // Check all creatures are taken actions, and do startNewTurn

            // StartNewTurnPhase();
        }

        private void StartNewTurnPhase()
        {
            // Turn Id and Phase ++
            if (turnId == 0)
            {
                turnId = 1;
                turnPhase = CreatureFaction.Friend;
            }
            else
            {
                if (turnPhase == CreatureFaction.Friend)
                {
                    turnPhase = CreatureFaction.Npc;
                }
                else if (turnPhase == CreatureFaction.Npc)
                {
                    turnPhase = CreatureFaction.Enemy;
                }
                else if (turnPhase == CreatureFaction.Enemy)
                {
                    turnId++;
                    turnPhase = CreatureFaction.Friend;
                }
            }

            // Notify turn events
            eventManager.NotifyTurnEvents();

            // Reset Creature status
            foreach(FDCreature creature in this.Friends)
            {
                creature.OnTurnStart();
            }

            foreach (FDCreature creature in this.Enemies)
            {
                creature.OnTurnStart();
            }

            foreach (FDCreature creature in this.Npcs)
            {
                creature.OnTurnStart();
            }

            // Show turn icon

        }

        #endregion

    }
}