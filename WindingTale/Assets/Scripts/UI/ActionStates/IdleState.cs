using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Activities;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class IdleState : ActionState
    {
        public IdleState(GameMain gamgMain, IStateResultHandler stateHandler) : base(gamgMain, stateHandler)
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

        public override void OnSelectPosition(FDPosition position)
        {
            FDCreature creature = gameMain.GameMap.GetCreatureAt(position);
            if (creature == null)
            {
                // Empty space, show system menu
                MenuSystemState state = new MenuSystemState(gameMain, stateHandler, position);
                stateHandler.HandlePushState(state);
            }
            else if (creature.IsActionable() && creature.Faction == CreatureFaction.Friend)
            {
                // Actionable friend
                ShowMoveRangeState nextState = new ShowMoveRangeState(gameMain, stateHandler, creature);
                stateHandler.HandlePushState(nextState);
            }
            else
            {
                // Show creature information
                ShowCreatureInfoActivity showInfo = new ShowCreatureInfoActivity(gameMain, creature, CreatureInfoType.View, (int result) => { });
                activityManager.Push(showInfo);
            }
        }


    }
}