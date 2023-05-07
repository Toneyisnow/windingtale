namespace WindingTale.Core.Common
{
    /// <summary>
    /// The effect that could apply to a creature from magic or item
    /// </summary>
    public enum EffectType
    {
        Hp = 1,
        Mp = 2,
        AntiFreeze = 3,
        AntiPoison = 4,
        HpMax = 5,
        MpMax = 6,
        Ap = 7,
        Dp = 8,
        Mv = 9,
        Dx = 10,
        Freezing = 11,
        Poison = 12,
        Forbidden = 13,
        StartAction = 14,
        Multi = 99,
    }
}