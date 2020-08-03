using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;

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
    /// All of the actions that GameState/GameEvent could call for
    /// </summary>
    public interface IGameAction
    {
        #region Information

        int TurnId();

        CreatureFaction TurnPhase();


        #endregion

        #region Game Operations

        void ComposeCreatureByDef(CreatureFaction faction, int creatureId, int definitionId, FDPosition position);

        /// <summary>
        /// The Walk is only used for animation, not used during the game playing
        /// </summary>
        /// <param name="moveAction"></param>
        void CreatureWalk(List<SingleWalkAction> moveAction);

        #endregion

        #region Do Operations

        void DoCreatureMove(int creatureId, FDMovePath movePath);

        void DoCreatureMoveCancel(int creatureId);

        void DoCreatureAttack(int creatureId, FDPosition targetPosition);

        void DoCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition, FDPosition transportPosition = null);

        void DoCreatureUseItem(int creatureId, int itemIndex);

        void DoCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId);

        void DoCreatureRest(int creatureId);


        void DoCreatureAllRest();


        #endregion

        #region Can Operations


        bool CanCreatureMove(int creatureId, FDPosition targetPosition);

        bool CanCreatureAttack(int creatureId, FDPosition targetPosition);

        bool CanCreatureSpellMagic(int creatureId, int magicId);

        bool CanCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition);

        bool CanCreatureUseItem(int creatureId, int itemIndex);

        bool CanCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId);



        #endregion

    }
}