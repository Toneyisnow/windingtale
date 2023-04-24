using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Common
{
    public class FDRange
    {
        public FDPosition CentralPosition
        {
            get; protected set;
        }

        protected List<FDPosition> positions = null;
        public virtual List<FDPosition> Positions
        {
            get
            {
                return this.positions;
            }
        }

        public FDRange(FDPosition pos)
        {
            this.CentralPosition = pos;
        }

        public virtual void AddPosition(FDPosition position)
        {
            if (this.positions == null)
            {
                this.positions = new List<FDPosition>();
            }

            if (!Contains(position))
            {
                this.positions.Add(position);
            }
        }

        public virtual bool Contains(FDPosition position)
        {
            return this.positions.Contains(position);
        }
    }
}