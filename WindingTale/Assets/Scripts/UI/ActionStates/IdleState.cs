using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.ActionStates
{
    public class IdleState : ActionState
    {
        public IdleState(GameMain gamgMain) : base(gamgMain)
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

        public override StateResult OnSelectPosition(FDPosition position)
        {
            FDCreature creature = gameMain.GameMap.GetCreatureAt(position);
            if (creature == null)
            {
                // Empty space, show system menu
                MenuSystemState state = new MenuSystemState(gameMain, position);
                return StateResult.Push(state);
            }
            else if (creature.IsActionable() && creature.Faction == CreatureFaction.Friend)
            {
                // Actionable friend
                ShowMoveRangeState nextState = new ShowMoveRangeState(gameMain, creature);
                return StateResult.Push(nextState);
            }
            else
            {
                // Show creature information
                CreatureShowInfoActivity showInfo = new CreatureShowInfoActivity(gameMain, creature, CreatureInfoType.View);

                return null;
            }
        }

        public override StateResult OnSelectCallback(int index)
        {
            return null;
        }

    }
}