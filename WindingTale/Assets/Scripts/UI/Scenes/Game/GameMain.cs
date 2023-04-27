



using System;

using WindingTale.Common;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Files;
using WindingTale.Core.Objects;
using WindingTale.UI.Components.Activities;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core;
using WindingTale.Core.Components.Packs;
using UnityEditor.VersionControl;

namespace WindingTale.UI.Scenes.Game
{
    public class GameMain
    {
        #region Properties


        public int TurnNo { get; private set; }
        

        public CreatureFaction TurnType { get; private set; }

        public GameMap GameMap { get; private set; }

        public GameHandler GameHandler { get; private set; }

        /// TODO: Whether should I use singleton pattern here?
        public static GameMain Instance { get; private set; } = new GameMain();

        #endregion

        #region Callbacks

        public DoCallback OnGameQuit { get; set; } = () => { };

        public DoCallback OnGameWin { get; set; } = () => { };

        public DoCallback OnGameOver { get; set; } = () => { };

        #endregion

        #region Private Members

        private GameMain()
        {
            this.TurnNo = 0;
            this.TurnType = CreatureFaction.Friend;
        }

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
            ChapterDefinition chapterDefinition = ChapterLoader.Load(chapterId);
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

            // Check if all creatures are inactive, then end turn

            if (creature.Faction == CreatureFaction.Friend && this.GameMap.Friends.Find(c => !c.HasActioned) == null)
            {
                PushActivity(() => this.OnPlayerEndTurn());
                PushActivity(() => this.OnNpcTurn());
            }
            else if (creature.Faction == CreatureFaction.Npc && this.GameMap.Npcs.Find(c => !c.HasActioned) == null)
            {
                PushActivity(() => this.OnNpcEndTurn());
                PushActivity(() => this.OnEnemyTurn());
            }
            else if (creature.Faction == CreatureFaction.Enemy && this.GameMap.Enemies.Find(c => !c.HasActioned) == null)
            {
                PushActivity(() => this.OnEnemyEndTurn());
                PushActivity(() => this.OnNewTurn());
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
                        fdEvent.Execute(this.GameMap);
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
                        conditionEvent.Execute(this.GameMap);
                    }
                }
            }
        }

        private void PushActivity(DoCallback callback)
        {
            CallbackActivity activity = new CallbackActivity(callback);
            this.ActivityManager.pushActivity(activity);
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

        public void CreatureMove(FDCreature creature, FDPosition position)
        {

        }

        public void CreatureMoveCancel(FDCreature creature)
        {
            // If the creature has moved, reset the creature position
            this.creature.ResetPosition();
            CreatureRefreshPack reset = new CreatureRefreshPack(this.creature.Clone());
            SendPack(reset);


        }

        public void CreatureAttack(FDCreature creature, FDCreature target)
        {
            AttackResult result = this.GameHandler.HandleCreatureAttack(creature, target);

            // TODO: AttackActivity to Fight Scene



            CheckConditionEvents();

            // Dead Animation


            // Check if the creature is dead and remove dead creatures


            PushActivity(() => OnCreatureDone(creature));
        }

        public void CreatureMagic(FDCreature creature, int magicIndex, FDPosition position)
        {

            PushActivity(() => OnCreatureDone(creature));
        }
        public void CreatureUseItem(FDCreature creature, int itemIndex, FDPosition position)
        {

            PushActivity(() => OnCreatureDone(creature));
        }

        public void CreatureRest(FDCreature creature)
        {
            // Check Treasure
            FDTreasure treasure = this.GameMap.GetTreatureAt(creature.Position);
            if (treasure != null)
            {
                PromptActivity prompt = new PromptActivity((index) =>
                {
                    // 1 means YES
                    if (index == 1)
                    {
                        if (creature.IsItemsFull())
                        {
                            PromptActivity confirmExchange = new PromptActivity((index) => {
                                if (index == 1)
                                {
                                    TalkActivity talk = new TalkActivity(messageId);
                                    PushActivity(talk);
                                }
                                else
                                {
                                    TalkActivity talk = new TalkActivity(messageId);
                                    PushActivity(talk);
                                }
                            });
                            PushActivity(confirmExchange);
                        }
                    }
                    else
                    {
                        TalkActivity talk = new TalkActivity(messageId);
                        PushActivity(talk);
                    }
                });
                PushActivity(prompt);
            }

            PushActivity(() => OnCreatureDone(creature));
        }

        public void EndAllFriendsTurn()
        {
            PromptActivity prompt = new PromptActivity((index) =>
            {
                // 1 means YES
                if (index == 1)
                {

                }
                else
                {

                }
            });

            PushActivity(prompt);
        }

        #endregion



    }
}
