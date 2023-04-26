using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using WindingTale.Common;

namespace WindingTale.Core.Components.Algorithms
{
    #region Single Result

    public enum SoloResultType
    {
        Damage = 0,
        Effect = 1,
    }

    public abstract class SoloResult
    {

        public SoloResultType ResultType { get; private set; }
        public SoloResult(SoloResultType type)
        { 
            this.ResultType = type;
        }

    }

    public class DamageResult : SoloResult
    {
        public int HpBefore
        {
            get; private set;
        }

        public int HpAfter
        {
            get; private set;
        }

        public bool HasMissed
        {
            get
            {
                return this.HpBefore == this.HpAfter;
            }
        }

        public bool IsCritical
        {
            get; private set;
        }

        public DamageResult(int before, int after, bool critical) : base(SoloResultType.Damage)
        {
            this.HpBefore = before;
            this.HpAfter = after;
            this.IsCritical = critical;
        }

    }

    public class EffectResult : SoloResult
    {
        public int Amount { get; private set; }


        public EffectResult(int amount) : base(SoloResultType.Effect)
        {
            this.Amount = amount;
        }
    }

    #endregion


    #region Battle Result

    public abstract class BattleResult
    {
        public int Experience { get; protected set; }

        public List<int> GainedItems { get; protected set; }


        public LevelUpInfo LevelUp { get; protected set; }

    }

    /// <summary>
    /// 
    /// </summary>
    public class AttackResult : BattleResult
    {
        public List<DamageResult> Damages;

        public List<DamageResult> BackDamages;

    }

    /// <summary>
    /// 
    /// </summary>
    public class MagicResult : BattleResult
    {
        /// <summary>
        /// Results has format of <CreatureId, SoloResult>
        /// </summary>
        public Dictionary<string, SoloResult> Results;
    }

    public class ItemResult
    {
        /// <summary>
        /// Results has format of <CreatureId, SoloResult>
        /// </summary>
        public Dictionary<string, SoloResult> Results;
    }

    public class LevelUpInfo
    {
        public int ImprovedHp
        {
            get; set;
        }

        public int ImprovedMp
        {
            get; set;
        }

        public int ImprovedAp
        {
            get; set;
        }

        public int ImprovedDp
        {
            get; set;
        }

        public int ImprovedDx
        {
            get; set;
        }

        public int LearntMagicId
        {
            get; set;
        }


    }
    #endregion
}
