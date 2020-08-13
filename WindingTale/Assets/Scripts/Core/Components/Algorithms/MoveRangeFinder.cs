using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Components.Algorithms
{
    public class MoveRangeFinder
    {
        private IGameAction gameAction;


        public MoveRangeFinder(IGameAction gameAction)
        {
            this.gameAction = gameAction;
        }

        public FDMoveRange CalculateMoveRange()
        {
            return null;
        }


    }
}