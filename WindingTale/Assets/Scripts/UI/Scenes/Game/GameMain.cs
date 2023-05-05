



using System;

using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Files;
using WindingTale.Core.Objects;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core;
using UnityEditor.VersionControl;
using WindingTale.UI.Activities;
using WindingTale.UI.MapObjects;
using WindingTale.Core.Algorithms;
using System.Collections.Generic;
using WindingTale.Chapters;

namespace WindingTale.UI.Scenes.Game
{
    public class GameMain
    {
        #region Properties


        public int TurnNo { get; private set; }


        public CreatureFaction TurnType { get; private set; }

        public GameMap GameMap { get; private set; }

        public GameHandler GameHandler { get; private set; }

        public ActivityManager ActivityManager { get; set; }

        /// TODO: Whether should I use singleton pattern here?
        public static GameMain Instance { get; private set; } = new GameMain();


        #endregion

        #region Callbacks

        public Action OnGameQuit { get; set; } = () => { };

        public Action OnGameWin { get; set; } = () => { };

        public Action OnGameOver { get; set; } = () => { };

        #endregion

        #region Private Members

        private GameMain()
        {
            this.TurnNo = 0;
            this.TurnType = CreatureFaction.Friend;
        }

        private ActivityManager activityManager = null;

        #endregion


        #region Game Entry Points

        /// <summary>
        /// Start a game from the beginning.
        /// </summary>
        public static GameMain StartNewGame()
        {
            return StartChapter(1, null);
        }

        /// <summary>
        /// Start a new chapter.
        /// </summary>
        /// <param name="chapterId"></param>
        /// <param name="recordId"></param>
        public static GameMain StartChapter(int chapterId, GameRecord record)
        {
            Instance = new GameMain();

            // Load chapter map
            ChapterDefinition chapterDefinition = ChapterLoader.LoadChapter(chapterId);
            Instance.GameMap = GameMap.LoadFromChapter(chapterDefinition, record);
            Instance.GameHandler = new GameHandler(Instance.GameMap);

            // Load chapter creatures from record

            return Instance;
        }


        public static GameMain LoadMapRecord()
        {
            Instance = new GameMain();
            return Instance;
        }

        /// <summary>
        /// Serialize the data into GameMapRecord and save to JSON file.
        /// </summary>
        public void SaveMapRecord()
        {

        }

        #endregion

        #region Game Private Actions

        private void OnNewTurn()
        {
            this.TurnNo++;
            this.OnPlayerTurn();
        }

        private void OnPlayerTurn()
        {
            this.TurnType = CreatureFaction.Friend;

            CheckTurnEvents();

            // Show Turn start icon
        }

        private void OnPlayerEndTurn()
        {
            // Set all player creatures to active

        }

        private void OnNpcTurn()
        {
            if (this.GameMap.Npcs.Count == 0)
            {
                this.OnEnemyTurn();
                return;
            }

            this.TurnType = CreatureFaction.Npc;
            CheckTurnEvents();

        }

        private void OnNpcEndTurn()
        {
            // Set all npc creatures to active
        }

        private void OnEnemyTurn()
        {
            this.TurnType = CreatureFaction.Enemy;
            CheckTurnEvents();
        }

        private void OnEnemyEndTurn()
        {
            // Set all enemy creatures to active
        }

        private void OnCreatureDone(FDCreature creature)
        {
            // If creature haven't moved, then take rest recover


            // Set the creature to inactive
            creature.HasActioned = true;

            // Check if all creatures are inactive, then end turn

            if (creature.Faction == CreatureFaction.Friend && this.GameMap.Friends.Find(c => !c.HasActioned) == null)
            {
                PushCallbackActivity(() => this.OnPlayerEndTurn());
                PushCallbackActivity(() => this.OnNpcTurn());
            }
            else if (creature.Faction == CreatureFaction.Npc && this.GameMap.Npcs.Find(c => !c.HasActioned) == null)
            {
                PushCallbackActivity(() => this.OnNpcEndTurn());
                PushCallbackActivity(() => this.OnEnemyTurn());
            }
            else if (creature.Faction == CreatureFaction.Enemy && this.GameMap.Enemies.Find(c => !c.HasActioned) == null)
            {
                PushCallbackActivity(() => this.OnEnemyEndTurn());
                PushCallbackActivity(() => this.OnNewTurn());
            }
        }

        private void CheckTurnEvents()
        {
            foreach (FDEvent fdEvent in this.GameMap.GetActiveEvents())
            {
                if (fdEvent.EventType == FDEventType.Turn)
                {
                    FDTurnEvent turnEvent = (FDTurnEvent)fdEvent;
                    if (turnEvent.TurnNo == this.TurnNo && turnEvent.TurnType == this.TurnType)
                    {
                        fdEvent.Execute(this);
                    }
                }
            }
        }

        private void CheckConditionEvents()
        {
            foreach (FDEvent fdEvent in this.GameMap.GetActiveEvents())
            {
                if (fdEvent.EventType == FDEventType.Condition)
                {
                    FDConditionEvent conditionEvent = (FDConditionEvent)fdEvent;
                    if (conditionEvent.Match(this.GameMap))
                    {
                        conditionEvent.Execute(this);
                    }
                }
            }
        }

        private void PushActivity(ActivityBase activity)
        {
            this.ActivityManager.Push(activity);
        }

        private void PushCallbackActivity(Action callback)
        {
            CallbackActivity activiy = new CallbackActivity(callback);
            this.ActivityManager.Push(activiy);
        }

        private void doEndAllFriendsTurn(int index)
        {
            // 1 means YES
            if (index == 1)
            {

            }
            else
            {

            }
        }

        #endregion

        #region Game Public Actions

        /// <summary>
        /// Create a new Creature and add to the map.
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="creatureId"></param>
        /// <param name="definitionId"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public FDCreature AddCreature(CreatureFaction faction, int creatureId, int definitionId, FDPosition position, int dropItemId = 0)
        {
            //
            return null;
        }

        public void CreatureMove(FDCreature creature, FDMovePath movePath)
        {

        }

        public void CreatureMoveCancel(FDCreature creature)
        {
            // If the creature has moved, reset the creature position
            creature.ResetPosition();
            CreatureRefreshActivity reset = new CreatureRefreshActivity(new List<int> { creature.Id });
            PushActivity(reset);
        }

        public void CreatureBatchMove(List<Tuple<FDCreature, FDMovePath>> creatureMoves)
        {
            BatchActivity batchActivity = new BatchActivity();
            foreach(Tuple<FDCreature, FDMovePath> move in creatureMoves)
            {
                batchActivity.Add(new CreatureMoveActivity(move.Item1, move.Item2));
            }
            PushActivity(batchActivity);
        }

        public void CreatureAttack(FDCreature creature, FDCreature target)
        {
            AttackResult result = this.GameHandler.HandleCreatureAttack(creature, target);

            // TODO: AttackActivity to Fight Scene



            CheckConditionEvents();

            // Dead Animation


            // Check if the creature is dead and remove dead creatures


            PushCallbackActivity(() => OnCreatureDone(creature));
        }

        public void CreatureMagic(FDCreature creature, int magicIndex, FDPosition position)
        {

            PushCallbackActivity(() => OnCreatureDone(creature));
        }
        public void CreatureUseItem(FDCreature creature, int itemIndex, FDPosition position)
        {

            PushCallbackActivity(() => OnCreatureDone(creature));
        }

        public void CreatureExchangeItem(FDCreature creature, int itemIndex, FDCreature target, int backItemIndex = -1)
        {
            PushCallbackActivity(() => OnCreatureDone(creature));
        }

        /// <summary>
        /// Note: this function will not handle pick up treasure, which is handled in ActionState
        /// </summary>
        /// <param name="creature"></param>
        public void CreatureRest(FDCreature creature)
        {
            PushCallbackActivity(() => OnCreatureDone(creature));
        }

        public void EndAllFriendsTurn()
        {
            this.GameMap.Friends.ForEach(friend =>
            {
                if (!friend.HasActioned)
                {
                    PushCallbackActivity(() => OnCreatureDone(friend));
                }
            });
        }

        #endregion



    }
}
