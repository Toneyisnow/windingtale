



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
using WindingTale.Core.Algorithms;
using System.Collections.Generic;
using WindingTale.Chapters;
using static UnityEditor.Progress;
using static UnityEngine.GraphicsBuffer;
using WindingTale.UI.ActionStates;
using System.Diagnostics;
using UnityEngine.UIElements;
using WindingTale.AI;

namespace WindingTale.UI.Scenes.Game
{
    public class GameMain
    {
        #region Properties


        public int TurnNo { get; private set; }


        public CreatureFaction TurnType { get; private set; }

        public GameMap GameMap { get; private set; }

        public GameHandler GameHandler { get; private set; }

        public IGameInterface GameInterface { get; private set; }

        public ActivityManager ActivityManager { get; private set; }

        public AIHandler EnemyAIHandler { get; private set; }

        public AIHandler NpcAIHandler { get; private set; }


        public StateDispatcher State { get; private set; }


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


        #endregion


        #region Game Entry Points

        /// <summary>
        /// Start a game from the beginning.
        /// </summary>
        public static GameMain StartNewGame(IGameInterface gameInterface, ActivityManager activityManager)
        {
            return StartChapter(gameInterface , activityManager, 1, null);
        }

        /// <summary>
        /// Start a new chapter.
        /// </summary>
        /// <param name="chapterId"></param>
        /// <param name="recordId"></param>
        public static GameMain StartChapter(IGameInterface gameInterface, ActivityManager activityManager, int chapterId, GameRecord record)
        {
            Instance = new GameMain();

            // Load chapter map
            DefinitionStore.Instance.LoadChapter(chapterId);
            ChapterDefinition chapterDefinition = ChapterLoader.LoadChapter(chapterId);
            Instance.GameMap = GameMap.LoadFromChapter(chapterDefinition, record);
            Instance.GameHandler = new GameHandler(Instance.GameMap);
            Instance.GameInterface = gameInterface;
            Instance.ActivityManager = activityManager;
            Instance.EnemyAIHandler = new AIHandler(Instance, CreatureFaction.Enemy);
            Instance.NpcAIHandler = new AIHandler(Instance, CreatureFaction.Npc);

            Instance.State = new StateDispatcher(Instance);


            // Load chapter creatures from record

            // Test
            //FDCreature c = Instance.AddCreature(CreatureFaction.Friend, 2, 2, FDPosition.At(0, 0));
            //Instance.CreatureMove(c, FDMovePath.Create(FDPosition.At(10, 20)));

            Instance.OnNewTurn();

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
            BatchActivity batch = new BatchActivity();
            foreach(FDCreature c in this.GameMap.Friends)
            {
                c.HasActioned = false;
                batch.Add(new CreatureRefreshActivity(c.Id));
            }
            this.ActivityManager.Push(batch);
        }

        private void OnNpcTurn()
        {
            if (this.GameMap.Npcs.Count == 0)
            {
                this.OnEnemyTurn();
                return;
            }

            this.TurnType = CreatureFaction.Npc;
            foreach (FDCreature c in this.GameMap.Npcs)
            {
                c.HasActioned = false;
            }
            CheckTurnEvents();

            CallbackActivity callback = new CallbackActivity(() =>
            {
                this.NpcAIHandler.Notified();
            });
            PushActivity(callback);
        }

        private void OnNpcEndTurn()
        {
            // Set all npc creatures to active
            BatchActivity batch = new BatchActivity();
            foreach (FDCreature c in this.GameMap.Npcs)
            {
                c.HasActioned = false;
                batch.Add(new CreatureRefreshActivity(c.Id));
            }
            this.ActivityManager.Push(batch);
        }

        private void OnEnemyTurn()
        {
            UnityEngine.Debug.Log("OnEnemyTurn");
            this.TurnType = CreatureFaction.Enemy;
            foreach (FDCreature c in this.GameMap.Enemies)
            {
                c.HasActioned = false;
            }

            CheckTurnEvents();

            CallbackActivity callback = new CallbackActivity(() =>
            {
                this.EnemyAIHandler.Notified();
            });
            PushActivity(callback);
        }

        private void OnEnemyEndTurn()
        {
            UnityEngine.Debug.Log("OnEnemyEndTurn");

            // Set all enemy creatures to active
            BatchActivity batch = new BatchActivity();
            foreach (FDCreature c in this.GameMap.Enemies)
            {
                c.HasActioned = false;
                batch.Add(new CreatureRefreshActivity(c.Id));
            }
            PushActivity(batch);
        }

        private void OnCreatureDone(FDCreature creature)
        {
            UnityEngine.Debug.Log("OnCreatureDone !!! creature=" + creature.Id);

            // Set the creature to inactive
            creature.HasActioned = true;
            creature.PrePosition = creature.Position;
            CreatureRefreshActivity refresh = new CreatureRefreshActivity(creature.Id);
            this.ActivityManager.Push(refresh);

            // Check if all creatures are inactive, then end turn

            if (creature.Faction == CreatureFaction.Friend && this.GameMap.Friends.Find(c => !c.HasActioned) == null)
            {
                PushCallbackActivity(() => this.OnPlayerEndTurn());
                PushCallbackActivity(() => this.OnNpcTurn());
            }
            else if (creature.Faction == CreatureFaction.Npc)
            {
                if (this.GameMap.Npcs.Find(c => !c.HasActioned) == null)
                {
                    PushCallbackActivity(() => this.OnNpcEndTurn());
                    PushCallbackActivity(() => this.OnEnemyTurn());
                }
                else
                {
                    this.NpcAIHandler.Notified();
                }
            }
            else if (creature.Faction == CreatureFaction.Enemy)
            {
                if (this.GameMap.Enemies.Find(c => !c.HasActioned) == null)
                {
                    PushCallbackActivity(() => this.OnEnemyEndTurn());
                    PushCallbackActivity(() => this.OnNewTurn());
                }
                else
                {
                    this.EnemyAIHandler.Notified();
                }
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

        private void PushActivities(List<ActivityBase> activities)
        {
            this.ActivityManager.Insert(activities);
        }

        private void PushActivity(ActivityBase activity)
        {
            this.ActivityManager.Push(activity);
        }
        private void InsertActivity(ActivityBase activity)
        {
            this.ActivityManager.Insert(activity);
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
        public FDCreature CreatureAdd(CreatureFaction faction, int creatureId, int definitionId, FDPosition position, int dropItemId = 0, AITypes aiType = AITypes.AIType_Aggressive)
        {
            CreatureDefinition definition = DefinitionStore.Instance.GetCreatureDefinition(definitionId);
            FDCreature creature = faction == CreatureFaction.Friend ? 
                 new FDCreature(creatureId , definition, faction) :
                 new FDAICreature(creatureId, definition, faction, aiType);

            creature.Position = position;
            this.GameMap.Creatures.Add(creature);

            CallbackActivity callback = new CallbackActivity(() => this.GameInterface.AddCreatureUI(creature, position));
            PushActivity(callback);

            return creature;
        }

        public void CreatureDispose(int creatureId)
        {
            FDCreature creature = this.GameMap.GetCreatureById(creatureId);
            if (creature != null)
            {
                this.GameMap.Creatures.Remove(creature);

                CallbackActivity callback = new CallbackActivity(() => this.GameInterface.RemoveCreatureUI(creature));
                PushActivity(callback);
            }
        }

        public void CreatureMove(FDCreature creature, FDMovePath movePath)
        {
            CreatureMoveActivity creatureMove = new CreatureMoveActivity(creature, movePath);
            PushActivity(creatureMove);
        }

        public void CreatureMoveCancel(FDCreature creature)
        {
            // If the creature has moved, reset the creature position
            creature.ResetPosition();
            CreatureRefreshActivity reset = new CreatureRefreshActivity(new List<int> { creature.Id });
            PushActivity(reset);
        }

        /// <summary>
        /// @deprecated
        /// </summary>
        /// <param name="creatureMoves"></param>
        public void CreatureBatchMove(List<Tuple<FDCreature, FDMovePath>> creatureMoves)
        {
            BatchActivity batchActivity = new BatchActivity();
            foreach(Tuple<FDCreature, FDMovePath> move in creatureMoves)
            {
                //// batchActivity.Add(new CreatureMoveActivity(move.Item1, move.Item2));
            }
            PushActivity(batchActivity);
        }

        public void CreatureAttack(FDCreature creature, FDCreature target)
        {
            AttackResult result = this.GameHandler.HandleCreatureAttack(creature, target, this.GameMap);
            List<ActivityBase> activities = new List<ActivityBase>();

            // TODO: AttackActivity to Fight Scene

            // 1. Apply the attack result to subject and target
            result.Damages.ForEach(damage => target.ApplyDamage(damage));
            result.BackDamages.ForEach(backDamage => creature.ApplyDamage(backDamage));

            CheckConditionEvents();

            // 2. Check if the creature or target is dead, if so, remove the target from the map
            List<int> deadCreatureIds = new List<int>();
            if (creature.IsDead())
            {
                deadCreatureIds.Add(creature.Id);
            }
            if (target.IsDead())
            {
                deadCreatureIds.Add(target.Id);
            }

            // Dead Animation
            if (deadCreatureIds.Count > 0)
            {
                CreatureDeadActivity deadActivity = new CreatureDeadActivity(deadCreatureIds);
                activities.Add(deadActivity);
            }

            CheckConditionEvents();

            // 3. Check gained item
            if (result.GainedItems.Count > 0)
            {
                result.GainedItems.ForEach((itemId) =>
                {
                    List<ActivityBase> acts = OnCreatureGainItem(creature, itemId);
                    activities.AddRange(acts);
                });
            }

            // 4. Apply experience
            if (result.Experience > 0)
            {
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 13, result.Experience);
                TalkActivity talk = new TalkActivity(message, creature);
                activities.Add(talk);

                LevelUpInfo levelUp = ApplyExperience(creature, result.Experience);
                if (levelUp != null)
                {
                    // 等级上升了！
                    FDMessage levelUpMessage = FDMessage.Create(FDMessage.MessageTypes.Information, 18);
                    TalkActivity talk2 = new TalkActivity(levelUpMessage, creature);
                    activities.Add(talk2);
                }
            }

            activities.Add(new CallbackActivity(() => OnCreatureDone(creature)));
            PushActivities(activities);
        }

        public void CreatureMagic(FDCreature creature, int magicIndex, FDPosition position)
        {
            int magicId = creature.GetMagicAt(magicIndex);
            if (magicId == 0)
            {
                throw new Exception("Creature doesn't have magic at index.");
            }

            MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);
            MagicResult magicResult = this.GameHandler.HandleCreatureMagic(creature, position, magic, this.GameMap);

            List<ActivityBase> activities = new List<ActivityBase>();

            // TODO: AttackActivity to Fight Scene

            // 1. Apply the attack result to subject and target
            creature.Mp -= magic.MpCost;
            List<int> deadCreatureIds = new List<int>();
            foreach (int creatureId in magicResult.Results.Keys)
            {
                SoloResult result = magicResult.Results[creatureId];
                FDCreature target = this.GameMap.GetCreatureById(creatureId);
                if (result.ResultType == SoloResultType.Damage)
                {
                    target.ApplyDamage((DamageResult)result);
                }
                else if (result.ResultType == SoloResultType.Effect)
                {
                    target.ApplyEffect((EffectResult)result);
                }

                if (target.IsDead())
                {
                    deadCreatureIds.Add(target.Id);
                }
            };

            CheckConditionEvents();

            // 2. Check if the creature or target is dead, if so, remove the target from the map
            
            // Dead Animation
            if (deadCreatureIds.Count > 0)
            {
                CreatureDeadActivity deadActivity = new CreatureDeadActivity(deadCreatureIds);
                activities.Add(deadActivity);
            }

            CheckConditionEvents();

            // 3. Check gained item
            if (magicResult.GainedItems.Count > 0)
            {
                magicResult.GainedItems.ForEach((itemId) =>
                {
                    List<ActivityBase> acts = OnCreatureGainItem(creature, itemId);
                    activities.AddRange(acts);
                });
            }

            // 4. Apply experience
            if (magicResult.Experience > 0)
            {
                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 13, magicResult.Experience);
                TalkActivity talk = new TalkActivity(message, creature);
                activities.Add(talk);

                LevelUpInfo levelUp = ApplyExperience(creature, magicResult.Experience);
                if (levelUp != null)
                {
                    // 等级上升了！
                    FDMessage levelUpMessage = FDMessage.Create(FDMessage.MessageTypes.Information, 18);
                    TalkActivity talk2 = new TalkActivity(levelUpMessage, creature);
                    activities.Add(talk2);
                }
            }

            activities.Add(new CallbackActivity(() => OnCreatureDone(creature)));
            PushActivities(activities);
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
            // Check creature not moved and not actioned, if yes, do recover
            if (!creature.HasMoved() && !creature.HasActioned)
            {
                RecoverResult recover = this.GameHandler.HandleCreatureRecover(creature);
                if (recover != null && recover.Amount > 0)
                {
                    CreatureRecoverActivity activity = new CreatureRecoverActivity(creature);
                    PushActivity(activity);
                }
            }

            PushCallbackActivity(() => OnCreatureDone(creature));
        }




        public void EndAllFriendsTurn()
        {
            this.GameMap.Friends.ForEach(friend =>
            {
                if (!friend.HasActioned)
                {
                    CreatureRest(friend);
                }
            });
        }

        #endregion

        #region Private Methods

        private List<ActivityBase> OnCreatureGainItem(FDCreature creature, int itemId)
        {
            List<ActivityBase> activities = new List<ActivityBase>();

            FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 25, itemId);
            TalkActivity talk = new TalkActivity(message, creature);
            activities.Add(talk);

            // If item is full, prompt whether to drop item
            if (creature.IsItemsFull())
            {
                FDMessage confirm = FDMessage.Create(FDMessage.MessageTypes.Confirm, 7);
                PromptActivity prompt = new PromptActivity(confirm, (result) => {
                    if (result == 1)
                    {
                        ShowCreatureInfoActivity showItems = new ShowCreatureInfoActivity(this, creature, CreatureInfoType.SelectAllItem, (index) =>
                        {
                            if (index > 0)
                            {
                                creature.RemoveItemAt(index);
                                creature.AddItem(itemId);
                            }
                            else
                            {
                                // 那么就不要了
                                FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 10, itemId);
                                TalkActivity talk = new TalkActivity(message, creature);
                                InsertActivity(talk);
                            }
                        });
                        InsertActivity(showItems);
                    }
                    else
                    {
                        // 那么就不要了
                        FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 10, itemId);
                        TalkActivity talk = new TalkActivity(message, creature);
                        InsertActivity(talk);
                    }
                }, creature);
                activities.Add(prompt);
            }

            return activities;
        }

        private LevelUpInfo ApplyExperience(FDCreature creature, int experience)
        {
            creature.Exp += experience;

            if (creature.Exp < 100 || creature.Level >= creature.Definition.GetMaxLevel()) 
            {
                return null;
            }

            creature.Exp -= 100;
            creature.Level++;

            LevelUpDefinition levelUpDef = DefinitionStore.Instance.GetLevelUpDefinition(creature.Definition.DefinitionId);
            LevelUpMagicDefinition levelUpMagic = DefinitionStore.Instance.GetLevelUpMagicDefinition(creature.Definition.DefinitionId, creature.Level);
            
            LevelUpInfo levelUp = new LevelUpInfo();
            levelUp.ImprovedHp = FDRandom.IntFromSpan(levelUpDef.HpRange);
            levelUp.ImprovedMp = FDRandom.IntFromSpan(levelUpDef.MpRange);
            levelUp.ImprovedAp= FDRandom.IntFromSpan(levelUpDef.ApRange);
            levelUp.ImprovedDp = FDRandom.IntFromSpan(levelUpDef.DpRange);
            levelUp.ImprovedDx = FDRandom.IntFromSpan(levelUpDef.DxRange);
            levelUp.LearntMagicId = levelUpMagic.MagicId;

            return levelUp;
        }

        #endregion

    }
}
