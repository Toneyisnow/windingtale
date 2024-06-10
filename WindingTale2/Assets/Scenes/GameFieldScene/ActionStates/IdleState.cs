using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.UI.Dialogs;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    public class IdleState : IActionState
    {
        public IdleState(GameMain gameMain): base(gameMain) { }

        public override void onEnter()
        {
            // Nothing
        }

        public override void onExit()
        {
            // Nothing
        }

        public override IActionState onSelectedPosition(FDPosition position)
        {
            FDCreature creature = this.fdMap.GetCreatureAt(position);

            if (creature != null )
            {
                if (creature.Faction == CreatureFaction.Friend)
                {
                    return new ShowMoveRangeState(gameMain, creature);
                }
                else 
                {
                    // Show creature info
                    gameMain.gameCanvas.ShowDialog(creature, CreatureInfoType.View, (int selected) =>
                    {
                        gameMain.gameCanvas.CloseDialog();
                    });
                    return this;
                }
            }

            return new MenuSystemState(gameMain, position);
        }

        public override IActionState onUserCancelled()
        {
            // Nothing to do here
            return this;
        }
    }
}