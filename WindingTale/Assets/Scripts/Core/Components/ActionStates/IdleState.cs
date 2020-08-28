using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.ActionStates
{
    public class IdleState : ActionState
    {
        public IdleState(IGameAction action) : base(action)
        {

        }

        public override void OnEnter()
        {
            // Nothing
            base.OnEnter();
        }

        public override void OnExit()
        {
            // Nothing
            base.OnExit();
        }

        public override StateOperationResult OnSelectPosition(FDPosition position)
        {

            FDCreature creature = gameAction.GetCreatureAt(position);
            if (creature == null)
            {
                // Empty space, show system menu
                MenuSystemState state = new MenuSystemState(gameAction, position);
                return new StateOperationResult(StateOperationResult.ResultType.Push, state);
            }
            else if (creature.IsActionable())
            {
                // Actionable friend
                ShowMoveRangeState nextState = new ShowMoveRangeState(gameAction, creature);
                return new StateOperationResult(StateOperationResult.ResultType.Push, nextState);
            }
            else
            {
                // Show creature information
                ShowCreatureInfoPack pack = new ShowCreatureInfoPack(creature.Data.Clone(), CreatureInfoType.View);
                var gameCallback = gameAction.GetCallback();
                gameCallback.OnCallback(pack);

                return new StateOperationResult(StateOperationResult.ResultType.None);
            }
        }

        public override StateOperationResult OnSelectIndex(int index)
        {
            return StateOperationResult.None();
        }

    }
}