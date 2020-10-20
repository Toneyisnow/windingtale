using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components;
using WindingTale.Core.ObjectModels;
using WindingTale.Core.Definitions;

namespace WindingTale.AI.Delegates
{
    public abstract class AIDelegate
    {
        protected IGameAction GameAction
        {
            get; private set;
        }

        protected FDCreature Creature
        {
            get; private set;
        }

        public AIDelegate(IGameAction gameAction, FDCreature c)
        {
            this.GameAction = gameAction;
            this.Creature = c;
        }

        public abstract void TakeAction();

        public virtual void SetParameter(FDPosition pos)
        {

        }

        public bool NeedRecover()
        {
            if (this.Creature.Faction == CreatureFaction.Npc && this.Creature.Data.Hp < this.Creature.Data.HpMax)
            {
                return true;
            }
            
            if(this.Creature.Data.Hp < this.Creature.Data.HpMax / 2)
            {
                return true;
            }

            return false;
        }

        public bool CanRecover()
        {
            return this.getRecoverItem() >= 0;
        }

        public bool NeedAndCanRecover()
        {
            return this.NeedRecover() && this.CanRecover();
        }

        public int getRecoverItem()
        {
            for(int index = 0; index < this.Creature.Data.Items.Count; index++)
            {
                int itemId = this.Creature.Data.Items[index];
                if (itemId == 101 || itemId == 102 || itemId == 103 || itemId == 104 || itemId == 122)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
