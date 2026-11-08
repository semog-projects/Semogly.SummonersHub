namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Atributo de runa (principal, inata ou sub). Os valores seguem o jogo; 7 não existe.</summary>
public enum RuneStat
{
    HpFlat = 1,
    HpPercent = 2,
    AttackFlat = 3,
    AttackPercent = 4,
    DefenseFlat = 5,
    DefensePercent = 6,
    Speed = 8,
    CriticalRate = 9,
    CriticalDamage = 10,
    Resistance = 11,
    Accuracy = 12,
}

public static class RuneStatExtensions
{
    /// <summary>Atributos que aceitam grind. Os demais só podem ser trocados por gem.</summary>
    public static bool IsGrindable(this RuneStat stat) => stat is
        RuneStat.HpFlat or RuneStat.HpPercent or
        RuneStat.AttackFlat or RuneStat.AttackPercent or
        RuneStat.DefenseFlat or RuneStat.DefensePercent or
        RuneStat.Speed;
}
