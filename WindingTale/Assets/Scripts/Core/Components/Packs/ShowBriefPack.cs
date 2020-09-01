using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components.Events.Conditions;

namespace WindingTale.Core.Components.Packs
{
    public class ShowBriefPack : PackBase
    {
        public List<EventCondition> WinConditions
        {
            get; private set;
        }

        public List<EventCondition> LossConditions
        {
            get; private set;
        }

        public int TurnNumber
        {
            get; private set;
        }

        public int ChapterId
        {
            get; private set;
        }

        public ShowBriefPack()
        {

        }
    }
}