using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.Core.Definitions
{
    /// <summary>
    /// The type of the mgaic
    /// </summary>
    public enum MagicType
    {
        Attack = 1,
        Recover = 2,
        Offensive = 3,
        Defensive = 4,
        Transmit = 5,
    }

    public class MagicDefinition
    {
        public int MagicId
        {
            get; set;
        }

        public int MpCost
        {
            get; set;
        }

        public MagicType Type
        {
            get; set;
        }

        public FDSpan Span
        {
            get; private set;
        }

        public int ApInvoledRate
        {
            get; private set;
        }

        public int HittingRate
        {
            get; private set;
        }

        public int EffectScope
        {
            get; private set;
        }

        public int EffectRange
        {
            get; private set;
        }

        public bool AllowAfterMove
        {
            get; private set;
        }

        public int AiConsiderRate
        {
            get; private set;
        }

        /// <summary>
        /// This is special one for the 光子炮
        /// </summary>
        public bool IsCross
        {
            get; private set;
        }



        public MagicDefinition()
        {

        }

        public static MagicDefinition ReadFromFile(ResourceDataFile dataFile)
        {
            MagicDefinition def = new MagicDefinition();

            def.MagicId = dataFile.ReadInt();
            def.Type = (MagicType)dataFile.ReadInt();

            int min = dataFile.ReadInt();
            int max = dataFile.ReadInt();

            def.Span = new FDSpan(min, max);

            if (def.Type == MagicType.Attack)
            {
                def.ApInvoledRate = dataFile.ReadInt();
            }

            def.HittingRate = dataFile.ReadInt();
            def.EffectScope = dataFile.ReadInt();
            def.EffectRange = dataFile.ReadInt();
            
            def.MpCost = dataFile.ReadInt();
            def.AllowAfterMove = dataFile.ReadBoolean();
            def.AiConsiderRate = dataFile.ReadInt();
            def.IsCross = false;

            if (def.EffectScope < 0)
            {
                def.EffectScope = -def.EffectScope;
                def.IsCross = true;
            }

            return def;
        }

    }
}