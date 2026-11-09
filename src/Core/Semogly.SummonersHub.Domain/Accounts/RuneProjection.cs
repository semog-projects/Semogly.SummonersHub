using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Estimativa de uma runa levada a +15: principal no valor máximo e subs com o ganho médio esperado
/// dos upgrades que faltam.
/// </summary>
/// <remarks>
/// A runa ganha um roll em +3, +6, +9 e +12. Enquanto tem menos de 4 subs, cada roll cria uma sub nova, de
/// atributo imprevisível, e esses rolls são ignorados. Os demais caem numa das 4 subs ao acaso, então cada sub
/// recebe, em média, ¼ dos rolls restantes, com o valor médio do roll do atributo. O resultado é arredondado,
/// porque os atributos da runa são inteiros.
/// </remarks>
public static class RuneProjection
{
    private const int MaxSubstats = 4;

    private static readonly int[] UpgradeLevels = [3, 6, 9, 12];

    // Faixa de um roll de sub em runa 6★ (o máximo bate com o SubstatMax de RuneEfficiency ÷ 5)
    private static readonly Dictionary<RuneStat, ValueRange> SubstatRoll = new()
    {
        [RuneStat.HpFlat] = new(135, 375),
        [RuneStat.HpPercent] = new(5, 8),
        [RuneStat.AttackFlat] = new(10, 20),
        [RuneStat.AttackPercent] = new(5, 8),
        [RuneStat.DefenseFlat] = new(10, 20),
        [RuneStat.DefensePercent] = new(5, 8),
        [RuneStat.Speed] = new(4, 6),
        [RuneStat.CriticalRate] = new(4, 6),
        [RuneStat.CriticalDamage] = new(4, 7),
        [RuneStat.Resistance] = new(4, 8),
        [RuneStat.Accuracy] = new(4, 8),
    };

    /// <summary>Roll médio de uma sub de runa 6★.</summary>
    public static decimal AverageRoll(RuneStat stat) => (SubstatRoll[stat].Min + SubstatRoll[stat].Max) / 2m;

    /// <summary>
    /// Cópia da runa com o grind lendário máximo em cada sub grindável (quando maior que o grind atual),
    /// para simular a runa depois de usar os grinds.
    /// </summary>
    public static Rune WithMaxGrinds(Rune rune)
    {
        var substats = rune.Substats
            .Select(sub => CraftItemRanges.For(CraftItemType.Grindstone, sub.Stat, rarity: 5) is { } range && range.Max > sub.GrindValue
                ? sub with { GrindValue = range.Max }
                : sub)
            .ToList();

        return new Rune(
            rune.GameId, rune.Slot, rune.Stars, rune.IsAncient, rune.Set, rune.Level, rune.Rarity, rune.OriginalRarity,
            rune.MainStat, rune.InnateStat, substats, rune.EquippedMonsterId);
    }

    /// <summary>
    /// Trocas possíveis com uma gem lendária (valor máximo da faixa): cada sub trocável por cada atributo que a
    /// runa ainda não tem. Se a runa já tem uma sub gemada, só ela pode ser trocada de novo (regra do jogo). As
    /// subs não repetem o principal nem a inata, o slot 1 não tem DEF e o slot 3 não tem ATK.
    /// </summary>
    public static IEnumerable<SimulatedGem> GemOptions(Rune rune)
    {
        var gemmed = rune.Substats.FirstOrDefault(s => s.IsGemmed);
        var replaceable = gemmed is null ? rune.Substats : [gemmed];
        var taken = rune.Substats.Select(s => s.Stat)
            .Append(rune.MainStat.Stat)
            .Concat(rune.InnateStat is { } innate ? [innate.Stat] : [])
            .ToHashSet();

        foreach (var sub in replaceable)
        {
            foreach (var stat in Enum.GetValues<RuneStat>().Where(s => !taken.Contains(s) && AllowedAsSubstat(s, rune.Slot)))
            {
                if (CraftItemRanges.For(CraftItemType.EnchantedGem, stat, rarity: 5) is { } range)
                {
                    yield return new SimulatedGem(rune.GameId, sub.Stat, stat, range.Max);
                }
            }
        }
    }

    /// <summary>Cópia da runa com a sub <see cref="SimulatedGem.From"/> trocada pela gem (sem grind: a gem zera o grind).</summary>
    public static Rune WithGem(Rune rune, SimulatedGem gem)
    {
        var substats = rune.Substats
            .Select(sub => sub.Stat == gem.From ? new RuneSubstat(gem.To, gem.Value, IsGemmed: true, GrindValue: 0) : sub)
            .ToList();

        return new Rune(
            rune.GameId, rune.Slot, rune.Stars, rune.IsAncient, rune.Set, rune.Level, rune.Rarity, rune.OriginalRarity,
            rune.MainStat, rune.InnateStat, substats, rune.EquippedMonsterId);
    }

    private static bool AllowedAsSubstat(RuneStat stat, int slot) => slot switch
    {
        1 => stat is not (RuneStat.DefenseFlat or RuneStat.DefensePercent),
        3 => stat is not (RuneStat.AttackFlat or RuneStat.AttackPercent),
        _ => true,
    };

    /// <summary>Cópia da runa em +15 (mesmo id e mesmo monstro), com o principal e as subs projetados.</summary>
    public static Rune ToMaxLevel(Rune rune, long? equippedMonsterId)
    {
        var remaining = UpgradeLevels.Count(level => level > rune.Level);
        var newSubstats = Math.Max(0, MaxSubstats - rune.Substats.Count);
        var upgradesToExisting = Math.Max(0, remaining - newSubstats);

        var substats = rune.Substats.Count == 0 || upgradesToExisting == 0
            ? rune.Substats
            : [.. rune.Substats.Select(sub => sub with
            {
                Value = sub.Value + (int)Math.Round(upgradesToExisting * AverageRoll(sub.Stat) / rune.Substats.Count, MidpointRounding.AwayFromZero),
            })];

        return new Rune(
            rune.GameId,
            rune.Slot,
            rune.Stars,
            rune.IsAncient,
            rune.Set,
            Rune.MaxLevel,
            rune.Rarity,
            rune.OriginalRarity,
            rune.MainStat with { Value = RuneEfficiency.MainStatAtMaxLevel(rune.MainStat.Stat, rune.Stars) },
            rune.InnateStat,
            substats,
            equippedMonsterId);
    }
}

/// <summary>Gem simulada numa runa: a sub <paramref name="From"/> vira <paramref name="To"/> com <paramref name="Value"/>.</summary>
public sealed record SimulatedGem(long RuneId, RuneStat From, RuneStat To, int Value);
