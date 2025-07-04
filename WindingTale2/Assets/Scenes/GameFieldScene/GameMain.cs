using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using WindingTale.AI;
using WindingTale.Chapters;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Files;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene.Activities;
using WindingTale.UI.Dialogs;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.GameFieldScene
{

    public class GameMain : MonoBehaviour, IGameActions
    {
        #region Properties

        public GameObject mapObject;

        public GameObject canvasObject;

        public ActivityQueue activityQueue = null;

        public static GameMain getDefault()
        {
            GameMain game = FindFirstObjectByType<GameMain>(); // GameObject.Find("GameRoot").GetComponent<GameMain>();
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

        
        public EventHandler eventHandler = null;

        private AIHandler enemyAIHandler = null;

        private AIHandler npcAIHandler = null;

        public int ChapterId
        {
            get { return chapterId; }
        }

        #endregion

        private int chapterId = 0;

        void Start()
        {
            // 确保音乐管理器在场景切换时不会销毁
            DontDestroyOnLoad(gameObject);

            if (GameFiledSceneParams.isContinue)
            {
                LoadFromMapRecord();
                GameFiledSceneParams.isContinue = false;
            }
            else
            {
                StartChapter(1);
            }
        }

        private void LoadFromMapRecord()
        {
            GameMapRecordManager manager = new GameMapRecordManager();
            manager.LoadFromFile("current_game", this);

            onKickOff();
        }

        void Update()
        {
            if (SceneManager.loadedSceneCount > 1)
            {
                return;
            }

            activityQueue.Update();
        }

        #region Game Cycles

        public void ContinueGame()
        {
            Debug.Log("Continue Game...");
            GameFiledSceneParams.isContinue = true;

            Destroy(gameObject);
            SceneManager.LoadScene("GameFieldScene", LoadSceneMode.Single);
        }

        public void SaveGame()
        {
            GameMapRecordManager manager = new GameMapRecordManager();
            manager.SaveToFile("current_game", this);
        }

        public void OnQuit()
        {

        }

        public void OnGameOver()
        {

        }

        public void OnGameWin()
        {

        }

        public void PlayBackgroundMusic()
        {
            BackgroundMusic musicPlayer = FindFirstObjectByType<BackgroundMusic>();
            musicPlayer.SetAudioClipName("Audios/Battle_2_HD_Final_V3");
            musicPlayer.PlayMusic();
        }

        public void StopBackgroundMusic()
        {

        }

        #endregion

        #region Creature Related Operation

        public void creatureMoveAsync(FDCreature creature, FDMovePath movePath)
        {
            this.PushActivity(ActivityFactory.CreatureWalkActivity(creature.Id, movePath));
        }

        public void creatureAttackAsync(FDCreature creature, FDCreature target)
        {
            Debug.Log("creatureAttack!!!");

            Creature c = gameMap.GetCreature(creature);

            AttackResult result = BattleHandler.HandleCreatureAttack(creature, target, gameMap.Map.Field);
            
            // Play attack animation
            GlobalVariables.Set("AttackResult", result);
            SceneManager.LoadScene("GameBattleScene", LoadSceneMode.Additive);


            // Apply the results
            this.PushActivity((gameMain) =>
            {
                c.SetActioned(true);

                BattleHandler.ApplyDamage(target, result.Damages.Last());
                if (result.BackDamages.Count > 0)
                {
                    BattleHandler.ApplyDamage(creature, result.BackDamages.Last());
                }
            });

            // Check if the target is dead
            this.PushActivity((gameMain) =>
            {
                List<ActivityBase> dyingActivities = new List<ActivityBase>();
                if (target.IsDead())
                {
                    dyingActivities.Add(ActivityFactory.CreatureDyingActivity(target));
                }

                if (creature.IsDead())
                {
                    dyingActivities.Add(ActivityFactory.CreatureDyingActivity(creature));
                }

                if (dyingActivities.Count > 0)
                {
                    // Play dying animation
                    gameMain.InsertActivity(new ParallelActivity(dyingActivities));
                }
            });

            // Say about experience
            this.PushActivity((gameMain) =>
            {
                if (result.Experience > 0)
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 5, result.Experience);
                    this.InsertActivity(new TalkActivity(message, creature));

                    BattleHandler.ApplyExperience(creature, result.Experience);
                }
                if (result.BackExperience > 0)
                {
                    FDMessage message = FDMessage.Create(FDMessage.MessageTypes.Information, 5, result.BackExperience);
                    this.InsertActivity(new TalkActivity(message, target));

                    BattleHandler.ApplyExperience(target, result.BackExperience);
                }
            });


            this.PushActivity((gameMain) =>
            {
                onCreatureEndTurn(creature);
            });
        }

        public void creatureMagic(FDCreature creature, FDPosition position, int magicId)
        {
            Debug.Log("creatureMagic!!!");

            Creature c = gameMap.GetCreature(creature);
            MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);

            DirectRangeFinder directRangeFinder = new DirectRangeFinder(gameMap.Map.Field, position, magic.EffectScope);
            FDRange magicScope = directRangeFinder.CalculateRange();

            List<FDCreature> targetList = gameMap.Map.GetCreaturesInRange(magicScope.ToList(), CreatureFaction.Enemy);

            MagicResult result = BattleHandler.HandleCreatureMagic(creature, targetList, position, magic, gameMap.Map.Field);
            c.SetActioned(true);

            // Play attack animation
            if (magic.hasAnimaion())
            {
                GlobalVariables.Set("MagicResult", result);
                SceneManager.LoadScene("GameBattleScene", LoadSceneMode.Additive);
            }

            // Apply the results
            this.PushActivity((gameMain) =>
            {
                c.SetActioned(true);

                foreach (var resultPair in result.Results)
                {
                    int targetCreatureId = resultPair.Key;
                    FDCreature target = targetList.Find(t => t.Id == targetCreatureId);
                    var soleResult = resultPair.Value;
                    if (soleResult.ResultType == SoloResultType.Damage)
                    {
                        DamageResult damageResult = (DamageResult)soleResult;
                        BattleHandler.ApplyDamage(target, damageResult);
                    }
                };
            });

            // Check dying events
            this.PushActivity((gameMain) =>
            {
                gameMain.eventHandler.notifyTriggeredEvents();
            });
                
            // Check if the target is dead
            this.PushActivity((gameMain) =>
            {
                List<ActivityBase> dyingActivities = new List<ActivityBase>();
                foreach (var magicResult in result.Results)
                {
                    int targetCreatureId = magicResult.Key;
                    FDCreature target = targetList.Find(t => t.Id == targetCreatureId);
                    if (target.IsDead())
                    {
                        dyingActivities.Add(ActivityFactory.CreatureDyingActivity(target));
                    }
                };

                if (dyingActivities.Count > 0)
                {
                    // Play dying animation
                    gameMain.InsertActivity(new ParallelActivity(dyingActivities));
                }
            });

            // Say about experience
            this.PushActivity((gameMain) =>
            {
                if (result.Experience > 0)
                {
                    this.InsertActivity(gameMain =>
                    {
                        // Show experience dialog
                    });
                }
            });


            this.PushActivity((gameMain) =>
            {
                onCreatureEndTurn(creature);
            });
        }

        public void creatureUseItem(FDCreature creature, int itemIndex, FDCreature target)
        {
            Debug.Log("creatureUseItem!!!");

            int itemId = creature.Items[itemIndex];
            creature.RemoveItemAt(itemIndex);
            CreatureFormula.TakeItemEffect(target, itemId);

            Creature c = gameMap.GetCreature(creature);
            c.SetActioned(true);

            this.PushActivity((gameMain) =>
            {
                onCreatureEndTurn(creature);
            });
        }

        public void creatureRest(FDCreature creature)
        {
            // Check Treasure

            Creature c = gameMap.GetCreature(creature);
            c.SetActioned(true);


            this.PushActivity((gameMain) =>
            {
                onCreatureEndTurn(creature);
            });
        }

        public void endTurnForAll()
        {

        }

        #endregion


        #region Internal Functions

        public void PushActivity(ActivityBase activity)
        {
            activityQueue.Push(activity);
        }

        public void PushActivity(Action<GameMain> action)
        {
            SimpleActivity activity = new SimpleActivity(action);
            PushActivity(activity);
        }

        public void PushActivity(Action<GameMain> startAction, Func<GameMain, bool> checkEnd)
        {
            DurationActivity activity = new DurationActivity(startAction, checkEnd);
            PushActivity(activity);
        }

        public void InsertActivity(ActivityBase activity)
        {
            activityQueue.Insert(activity);
        }

        public void InsertActivity(Action<GameMain> action)
        {
            SimpleActivity activity = new SimpleActivity(action);
            InsertActivity(activity);
        }

        public void InsertActivities(List<Action<GameMain>> actions)
        {
            List<ActivityBase> activities = actions.Select(action => { return new SimpleActivity(action); }).ToList<ActivityBase>();
            activityQueue.Insert(activities);
        }

        #endregion


        #region Game Loops Functions

        private void StartChapter(int chapterId)
        {
            gameMap.Initialize(chapterId);

            List<FDEvent> chapterEvents = ChapterLoader.LoadEvents(this, chapterId);
            eventHandler = new EventHandler(chapterEvents, this);

            onKickOff();
        }

        private void onKickOff()
        {
            enemyAIHandler = new AIHandler(this, CreatureFaction.Enemy);
            npcAIHandler = new AIHandler(this, CreatureFaction.Npc);
            activityQueue = new ActivityQueue(this);

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
                PushActivity(gameMain => onPlayerTurn());
            }
            else if (this.gameMap.Map.TurnType == CreatureFaction.Npc)
            {
                PushActivity(gameMain => onNpcTurn());
            }
            else if (this.gameMap.Map.TurnType == CreatureFaction.Enemy)
            {
                PushActivity(gameMain => onEnemyTurn());
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
                // Show animation for rest recovery
                this.InsertActivity(ActivityFactory.CreatureRestRecoverActivity(creature));
            }

            Creature c = gameMap.GetCreature(creature);
            if (c != null)
            {
                c.SetActioned(true);
                c.creature.PrePosition = null;
            }

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
                bool aiActioned = enemyAIHandler.Notified();
                
            } else if (this.gameMap.Map.TurnType == CreatureFaction.Npc)
            {
                bool aiActioned = npcAIHandler.Notified();
            }
        }

        #endregion
    }

}