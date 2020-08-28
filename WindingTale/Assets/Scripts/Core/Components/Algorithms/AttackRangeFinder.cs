using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;

namespace WindingTale.Core.Components.Algorithms
{
   
    public class AttackRangeFinder
    {
        private IGameAction gameAction = null;

        public AttackRangeFinder(IGameAction action)
        {
            this.gameAction = action;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public FDRange FindRange(FDCreature creature)
        {
            FDPosition pos = creature.Position;

            FDRange range = new FDRange(pos);
            range.AddPosition(FDPosition.At(pos.X + 1, pos.Y));
            range.AddPosition(FDPosition.At(pos.X, pos.Y + 1));
            range.AddPosition(FDPosition.At(pos.X - 1, pos.Y));
            range.AddPosition(FDPosition.At(pos.X, pos.Y - 1));

            return range;
        }
    }
}