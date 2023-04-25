



using System;

using WindingTale.Common;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Events;
using WindingTale.Core.Map;
using WindingTale.Core.Files;
using WindingTale.Core.Objects;
using WindingTale.UI.Components.Activities;

namespace WindingTale.UI.Scenes.Game
{
    public class GameMain
    {
        #region Properties

        /// <summary>
        /// /public int TurnNo { get; private set; }
        /// </summary>

        //// public CreatureType TurnType { get; private set; }

        public GameMap GameMap { get; private set; }

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
            this.TurnType = CreatureType.Player;
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
            this.GameMap.TurnNo++;
            this.OnPlayerTurn();
        }

        private void OnPlayerTurn()
        {
            this.GameMap.TurnType = CreatureType.Player;

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

            this.GameMap.TurnType = CreatureType.Npc;
            CheckTurnEvents();

        }

        private void OnNpcEndTurn()
        {
            // Set all npc creatures to active
        }

        private void OnEnemyTurn()
        {
            this.GameMap.TurnType = CreatureType.Enemy;
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
            foreach (FDEvent fdEvent in this.GameMap.Events)
            {
                if (fdEvent.EventType == FDEventType.Turn && fdEvent.IsTriggered(this.GameMap))
                {
                    fdEvent.Execute(this.GameMap);
                }
            }
        }

        private void CheckConditionEvents()
        {
            foreach (FDEvent fdEvent in this.GameMap.Events)
            {
                if (fdEvent.EventType == FDEventType.Condition && fdEvent.IsTriggered(this.GameMap))
                {
                    fdEvent.Execute(this.GameMap);
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

        public void CreatureAttack(FDCreature creature, FDPosition position)
        {
            AttackResult result = this.GameEngine.handleCreatureAttack(creature, position);

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
            // Check treature

            PushActivity(() => OnCreatureDone(creature));
        }

        public void EndAllFriendsTurn()
        {
            PromptActivity prompt = new PromptActivity(doEndAllFriendsTurn);
        }

        #endregion



    }
}
