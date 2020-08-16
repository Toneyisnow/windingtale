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
            return null;
        }
    }
}