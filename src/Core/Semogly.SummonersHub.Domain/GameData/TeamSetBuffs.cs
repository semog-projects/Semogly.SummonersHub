using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>
/// Sets de equipe: em batalha, cada set completo de um aliado (ou do próprio monstro) dá o bônus a todo o time.
/// Fora de batalha, como na tela do monstro, não aparecem; por isso o cálculo de atributos não os inclui e o
/// otimizador só os soma quando o usuário informa os sets do time.
/// </summary>
public static class TeamSetBuffs
{
    /// <summary>Bônus do set para os aliados (percentual sobre a base em HP/ATK/DEF; pontos em PRE/RES), ou nulo.</summary>
    public static RuneStatValue? AllyBonus(this RuneSet set) => set switch
    {
        RuneSet.Fight => new(RuneStat.AttackPercent, 8),
        RuneSet.Determination => new(RuneStat.DefensePercent, 8),
        RuneSet.Enhance => new(RuneStat.HpPercent, 8),
        RuneSet.Accuracy => new(RuneStat.Accuracy, 10),
        RuneSet.Tolerance => new(RuneStat.Resistance, 10),
        _ => null,
    };

    public static bool IsTeamSet(this RuneSet set) => set.AllyBonus() is not null;

    /// <summary>Atributos com os bônus dos sets de equipe (um por set informado; repetir soma de novo).</summary>
    public static StatValues Apply(StatValues total, BaseStats @base, IEnumerable<RuneSet> teamSets)
    {
        var result = total;
        foreach (var bonus in teamSets.Select(s => s.AllyBonus()).OfType<RuneStatValue>())
        {
            result = bonus.Stat switch
            {
                RuneStat.AttackPercent => result with { Attack = result.Attack + PercentOf(@base.Attack, bonus.Value) },
                RuneStat.DefensePercent => result with { Defense = result.Defense + PercentOf(@base.Defense, bonus.Value) },
                RuneStat.HpPercent => result with { Hp = result.Hp + PercentOf(@base.Hp, bonus.Value) },
                RuneStat.Accuracy => result with { Accuracy = result.Accuracy + bonus.Value },
                RuneStat.Resistance => result with { Resistance = result.Resistance + bonus.Value },
                _ => result,
            };
        }

        return result;
    }

    // Mesmo arredondamento do cálculo de atributos: percentual da base, para cima
    private static int PercentOf(int baseValue, int percent) => (int)Math.Ceiling(baseValue * percent / 100m);
}
