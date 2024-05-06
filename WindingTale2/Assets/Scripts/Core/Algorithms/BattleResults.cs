using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using WindingTale.Core.Common;

namespace WindingTale.Core.Algorithms
{
    #region Single Result

    public enum SoloResultType
    {
        Damage = 0,
        Recover = 1,
        Effect = 2,
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

    public class RecoverResult : SoloResult
    {
        public RecoverType Type { get; private set; }

        public int Amount { get; private set; }


        public RecoverResult(RecoverType type, int amount) : base(SoloResultType.Recover)
        {
            this.Type = type;
            this.Amount = amount;
        }
    }

    public class EffectResult : SoloResult
    {
        public EffectType Type { get; private set; }

        public int RoundCount { get; private set; }

        public EffectResult(EffectType type, int roundCount) : base(SoloResultType.Effect)
        {
            this.Type = type;
            RoundCount = roundCount;
        }
    }

    public class MultiEffectResult : EffectResult
    {
        public List<EffectResult> Effects { get; private set; }

        public void AddEffect(EffectResult effect)
        {
            this.Effects.Add(effect);
        }

        public MultiEffectResult(List<EffectResult> effects) : base(EffectType.Multi, 0)
        {
            this.Effects = effects;
        }
    }


    #endregion


    #region Battle Result

    public abstract class BattleResult
    {
        public int Experience { get; set; }

        public List<int> GainedItems { get; set; }


        public LevelUpInfo LevelUp { get; set; }

        public BattleResult()
        {
            this.Experience = 0;
            this.GainedItems = new List<int>();
            this.LevelUp = null;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class AttackResult : BattleResult
    {
        public List<DamageResult> Damages;

        public List<DamageResult> BackDamages;

        public AttackResult() : base()
        {
            this.Damages = new List<DamageResult>();
            this.BackDamages = new List<DamageResult>();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MagicResult : BattleResult
    {
        /// <summary>
        /// Results has format of <CreatureId, SoloResult>
        /// </summary>
        public Dictionary<int, SoloResult> Results;

        public MagicResult() : base()
        {
            this.Results = new Dictionary<int, SoloResult>();
        }
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

        public LevelUpInfo()
        {
            this.ImprovedHp = 0;
            this.ImprovedMp = 0;
            this.ImprovedAp = 0;
            this.ImprovedDp = 0;
            this.ImprovedDx = 0;
            this.LearntMagicId = 0;
        }
    }
    #endregion
}
