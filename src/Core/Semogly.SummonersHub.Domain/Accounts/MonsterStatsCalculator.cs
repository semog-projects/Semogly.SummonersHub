using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Calcula os atributos totais de um monstro como aparecem na tela dele no jogo: base + runas
/// (principal, inata e subs com grind) + bônus de sets completos + atributo principal dos artefatos.
/// </summary>
/// <remarks>
/// Fora do cálculo, como no jogo fora de batalha: líder, torres/flags, buffs de sets de equipe
/// (Fight, Determination, Enhance, Accuracy, Tolerance), subs de artefato (condicionais) e
/// relíquias. Bônus percentuais incidem sobre o atributo base e são arredondados para cima.
/// </remarks>
public static class MonsterStatsCalculator
{
    public static MonsterStats Calculate(Monster monster, IEnumerable<Rune> runes, IEnumerable<Artifact> artifacts)
    {
        var equippedRunes = runes.Where(r => r.EquippedMonsterId == monster.GameId).ToList();
        var equippedArtifacts = artifacts.Where(a => a.EquippedMonsterId == monster.GameId).ToList();

        var fromRunes = SumRuneStats(equippedRunes);
        var activeSets = ActiveSets(equippedRunes);

        foreach (var set in activeSets)
        {
            if (set.StatBonus() is { } bonus)
            {
                fromRunes[bonus.Stat] = fromRunes.GetValueOrDefault(bonus.Stat) + bonus.Value;
            }
        }

        var artifactHp = SumArtifactMainStat(equippedArtifacts, ArtifactEffect.HpFlat);
        var artifactAttack = SumArtifactMainStat(equippedArtifacts, ArtifactEffect.AttackFlat);
        var artifactDefense = SumArtifactMainStat(equippedArtifacts, ArtifactEffect.DefenseFlat);

        var b = monster.BaseStats;
        int Get(RuneStat stat) => fromRunes.GetValueOrDefault(stat);

        var total = new StatValues(
            Hp: b.Hp + PercentOf(b.Hp, Get(RuneStat.HpPercent)) + Get(RuneStat.HpFlat) + artifactHp,
            Attack: b.Attack + PercentOf(b.Attack, Get(RuneStat.AttackPercent)) + Get(RuneStat.AttackFlat) + artifactAttack,
            Defense: b.Defense + PercentOf(b.Defense, Get(RuneStat.DefensePercent)) + Get(RuneStat.DefenseFlat) + artifactDefense,
            Speed: b.Speed + Get(RuneStat.Speed) + PercentOf(b.Speed, SwiftBonus(activeSets)),
            CriticalRate: b.CriticalRate + Get(RuneStat.CriticalRate),
            CriticalDamage: b.CriticalDamage + Get(RuneStat.CriticalDamage),
            Resistance: b.Resistance + Get(RuneStat.Resistance),
            Accuracy: b.Accuracy + Get(RuneStat.Accuracy));

        var @base = new StatValues(b.Hp, b.Attack, b.Defense, b.Speed, b.CriticalRate, b.CriticalDamage, b.Resistance, b.Accuracy);

        return new MonsterStats(@base, total, activeSets);
    }

    /// <summary>
    /// Sets completos. Cada conjunto completo conta uma vez (ex.: 4 runas Energy = 2 sets Energy).
    /// Runas Intangible completam um set que falta uma peça, priorizando os de 4 peças.
    /// </summary>
    internal static IReadOnlyList<RuneSet> ActiveSets(IReadOnlyCollection<Rune> runes)
    {
        var counts = runes
            .Where(r => r.Set != RuneSet.Intangible)
            .GroupBy(r => r.Set)
            .ToDictionary(g => g.Key, g => g.Count());

        var intangibles = runes.Count(r => r.Set == RuneSet.Intangible);
        for (var i = 0; i < intangibles; i++)
        {
            var almostComplete = counts
                .Where(c => c.Key.RequiredPieces() > 0 && c.Value % c.Key.RequiredPieces() == c.Key.RequiredPieces() - 1)
                .OrderByDescending(c => c.Key.RequiredPieces())
                .Select(c => (RuneSet?)c.Key)
                .FirstOrDefault();

            if (almostComplete is not { } set)
            {
                break;
            }

            counts[set]++;
        }

        return counts
            .Where(c => c.Key.RequiredPieces() > 0)
            .SelectMany(c => Enumerable.Repeat(c.Key, c.Value / c.Key.RequiredPieces()))
            .OrderBy(s => s)
            .ToList();
    }

    private static Dictionary<RuneStat, int> SumRuneStats(IEnumerable<Rune> runes)
    {
        var totals = new Dictionary<RuneStat, int>();

        void Add(RuneStat stat, int value) => totals[stat] = totals.GetValueOrDefault(stat) + value;

        foreach (var rune in runes)
        {
            Add(rune.MainStat.Stat, rune.MainStat.Value);

            if (rune.InnateStat is { } innate)
            {
                Add(innate.Stat, innate.Value);
            }

            foreach (var sub in rune.Substats)
            {
                Add(sub.Stat, sub.Value + sub.GrindValue);
            }
        }

        return totals;
    }

    // O bônus do Swift é percentual sobre a velocidade base, por isso fica fora da soma de runas
    private static int SwiftBonus(IEnumerable<RuneSet> activeSets) =>
        activeSets.Count(s => s == RuneSet.Swift) * RuneSetExtensions.SwiftSpeedPercent;

    private static int SumArtifactMainStat(IEnumerable<Artifact> artifacts, ArtifactEffect effect) =>
        (int)artifacts.Where(a => a.MainStat.Effect == effect).Sum(a => a.MainStat.Value);

    private static int PercentOf(int baseValue, int percent) =>
        percent == 0 ? 0 : (int)Math.Ceiling(baseValue * percent / 100m);
}

public sealed record StatValues(
    int Hp,
    int Attack,
    int Defense,
    int Speed,
    int CriticalRate,
    int CriticalDamage,
    int Resistance,
    int Accuracy)
{
    /// <summary>
    /// HP efetivo, como a comunidade calcula (SWOP, SWARFARM): HP × (1140 + 3,5 × DEF) ÷ 1000.
    /// Resume quanto dano o monstro aguenta considerando a redução da DEF.
    /// </summary>
    public int EffectiveHp => (int)Math.Round(Hp * (1140 + (3.5m * Defense)) / 1000);
}

/// <param name="ActiveSets">Sets completos, repetidos quando completos mais de uma vez.</param>
public sealed record MonsterStats(StatValues Base, StatValues Total, IReadOnlyList<RuneSet> ActiveSets);
