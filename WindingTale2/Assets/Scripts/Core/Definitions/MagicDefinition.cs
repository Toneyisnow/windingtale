using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Files;

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

        public string Name { get; set; }

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

        public int ApInvolvedRate
        {
            get; private set;
        }

        public int HittingRate
        {
            get; private set;
        }

        /// <summary>
        /// Scope is the impacted area for the magic
        /// </summary>
        public int EffectScope
        {
            get; private set;
        }

        /// <summary>
        /// Range is the area that allows to spell the magic
        /// </summary>
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
                def.ApInvolvedRate = dataFile.ReadInt();
            }

            def.HittingRate = dataFile.ReadInt();
            def.EffectRange = dataFile.ReadInt();
            def.EffectScope = dataFile.ReadInt();

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

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public EffectResult GenerateEffect()
        {
            int value = 0;
            switch (MagicId)
            {
                case 301:
                    value = FDRandom.IntFromSpan(2, 4);
                    return new EffectResult(EffectType.Forbidden, value);
                case 302:
                    value = FDRandom.IntFromSpan(3, 5);
                    return new EffectResult(EffectType.Poison, value);
                case 303:
                    value = FDRandom.IntFromSpan(2, 4);
                    return new EffectResult(EffectType.Freezing, value);
                case 401:
                    return new EffectResult(EffectType.EnhancedAp, 5);
                case 402:
                    return new EffectResult(EffectType.EnhancedDp, 5);
                case 403:
                    return new EffectResult(EffectType.EnhancedDx, 5);
                case 404:
                    return new EffectResult(EffectType.AntiPoison, 0);
                case 405:
                    return new EffectResult(EffectType.AntiFreeze, 0);
                case 406:
                    return new EffectResult(EffectType.StartAction, 0);
                case 407:
                    return new MultiEffectResult(new List<EffectResult>() {
                        new EffectResult(EffectType.EnhancedAp, 5),
                        new EffectResult(EffectType.EnhancedDp, 5),
                        new EffectResult(EffectType.EnhancedDx, 5),
                    });
                case 408:
                    return null;
                default:
                    return null;
            }
        }

        public int GetBaseExperience()
        {
            switch (this.MagicId)
            {
                case 401:
                case 402:
                case 403:
                    return 4;
                case 406:
                    return 8;
                case 301:
                case 302:
                case 303:
                case 404:
                case 405:
                    return 4;
                case 407:
                case 408:
                    return 3;
                case 501:
                    return 10;
                default:
                    break;
            }
            return 0;
        }
    }
}