using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// The thief: it makes for one particular chest, takes what is in it, and then runs for
    /// its escape point. If somebody else opens the chest first it skips straight to running.
    ///
    /// Both positions are carried on the creature (FDAICreature.TreasurePosition /
    /// EscapePosition), set by the chapter script via ChapterEvents.SetCreatureAiTreasure.
    /// </summary>
    public class AITreasureDelegate : AIDelegate
    {
        public AITreasureDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            FDAICreature aiCreature = this.creature as FDAICreature;
            FDPosition treasurePosition = aiCreature?.TreasurePosition;
            FDPosition escapePosition = aiCreature?.EscapePosition;

            FDTreasure treasure = treasurePosition != null ? gameField.GetTreasureAt(treasurePosition) : null;

            FDPosition headingPosition = (treasure != null && !treasure.HasOpened)
                ? treasurePosition
                : escapePosition;

            if (headingPosition == null)
            {
                // Chest already emptied and nowhere to run to.
                Debug.LogWarning("AITreasureDelegate: creature " + creature.Id + " has nowhere to head for.");
                this.EndTurn();
                return;
            }

            this.MoveToward(headingPosition);

            gameMain.PushActivity((game) => this.PickUpTreasureIfArrived());
        }

        /// <summary>
        /// Takes the chest the creature is standing on, if the walk got it there. Run after
        /// the walk so it sees where the creature actually ended up.
        /// </summary>
        private void PickUpTreasureIfArrived()
        {
            FDTreasure treasure = gameField.GetTreasureAt(this.creature.Position);

            if (treasure != null && !treasure.HasOpened)
            {
                treasure.Open();

                if (treasure.ItemId > 0 && !this.creature.IsItemsFull())
                {
                    // Whatever it carries off is dropped again when the party kills it.
                    this.creature.AddItem(treasure.ItemId);
                }

                Debug.Log(string.Format("AI: creature={0} picked up treasure at {1}",
                    creature.Id, creature.Position));
            }

            this.EndTurn();
        }

    }
}
