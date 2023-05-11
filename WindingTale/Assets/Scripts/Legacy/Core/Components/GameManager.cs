using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WindingTale.AI;
using WindingTale.AI.Delegates;
using WindingTale.Common;
using WindingTale.Core.Components.ActionStates;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Components.Data;
using WindingTale.Core.Components.Events;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Components;

namespace WindingTale.Legacy.Core.Components
{
    /// <summary>
    /// 
    /// </summary>
    public class GameManager : IGameAction, IGameHandler
    {
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

        public void NotifyAI()
        {
            if (this.turnPhase == CreatureFaction.Enemy)
            {
                enemyAIHandler.IsNotified();
            }
            else if (this.turnPhase == CreatureFaction.Npc)
            {
                npcAIHandler.IsNotified();
            }
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
            if (creature== null)
            {
                throw new ArgumentNullException("creature");
            }

            FDCreature target = this.GetCreatureAt(targetPosition);
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            FightInformation fighting = DamageFormula.DealWithAttack(creature, target, gameField, true);

            BattleFightPack fightPack = new BattleFightPack(creature, target, fighting);
            gameCallback.OnHandlePack(fightPack);

            // Remove dead creature
            if (target.Data.Hp <= 0)
            {
                this.DisposeCreature(target.CreatureId, true, true);
            }
            if (creature.Data.Hp <= 0)
            {
                this.DisposeCreature(creature.CreatureId, true, true);
            }

            if (creature.Faction == CreatureFaction.Friend)
            {
                // Talk about experience
                FDMessage mId = FDMessage.Create(FDMessage.MessageTypes.Information, 5, 33);
                TalkPack talk = new TalkPack(creature, mId);
                gameCallback.OnHandlePack(talk);
            }

            PostCreatureAction(creature);
        }

        public void DoCreatureSpellMagic(int creatureId, int magicId, FDPosition targetPosition, FDPosition transportPosition = null)
        {
            FDCreature creature = this.GetCreature(creatureId);
            PostCreatureAction(creature);
        }

        public void DoCreatureUseItem(int creatureId, int itemIndex, int targetCreatureId)
        {
            FDCreature creature = this.GetCreature(creatureId);
            FDCreature targetCreature = this.GetCreature(targetCreatureId);
            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemIndex);

            creature.Data.RemoveItemAt(itemIndex);

            PostCreatureAction(creature);
        }

        public void DoCreatureExchangeItem(int creatureId, int itemIndex, int targetCreatureId, int exchangeItemIndex = -1)
        {
            FDCreature creature = this.GetCreature(creatureId);
            FDCreature targetCreature = this.GetCreature(targetCreatureId);

            int itemId = creature.Data.GetItemAt(itemIndex);
            creature.Data.RemoveItemAt(itemIndex);
            
            if (exchangeItemIndex >= 0)
            {
                int exchangeItemId = targetCreature.Data.GetItemAt(exchangeItemIndex);
                targetCreature.Data.RemoveItemAt(exchangeItemIndex);
                creature.Data.AddItem(exchangeItemId);
            }

            targetCreature.Data.AddItem(itemId);

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
                creature.Data.Hp = CreatureFormula.GetRestRecoveredHp(creature.Data.Hp, creature.Data.HpMax);
                CreatureAnimationPack pack = new CreatureAnimationPack(creature, CreatureAnimationPack.AnimationType.Rest);
                gameCallback.OnHandlePack(pack);
            }

            PostCreatureAction(creature);
        }

        /// <summary>
        /// To pick up the treature at the current position.
        /// </summary>
        /// <param name="creatureId"></param>
        /// <param name="itemIndex"></param>
        public void PickTreasure(FDPosition position)
        {

        }

        /// <summary>
        /// To update the treasure box
        /// </summary>
        /// <param name="position"></param>
        /// <param name="itemId"></param>
        public void UpdateTreasure(FDPosition position, int itemId)
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
                    creature.Data.Hp = CreatureFormula.GetRestRecoveredHp(creature.Data.Hp, creature.Data.HpMax);
                    CreatureAnimationPack pack = new CreatureAnimationPack(creature, CreatureAnimationPack.AnimationType.Rest);
                    gameCallback.OnHandlePack(pack);
                }

                // Set creature status
                creature.HasActioned = true;
                gameCallback.OnHandlePack(new CreatureRefreshPack(creature));
            }

            // Do startNewTurn
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

            if (walkAction.MovePath?.Desitination != null)
            {
                creature.SetMoveTo(walkAction.MovePath.Desitination);
            }

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
            gameCallback.OnHandlePack(new CreatureRefreshPack(creature));

            // Check all creatures are taken actions, and do startNewTurn

            List<FDCreature> creatureList = null;
            if (this.turnPhase == CreatureFaction.Friend)
            {
                creatureList = this.Friends;
            }
            else if (this.turnPhase == CreatureFaction.Enemy)
            {
                creatureList = this.Enemies;
            }
            else if (this.turnPhase == CreatureFaction.Npc)
            {
                creatureList = this.Npcs;
            }

            bool hasAllActioned = true;
            foreach(FDCreature c in creatureList)
            {
                if (!c.HasActioned)
                {
                    hasAllActioned = false;
                    break;
                }
            }

            if (hasAllActioned)
            {
                StartNewTurnPhase();
            }
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
                    if(this.Npcs.Count > 0)
                    {
                        turnPhase = CreatureFaction.Npc;
                    }
                    else
                    {
                        turnPhase = CreatureFaction.Enemy;
                    }
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

            // Update UICreature on UI
            gameCallback.OnHandlePack(new CreatureRefreshAllPack());

            // AI Call

            // Show turn icon
            if (this.turnPhase == CreatureFaction.Friend)
            {
            
            }
        }

        #endregion

    }
}