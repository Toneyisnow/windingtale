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
            // The real chapter id lives on the loaded map; fall back to the field
            // before a map is loaded.
            get { return (gameMap != null && gameMap.Map != null) ? gameMap.Map.ChapterId : chapterId; }
        }

        #endregion

        private int chapterId = 0;

        void Start()
        {
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

        private int GetLocalTaiId(FDCreature creature)
        {
            ShapeDefinition shape = gameMap.Map.Field.GetShapeAt(creature.Position);
            if (shape != null && shape.TaiId > 0)
                return shape.TaiId;

            ChapterDefinition chapter = DefinitionStore.Instance.LoadChapter(gameMap.Map.ChapterId);
            if (chapter?.DefaultTaiIds != null && chapter.DefaultTaiIds.Count > 0)
                return chapter.DefaultTaiIds[UnityEngine.Random.Range(0, chapter.DefaultTaiIds.Count)];

            return 0;
        }

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
            GlobalVariables.Set("LocalTaiId", GetLocalTaiId(creature));
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
                    applyExperienceAndLevelUp(creature, result.Experience);
                }
                if (result.BackExperience > 0)
                {
                    applyExperienceAndLevelUp(target, result.BackExperience);
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
                GlobalVariables.Set("LocalTaiId", GetLocalTaiId(creature));
                GlobalVariables.Set("MagicResult", result);
                SceneManager.LoadScene("GameBattleScene", LoadSceneMode.Additive);
            }

            // Apply the results
            this.PushActivity((gameMain) =>
            {
                c.SetActioned(true);

                // Pay the MP for the spell. The battle scene draws its MP drain from
                // MagicResult.MpBefore, so it does not matter whether this lands before or
                // after the animation gets there.
                creature.UpdateMp(-result.MpCost);

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
                    applyExperienceAndLevelUp(creature, result.Experience);
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
            // Note: Don't check treature here.
            // Make animation for rest recovery

            this.PushActivity((gameMain) =>
            {
                onCreatureEndTurn(creature);
            });
        }

        public void endTurnForAll()
        {
            List<FDCreature> friends = gameMap.Map.Friends;
            foreach (FDCreature creature in friends)
            {
                if (!creature.HasActioned)
                {
                    onCreatureEndTurn(creature);
                }
            }
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

            // After any turn-start conversation, slide the cursor (and camera) to hero
            // #001. Runs as a duration activity so the turn waits for the slide to land.
            this.PushActivity(
                game =>
                {
                    var hero = game.gameMap.Map.GetCreatureById(1);
                    if (hero != null && hero.Position != null)
                    {
                        game.gameMap.SlideCursorTo(hero.Position, GameCanvas.DialogPosition.Bottom);
                    }
                },
                game => !game.gameMap.IsSlideBusy
            );

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

        /// <summary>
        /// Grants experience and, when it takes the creature over the threshold, applies
        /// the level up and queues the level-up dialog right behind the experience one.
        /// A single gain never exceeds 100 exp, so this can raise at most one level.
        /// </summary>
        private void applyExperienceAndLevelUp(FDCreature creature, int experience)
        {
            LevelUpInfo levelUp = BattleHandler.ApplyExperience(creature, experience);
            if (levelUp != null)
            {
                BattleHandler.ApplyLevelUp(creature, levelUp);
            }

            // InsertActivity puts an activity at the FRONT of the queue, so these go in
            // back to front: the level-up line first, then the experience line on top of
            // it. The player then sees "+N exp" followed by the level-up summary.
            if (levelUp != null)
            {
                this.InsertActivity(new TalkActivity(buildLevelUpMessages(creature, levelUp), creature));
            }

            FDMessage expMessage = FDMessage.Create(FDMessage.MessageTypes.Information, 5, experience);
            this.InsertActivity(new TalkActivity(expMessage, creature));
        }

        /// <summary>
        /// The level-up dialog: "等级上升了！" followed by one line per stat that actually
        /// improved, and the newly learnt magic if there is one. Messages 12-17 all start
        /// with '#', so they read as separate lines of the same dialog.
        /// </summary>
        private static List<FDMessage> buildLevelUpMessages(FDCreature creature, LevelUpInfo levelUp)
        {
            List<FDMessage> messages = new List<FDMessage>
            {
                FDMessage.Create(FDMessage.MessageTypes.Information, 11)
            };

            if (levelUp.ImprovedHp > 0)
            {
                messages.Add(FDMessage.Create(FDMessage.MessageTypes.Information, 12, levelUp.ImprovedHp));
            }
            if (levelUp.ImprovedMp > 0)
            {
                messages.Add(FDMessage.Create(FDMessage.MessageTypes.Information, 13, levelUp.ImprovedMp));
            }
            if (levelUp.ImprovedAp > 0)
            {
                messages.Add(FDMessage.Create(FDMessage.MessageTypes.Information, 14, levelUp.ImprovedAp));
            }
            if (levelUp.ImprovedDp > 0)
            {
                messages.Add(FDMessage.Create(FDMessage.MessageTypes.Information, 15, levelUp.ImprovedDp));
            }
            if (levelUp.ImprovedDx > 0)
            {
                messages.Add(FDMessage.Create(FDMessage.MessageTypes.Information, 16, levelUp.ImprovedDx));
            }

            if (levelUp.LearntMagicId > 0)
            {
                MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(levelUp.LearntMagicId);
                if (magic != null)
                {
                    messages.Add(FDMessage.Create(FDMessage.MessageTypes.Information, 17, 0, 0, magic.Name));
                }
            }

            return messages;
        }

        private void onCreatureEndTurn(FDCreature creature)
        {
            // Resting in place (neither moved nor acted this turn) recovers HP.
            bool hasRestRecover = !creature.HasMoved() && !creature.HasActioned;
            int recoveredHp = hasRestRecover
                ? CreatureFormula.GetRestRecoveredHp(creature.Hp, creature.HpMax) - creature.Hp
                : 0;

            // Always settled in data, even when the creature has no icon to grey out --
            // the turn cannot end while anyone is still flagged as not having acted.
            creature.HasActioned = true;

            Creature c = gameMap.GetCreature(creature);
            if (recoveredHp > 0)
            {
                // The HP gain, the flash and the visual greyout all happen inside the
                // activity. Queued rather than inserted so a whole batch of resting
                // creatures (endTurnForAll) flashes in order, one after another.
                this.PushActivity(ActivityFactory.CreatureRecoverActivity(creature, recoveredHp));
            }
            else if (c != null)
            {
                // Nothing to recover (already at full HP, or the creature moved/acted):
                // no animation, grey out right away.
                c.SetActioned(true);
            }

            if (c != null)
            {
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