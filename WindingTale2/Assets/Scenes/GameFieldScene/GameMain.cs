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

        public GameObject turnInfoPrefab;

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

        /// <summary>
        /// The party that walked in from the village, as the last won battle and the shops
        /// left it. The chapter's own script decides who takes the field; whoever it spawns
        /// is restored from this rather than built from the definition, so levels, items,
        /// magic and experience carry across chapters. See LoadGame.
        ///
        /// Null for a battle nobody walked into -- a New Game, or the scene opened on its
        /// own -- and for a resumed save, whose creatures come from the save itself.
        /// </summary>
        public GameRecord PartyRecord { get; private set; }

        #endregion

        private int chapterId = 0;

        private TurnInfo turnInfo = null;

        /// <summary>The single save slot the Continue button reads and writes.</summary>
        public const string SaveRecordName = "current_game";

        /// <summary>Time the whole screen takes to fade to black when leaving the battle, in seconds.</summary>
        public const float QuitFadeDuration = 3.0f;

        /// <summary>
        /// Set the moment the battle is being left, whether lost or given up. It freezes
        /// the activity queue so nothing keeps playing on behind the fade, and it makes
        /// the second call to QuitToScene a no-op.
        /// </summary>
        private bool isQuitting = false;

        void Start()
        {
            DontDestroyOnLoad(gameObject);

            if (GameFiledSceneParams.isContinue)
            {
                GameFiledSceneParams.isContinue = false;
                ContinueFromRecord();
            }
            else
            {
                // The village hands its party over on GlobalVariables, the same key the
                // won battle handed it to the village on. A New Game has none.
                LoadGame(GlobalVariables.Take<GameRecord>(VillageScene.RecordVariableName));
            }
        }

        /// <summary>
        /// The "读取战场战况" path, shared by the Title scene's Continue button and the
        /// in-battle record menu. Two steps, in this order and with no overlap:
        ///
        ///   1. LoadChapter  -- everything the chapter defines and a battle never changes:
        ///                      the field shapes, the obstacles, where the chests sit, and
        ///                      the chapter's event scripts (which is where the creature
        ///                      spawns, the conversations and the win/lose conditions live).
        ///   2. ApplyRecord  -- everything the battle did change: the turn counter, the
        ///                      purse, every creature's position and state, the dead, the
        ///                      emptied chests, and which events have already fired.
        ///
        /// The chapter's own creature spawns are inside its turn events, so step 2's
        /// "already fired" flags are what stop them replaying on top of the saved
        /// creatures -- that is the one seam between the two steps.
        /// </summary>
        private void ContinueFromRecord()
        {
            GameMapRecord record = GameMapRecordManager.ReadRecord(SaveRecordName);
            if (record == null)
            {
                // Nothing to continue from. Both entry points check for a record before
                // sending us here, so this means the file went missing in between. Fall
                // back to a fresh chapter 1.
                Debug.LogWarning("No save file to continue from; starting chapter 1 instead.");
                LoadGame(null);
                return;
            }

            LoadChapter(record.ChapterId);

            GameMapRecordManager manager = new GameMapRecordManager();
            manager.ApplyRecord(record, this);

            // Chapter turn scripts are skipped when continuing, so start the music here
            PlayBackgroundMusic();

            onKickOff();
        }

        void Update()
        {
            //// The game is on its way out: let the fade play over a still battlefield.
            if (isQuitting)
            {
                return;
            }

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
            manager.SaveToFile(SaveRecordName, this);
        }

        /// <summary>
        /// 退出战场: the player gave up the battle from the record menu. Same quitting
        /// animation as a loss, but it hands straight back to the title screen -- there
        /// is no game-over screen to show for a battle nobody lost.
        /// </summary>
        public void OnQuit()
        {
            QuitToScene("TitleScene");
        }

        /// <summary>
        /// The battle is lost. Plays the quitting animation over the frozen field, then
        /// puts the game-over screen up in the field's place.
        /// </summary>
        public void OnGameOver()
        {
            QuitToScene("GameOverScene");
        }

        /// <summary>
        /// The quitting animation: fades the whole screen to black over
        /// QuitFadeDuration seconds, then tears the field scene down and loads
        /// <paramref name="sceneName"/> in its place.
        ///
        /// Everything freezes the moment it starts, and a second call is ignored --
        /// which covers both a second hero dying in the same round and an impatient
        /// second confirm on the quit menu.
        ///
        /// GameMain survives scene loads (DontDestroyOnLoad in Start), so like
        /// ContinueGame it has to take itself down before loading anything else --
        /// otherwise a second GameMain would come up with the next battle.
        /// </summary>
        private void QuitToScene(string sceneName)
        {
            if (isQuitting)
            {
                return;
            }
            isQuitting = true;

            StopBackgroundMusic();

            ScreenFader fader = ScreenFader.Create(0.0f);
            fader.FadeTo(1.0f, QuitFadeDuration, () =>
            {
                Destroy(gameObject);
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            });
        }

        /// <summary>
        /// The battle is won. The chapter's closing conversation is still sitting in the
        /// activity queue when the win fires, so the handover is queued behind it rather
        /// than run here: the player reads the last line, and only then does the field
        /// fade out into the village.
        /// </summary>
        public void OnGameWin()
        {
            this.PushActivity(game => game.EnterVillage());
        }

        /// <summary>
        /// Leaves the won battlefield for the village. What survives the battle is the
        /// party -- everyone healed up, the fallen back on their feet at 0 HP -- and the
        /// purse; GameRecordManager.CreateFromMapRecord is where that is decided. The
        /// village picks the record up out of GlobalVariables on the way in.
        ///
        /// Same quitting animation as a loss: the field freezes and fades to black, and
        /// the village fades up out of that same black.
        /// </summary>
        private void EnterVillage()
        {
            ChapterLoader.AdjustFriendsAfterWon(this, ChapterId);
            GameMapRecord mapRecord = new GameMapRecordManager().BuildRecord(this);
            GameRecord record = GameRecordManager.CreateFromMapRecord(mapRecord, PartyRecord);

            GlobalVariables.Set(VillageScene.RecordVariableName, record);

            QuitToScene("VillageScene");
        }

        /// <summary>
        /// Starts the battlefield's music: the chapter's Field track, the one the player's
        /// turn runs on. This is the entry point the chapter scripts and the continue path
        /// call to get the music going; the turn cycle then swaps it for the enemy track and
        /// back (see onNpcTurn / onEnemyTurn / onPlayerTurn).
        /// </summary>
        public void PlayBackgroundMusic()
        {
            PlayFieldMusic();
        }

        /// <summary>The Field track, played through the player's turn.</summary>
        private void PlayFieldMusic()
        {
            BackgroundMusicDefinition music = GetBackgroundMusic();
            BackgroundMusic.GetOrCreate().PlayClipByName(ResolveAudioPath(music?.Field));
        }

        /// <summary>The Enemy track, played through the enemy's turn.</summary>
        private void PlayEnemyMusic()
        {
            BackgroundMusicDefinition music = GetBackgroundMusic();
            BackgroundMusic.GetOrCreate().PlayClipByName(ResolveAudioPath(music?.Enemy));
        }

        /// <summary>This chapter's background music config, or null if it defines none.</summary>
        private BackgroundMusicDefinition GetBackgroundMusic()
        {
            ChapterDefinition chapter = DefinitionStore.Instance.LoadChapter(ChapterId);
            return chapter != null ? chapter.BackgroundMusic : null;
        }

        /// <summary>
        /// Turns a chapter's bare clip name ("Battle_2_HD_Final_V3") into the Resources path
        /// under Audios; an empty name (a situation with no music) resolves to null, which
        /// PlayClipByName reads as "stop".
        /// </summary>
        private static string ResolveAudioPath(string clipName)
        {
            return string.IsNullOrEmpty(clipName) ? null : "Audios/" + clipName;
        }

        public void StopBackgroundMusic()
        {
            BackgroundMusic musicPlayer = FindFirstObjectByType<BackgroundMusic>();
            if (musicPlayer != null)
            {
                musicPlayer.StopMusic();
            }
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

        /// <summary>
        /// Step one of loading a battle: builds the chapter's static half and nothing
        /// else. That is the field (shapes and obstacles), the chest placements, and the
        /// chapter's event scripts -- all of it read from the chapter definition, none of
        /// it dependent on how far a battle has got.
        ///
        /// It deliberately leaves the map empty of creatures: a chapter's creatures
        /// appear through its turn events, so a new game gets them when turn 1 fires and
        /// a continued game gets them from the record instead (see ContinueFromRecord).
        /// </summary>
        private void LoadChapter(int chapterId)
        {
            this.chapterId = chapterId;

            gameMap.Initialize(chapterId);

            List<FDEvent> chapterEvents = ChapterLoader.LoadEvents(this, chapterId);
            eventHandler = new EventHandler(chapterEvents, this);
        }

        /// <summary>
        /// Starts a chapter from its beginning, with the party the village handed over.
        /// The other way into a battle is ContinueFromRecord, which resumes a saved one
        /// instead; this one always begins at turn 1 and lets the chapter script play.
        ///
        /// The record is what the party carries between chapters -- levels, items, magic,
        /// experience and the purse. It is not laid over the map here: a chapter decides
        /// for itself who takes the field and where, so each friend is restored from the
        /// record at the moment the chapter's script spawns it (ChapterEvents.AddCreatureToMap).
        /// The purse has nowhere else to be applied, so it is applied here.
        ///
        /// A null record -- a New Game, or the field scene opened on its own -- starts
        /// everyone from their definitions, which is where chapter 1 begins from anyway.
        /// </summary>
        public void LoadGame(GameRecord record)
        {
            this.PartyRecord = record;

            LoadChapter(ResolveChapterId(record));

            if (record != null)
            {
                gameMap.Map.TotalMoney = record.TotalMoney;
            }

            onKickOff();
        }

        /// <summary>
        /// Which chapter LoadGame opens: the party's own, if it walked in with one -- the
        /// won battle already advanced it, so it names the chapter to play, not the one
        /// just finished. Otherwise the chapter handed over on GlobalVariables (the title's
        /// New Game sets 1), and failing that the first.
        ///
        /// The variable is taken whether it is used or not, so a stale one cannot outlive
        /// the load it was set for.
        /// </summary>
        private static int ResolveChapterId(GameRecord record)
        {
            int handedOverChapterId = GlobalVariables.Take<int>(GameFiledSceneParams.ChapterIdVariableName);

            if (record != null && record.ChapterId > 0)
            {
                return record.ChapterId;
            }

            return handedOverChapterId > 0 ? handedOverChapterId : 1;
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

            //// Turn-start banner. Only the player turn opens a new turn number, so that
            //// is the only one worth announcing. Runs as a duration activity so the turn
            //// waits for the animation to play out.
            if (this.gameMap.Map.TurnType == CreatureFaction.Friend)
            {
                int turnNo = this.gameMap.Map.TurnNo;
                this.PushActivity(
                    game => game.ShowTurnInfo(turnNo),
                    game => game.turnInfo == null || !game.turnInfo.IsPlaying
                );

                // Once the turn banner has finished, slide the cursor (and camera) to
                // hero #001. Only the player turn does this, right after the banner --
                // the AI sub-turns must not yank the cursor back to 001 while
                // conversations and enemy moves are playing out. Runs as a duration
                // activity so the turn waits for the slide to land.
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

        /// <summary>
        /// Spawns the turn banner and starts its animation. It destroys itself when done.
        /// </summary>
        private void ShowTurnInfo(int turnNo)
        {
            turnInfo = null;

            if (turnInfoPrefab == null)
            {
                return;
            }

            GameObject turnInfoObject = Instantiate(turnInfoPrefab);
            turnInfo = turnInfoObject.GetComponent<TurnInfo>();
            if (turnInfo == null)
            {
                Destroy(turnInfoObject);
                return;
            }

            turnInfo.Play(turnNo);
        }

        private void onPlayerTurn()
        {
            // Back to the player's turn: bring the Field music back (a no-op if it is
            // already the track playing, e.g. after a turn with no enemy music).
            PlayFieldMusic();

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
            // The NPC turn plays in silence.
            StopBackgroundMusic();

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
            // The enemy's turn swaps in the Enemy track.
            PlayEnemyMusic();

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