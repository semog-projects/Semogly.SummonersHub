using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Eficiência de runa no mesmo modelo do SW Exporter (referência da comunidade): quanto a runa tem de
/// atributos em relação a uma runa 6★ perfeita.
/// </summary>
/// <remarks>
/// Razão = principal (valor em +15 nas estrelas da runa ÷ valor em +15 de uma 6★) + Σ subs e inata
/// (valor com grind ÷ máximo da sub em +15, ou seja, 5 rolls máximos). Uma runa perfeita soma 2,8:
/// principal 1 + 9 rolls máximos de 0,2 (4 subs iniciais, 4 upgrades e a inata). Eficiência = razão ÷ 2,8.
/// A máxima supõe que cada upgrade que falta (+3, +6, +9 e +12) caia com o roll máximo.
/// </remarks>
public static class RuneEfficiency
{
    /// <summary>Razão de uma runa 6★ perfeita.</summary>
    private const decimal PerfectRatio = 2.8m;

    /// <summary>Peso de um roll máximo na razão (1 ÷ 5 rolls).</summary>
    private const decimal MaxRollRatio = 0.2m;

    /// <summary>Níveis em que a runa ganha uma sub nova ou um upgrade de sub.</summary>
    private static readonly int[] UpgradeLevels = [3, 6, 9, 12];

    // Valor do principal em +15, por estrelas (índice 0 = 1★)
    private static readonly Dictionary<RuneStat, int[]> MainStatMax = new()
    {
        [RuneStat.HpFlat] = [804, 1092, 1380, 1704, 2088, 2448],
        [RuneStat.HpPercent] = [18, 20, 38, 43, 51, 63],
        [RuneStat.AttackFlat] = [54, 74, 93, 113, 135, 160],
        [RuneStat.AttackPercent] = [18, 20, 38, 43, 51, 63],
        [RuneStat.DefenseFlat] = [54, 74, 93, 113, 135, 160],
        [RuneStat.DefensePercent] = [18, 20, 38, 43, 51, 63],
        [RuneStat.Speed] = [18, 19, 25, 30, 39, 42],
        [RuneStat.CriticalRate] = [18, 20, 37, 41, 47, 58],
        [RuneStat.CriticalDamage] = [20, 37, 43, 58, 65, 80],
        [RuneStat.Resistance] = [18, 20, 38, 44, 51, 64],
        [RuneStat.Accuracy] = [18, 20, 38, 44, 51, 64],
    };

    // Máximo de uma sub de runa 6★ em +15 (5 rolls máximos)
    private static readonly Dictionary<RuneStat, int> SubstatMax = new()
    {
        [RuneStat.HpFlat] = 1875,
        [RuneStat.HpPercent] = 40,
        [RuneStat.AttackFlat] = 100,
        [RuneStat.AttackPercent] = 40,
        [RuneStat.DefenseFlat] = 100,
        [RuneStat.DefensePercent] = 40,
        [RuneStat.Speed] = 30,
        [RuneStat.CriticalRate] = 30,
        [RuneStat.CriticalDamage] = 35,
        [RuneStat.Resistance] = 40,
        [RuneStat.Accuracy] = 40,
    };

    public static RuneEfficiencyValue Calculate(Rune rune)
    {
        var ratio = MainStatRatio(rune);

        if (rune.InnateStat is { } innate)
        {
            ratio += (decimal)innate.Value / SubstatMax[innate.Stat];
        }

        foreach (var sub in rune.Substats)
        {
            ratio += (decimal)(sub.Value + sub.GrindValue) / SubstatMax[sub.Stat];
        }

        var remainingUpgrades = UpgradeLevels.Count(level => level > rune.Level);

        return new RuneEfficiencyValue(
            ToPercent(ratio),
            ToPercent(ratio + (remainingUpgrades * MaxRollRatio)));
    }

    private static decimal MainStatRatio(Rune rune)
    {
        var table = MainStatMax[rune.MainStat.Stat];
        return (decimal)table[rune.Stars - 1] / table[^1];
    }

    private static decimal ToPercent(decimal ratio) => Math.Round(ratio / PerfectRatio * 100, 2);
}

/// <param name="Current">Eficiência atual, em %.</param>
/// <param name="Max">Eficiência se os upgrades que faltam até o +12 caírem com o roll máximo, em %.</param>
public sealed record RuneEfficiencyValue(decimal Current, decimal Max);
