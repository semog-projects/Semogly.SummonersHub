using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Onde aplicar um grind ou gem: runas compatíveis e quanto a eficiência sobe no pior e no melhor sorteio.
/// </summary>
/// <remarks>
/// Regras do jogo consideradas:
/// <list type="bullet">
/// <item>O item vale para runas do mesmo set; os immemorial valem para qualquer set.</item>
/// <item>Grind: só em sub (não na inata) de atributo grindável. O grind novo substitui o anterior se for maior,
/// então o ganho é o sorteio menos o grind atual.</item>
/// <item>Gem: uma por runa. Troca uma sub por um atributo que a runa ainda não tem (nem como principal ou inata)
/// e que o slot aceita; a sub trocada perde o grind.</item>
/// </list>
/// </remarks>
public static class CraftSuggestions
{
    public static IReadOnlyList<CraftSuggestion> For(CraftItemType type, CraftItemCode code, IEnumerable<Rune> runes)
    {
        if (CraftItemRanges.For(type, code.Stat, code.Rarity) is not { } range)
        {
            return [];
        }

        var compatible = runes.Where(r => code.Set == RuneSet.Immemorial || r.Set == code.Set);

        return type.IsGem()
            ? [.. compatible.Select(r => BestGem(r, code.Stat, range)).OfType<CraftSuggestion>()]
            : [.. compatible.Select(r => Grind(r, code.Stat, range)).OfType<CraftSuggestion>()];
    }

    private static CraftSuggestion? Grind(Rune rune, RuneStat stat, ValueRange range)
    {
        if (!stat.IsGrindable())
        {
            return null;
        }

        var index = rune.Substats.FindIndex(s => s.Stat == stat);
        if (index < 0)
        {
            return null;
        }

        var sub = rune.Substats[index];

        // Grind sorteado abaixo do atual não muda nada
        var minGain = Math.Max(0, range.Min - sub.GrindValue);
        var maxGain = Math.Max(0, range.Max - sub.GrindValue);
        if (maxGain == 0)
        {
            return null;
        }

        return new CraftSuggestion(
            rune,
            index,
            sub.Stat,
            sub.Value + sub.GrindValue,
            sub.Value + sub.GrindValue + minGain,
            sub.Value + sub.GrindValue + maxGain,
            RuneEfficiency.SubstatPoints(stat, minGain),
            RuneEfficiency.SubstatPoints(stat, maxGain));
    }

    private static CraftSuggestion? BestGem(Rune rune, RuneStat stat, ValueRange range)
    {
        if (rune.Substats.Any(s => s.IsGemmed)
            || rune.MainStat.Stat == stat
            || rune.InnateStat?.Stat == stat
            || !RuneSlotRules.AcceptsSubstat(rune.Slot, stat))
        {
            return null;
        }

        // A runa não repete atributo: se já tem a sub, só pode trocar essa mesma sub
        var existing = rune.Substats.FindIndex(s => s.Stat == stat);
        IEnumerable<int> candidates = existing >= 0 ? [existing] : Enumerable.Range(0, rune.Substats.Count);

        return candidates
            .Select(index =>
            {
                var sub = rune.Substats[index];
                var current = sub.Value + sub.GrindValue;
                var lost = RuneEfficiency.SubstatPoints(sub.Stat, current);
                return new CraftSuggestion(
                    rune,
                    index,
                    sub.Stat,
                    current,
                    range.Min,
                    range.Max,
                    RuneEfficiency.SubstatPoints(stat, range.Min) - lost,
                    RuneEfficiency.SubstatPoints(stat, range.Max) - lost);
            })
            .Where(s => s.MaxEfficiencyGain > 0)
            .OrderByDescending(s => s.MaxEfficiencyGain)
            .FirstOrDefault();
    }
}

/// <summary>Restrições de sub por slot.</summary>
public static class RuneSlotRules
{
    /// <summary>Slot 1 (ATK fixo) não tem subs de DEF; slot 3 (DEF fixo) não tem subs de ATK.</summary>
    public static bool AcceptsSubstat(int slot, RuneStat stat) => (slot, stat) switch
    {
        (1, RuneStat.DefenseFlat or RuneStat.DefensePercent) => false,
        (3, RuneStat.AttackFlat or RuneStat.AttackPercent) => false,
        _ => true,
    };
}

/// <param name="SubstatIndex">Sub que recebe o grind ou é trocada pela gem.</param>
/// <param name="TargetStat">Atributo dessa sub antes da aplicação.</param>
/// <param name="CurrentValue">Valor atual da sub, com grind.</param>
/// <param name="MinValue">Valor depois, no pior sorteio (gem: valor da sub nova).</param>
/// <param name="MaxValue">Valor depois, no melhor sorteio.</param>
/// <param name="MinEfficiencyGain">Pontos de eficiência ganhos no pior sorteio (pode ser negativo na gem).</param>
/// <param name="MaxEfficiencyGain">Pontos de eficiência ganhos no melhor sorteio.</param>
public sealed record CraftSuggestion(
    Rune Rune,
    int SubstatIndex,
    RuneStat TargetStat,
    int CurrentValue,
    int MinValue,
    int MaxValue,
    decimal MinEfficiencyGain,
    decimal MaxEfficiencyGain);
