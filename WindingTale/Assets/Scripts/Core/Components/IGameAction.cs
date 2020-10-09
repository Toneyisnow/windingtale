using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Data;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components
{
    public class SingleWalkAction
    {
        public int CreatureId
        {
            get; set;
        }

        /// <summary>
        /// The time of delay unit by moving a block
        /// </summary>
        public int DelayUnits
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public FDMovePath MovePath
        {
            get; set;
        }

        public SingleWalkAction(int creatureId, FDMovePath movePath, int delayUnits = 0)
        {
            this.CreatureId = creatureId;
            this.MovePath = movePath;
            this.DelayUnits = delayUnits;
        }

        public SingleWalkAction(int creatureId, FDPosition position, int delayUnits = 0)
        {
            this.CreatureId = creatureId;
            this.MovePath = FDMovePath.Create(position);
            this.DelayUnits = delayUnits;
        }


    }

    /// <summary>
    /// 
    /// </summary>
    public interface IGameInformative
    {
        int TurnId();

        int ChapterId();

        CreatureFaction TurnPhase();

        GameField GetField();

        FDCreature GetCreature(int creatureId);

        FDCreature GetDeadCreature(int creatureId);

        FDCreature GetCreatureAt(FDPosition position);

        List<FDCreature> GetAdjacentFriends(int creatureId);

        List<FDCreature> GetCreatureInRange(FDRange range, CreatureFaction faction);

        /// <summary>
        /// Provide a suggested target for attack in the range
        /// </summary>
        /// <param name="creatureId"></param>
        /// <returns></returns>
        FDCreature GetPreferredAttackTargetInRange(int creatureId);

        ItemDefinition GetTreatureAt(FDPosition position);

    }

    /// <summary>
    /// All of the actions that GameState/GameEvent could call for
    /// </summary>
    public interface IGameAction : IGameInformative
    {
        IGameCallback GetCallback();


        #region Game Internal Actions

        void ComposeCreature(CreatureFaction faction, int creatureId, int definitionId, FDPosition position, int dropItem = 0);

        void DisposeCreature(int creatureId, bool disposeFromUI = true);

        void SwitchCreature(int creatureId, CreatureFaction faction);

        /// <summary>
        /// The Walk is only used for animation, not used during the game playing
        /// </summary>
        /// <param name="moveAction"></param>
        void CreatureWalks(List<SingleWalkAction> moveActions);

        void CreatureWalk(SingleWalkAction moveAction);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creatureId"></param>
        /// <param name="sequenceId"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        void ShowTalk(int sequenceId, int startIndex, int endIndex);


        void GameOver();

        void LoadGame(BattleRecord battleRecord);

        bool CanSaveGame();

        void SaveGame();

        #endregion

        #region Do Actions

        void DoCreatureMove(int creatureId, FDMovePath movePath);

        void DoCreatureMoveCancel(int creatureId);

        void DoCreatureAttack(int creatureId, FDPosition targetPosition);

        void DoCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition, FDPosition transportPosition = null);

        void DoCreatureUseItem(int creatureId, int itemIndex, int targetCreatureId);

        void DoCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId, int exchangeItemIndex = -1);

        void DoCreatureRest(int creatureId);

        void PickTreasure(FDPosition position);

        void UpdateTreasure(FDPosition position, int itemId);

        void DoCreatureAllRest();


        #endregion

        #region Can Actions

        bool CanCreatureMove(int creatureId, FDPosition targetPosition);

        bool CanCreatureAttack(int creatureId, FDPosition targetPosition);

        bool CanCreatureSpellMagic(int creatureId, int magicId);

        bool CanCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition);

        bool CanCreatureUseItem(int creatureId, int itemIndex);

        bool CanCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId);



        #endregion

    }

    /// <summary>
    /// Game Operation called from Interface
    /// </summary>
    public interface IGameHandler : IGameInformative
    {
        void StartGame(ChapterRecord record);

        void HandleOperation(FDPosition position);

        void HandleOperation(int index);

    }



}