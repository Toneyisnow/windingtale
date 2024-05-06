namespace WindingTale.Core.Common
{
    /// <summary>
    /// The effect that could apply to a creature from magic or item
    /// </summary>
    public enum EffectType
    {
        EnhancedAp = 1,
        EnhancedDp = 2,
        EnhancedDx = 3,
        Freezing = 4,
        Poison = 5,
        Forbidden = 6,
        StartAction = 7,
        AntiFreeze = 8,
        AntiPoison = 9,
        Multi = 99,
    }

    public enum RecoverType
    {
        Hp = 1,
        Mp = 2,
        HpMax = 3,
        MpMax = 4,
        Ap = 7,
        Dp = 8,
        Mv = 9,
        Dx = 10,
    }
}