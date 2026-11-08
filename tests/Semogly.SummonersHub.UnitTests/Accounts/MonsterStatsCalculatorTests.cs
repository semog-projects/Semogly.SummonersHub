using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class MonsterStatsCalculatorTests
{
    private const long MonsterId = 1;

    // Base de um Lapis 6★ nível 40 (desperto)
    private static readonly Monster Lapis = new(
        MonsterId, 19811, 40, 6, Element.Water,
        new BaseStats(Hp: 9885, Attack: 736, Defense: 582, Speed: 95, Resistance: 15, Accuracy: 0, CriticalRate: 15, CriticalDamage: 50),
        [], false, null);

    [Fact]
    public void WithoutEquipment_ShouldReturnBaseStats()
    {
        var stats = MonsterStatsCalculator.Calculate(Lapis, [], []);

        Assert.Equal(stats.Base, stats.Total);
        Assert.Empty(stats.ActiveSets);
    }

    [Fact]
    public void ShouldSumMainInnateSubstatsAndGrinds()
    {
        Rune[] runes =
        [
            NewRune(1, RuneSet.Revenge, main: (RuneStat.AttackFlat, 160),
                innate: (RuneStat.Speed, 6),
                subs: [Sub(RuneStat.CriticalRate, 9), Sub(RuneStat.Speed, 10, grind: 0), Sub(RuneStat.AttackPercent, 8, grind: 7)]),
            NewRune(2, RuneSet.Will, main: (RuneStat.Speed, 42), subs: [Sub(RuneStat.CriticalDamage, 12)]),
        ];

        var stats = MonsterStatsCalculator.Calculate(Lapis, runes, []);

        Assert.Equal(95 + 6 + 10 + 42, stats.Total.Speed);
        Assert.Equal(15 + 9, stats.Total.CriticalRate);
        Assert.Equal(50 + 12, stats.Total.CriticalDamage);

        // ATK% (8 + grind 7 = 15%) incide sobre a base: ceil(736 × 0,15) = 111
        Assert.Equal(736 + 111 + 160, stats.Total.Attack);
    }

    [Fact]
    public void PercentBonus_ShouldRoundUp()
    {
        Rune[] runes = [NewRune(1, RuneSet.Will, main: (RuneStat.HpPercent, 63))];

        var stats = MonsterStatsCalculator.Calculate(Lapis, runes, []);

        // 9885 × 0,63 = 6227,55 → 6228
        Assert.Equal(9885 + 6228, stats.Total.Hp);
    }

    [Fact]
    public void SwiftSet_ShouldAddPercentOfBaseSpeedRoundedUp()
    {
        var runes = Enumerable.Range(1, 4).Select(i => NewRune(i, RuneSet.Swift, main: (RuneStat.HpFlat, 100))).ToArray();

        var stats = MonsterStatsCalculator.Calculate(Lapis, runes, []);

        // ceil(95 × 0,25) = ceil(23,75) = 24
        Assert.Equal(95 + 24, stats.Total.Speed);
        Assert.Equal([RuneSet.Swift], stats.ActiveSets);
    }

    [Fact]
    public void StatSets_ShouldStackWhenCompletedMoreThanOnce()
    {
        // 6 runas Energy = 3 sets = HP +45%
        var runes = Enumerable.Range(1, 6).Select(i => NewRune(i, RuneSet.Energy, main: (RuneStat.DefenseFlat, 0))).ToArray();

        var stats = MonsterStatsCalculator.Calculate(Lapis, runes, []);

        Assert.Equal(3, stats.ActiveSets.Count);
        Assert.Equal(9885 + (int)Math.Ceiling(9885 * 0.45m), stats.Total.Hp);
    }

    [Theory]
    [InlineData(RuneSet.Blade, 15 + 12, 50, 15, 0)]
    [InlineData(RuneSet.Rage, 15, 50 + 40, 15, 0)]
    [InlineData(RuneSet.Endure, 15, 50, 15 + 20, 0)]
    [InlineData(RuneSet.Focus, 15, 50, 15, 20)]
    public void FlatPercentSets_ShouldAddPoints(RuneSet set, int criticalRate, int criticalDamage, int resistance, int accuracy)
    {
        var runes = Enumerable.Range(1, set.RequiredPieces()).Select(i => NewRune(i, set, main: (RuneStat.HpFlat, 0))).ToArray();

        var stats = MonsterStatsCalculator.Calculate(Lapis, runes, []);

        Assert.Equal(criticalRate, stats.Total.CriticalRate);
        Assert.Equal(criticalDamage, stats.Total.CriticalDamage);
        Assert.Equal(resistance, stats.Total.Resistance);
        Assert.Equal(accuracy, stats.Total.Accuracy);
    }

    [Fact]
    public void IncompleteSet_ShouldNotGiveBonus()
    {
        Rune[] runes = [NewRune(1, RuneSet.Swift, main: (RuneStat.HpFlat, 0)), NewRune(2, RuneSet.Swift, main: (RuneStat.HpFlat, 0))];

        var stats = MonsterStatsCalculator.Calculate(Lapis, runes, []);

        Assert.Empty(stats.ActiveSets);
        Assert.Equal(95, stats.Total.Speed);
    }

    [Fact]
    public void Intangible_ShouldCompleteSetMissingOnePiece()
    {
        Rune[] runes =
        [
            .. Enumerable.Range(1, 3).Select(i => NewRune(i, RuneSet.Swift, main: (RuneStat.HpFlat, 0))),
            NewRune(4, RuneSet.Intangible, main: (RuneStat.HpFlat, 0)),
            NewRune(5, RuneSet.Will, main: (RuneStat.HpFlat, 0)),
            NewRune(6, RuneSet.Will, main: (RuneStat.HpFlat, 0)),
        ];

        Assert.Equal([RuneSet.Swift, RuneSet.Will], MonsterStatsCalculator.Calculate(Lapis, runes, []).ActiveSets);
    }

    [Fact]
    public void ShouldAddArtifactMainStatsOnly()
    {
        Artifact[] artifacts =
        [
            new(10, ArtifactType.Element, Element.Water, null, 5, 5, 15,
                new ArtifactStatValue(ArtifactEffect.AttackFlat, 100),
                [new ArtifactSubstat(ArtifactEffect.AdditionalDamageBySpeed, 31, 0)], MonsterId),
            new(11, ArtifactType.Archetype, null, Archetype.Attack, 5, 5, 15,
                new ArtifactStatValue(ArtifactEffect.HpFlat, 1500), [], MonsterId),
        ];

        var stats = MonsterStatsCalculator.Calculate(Lapis, [], artifacts);

        Assert.Equal(736 + 100, stats.Total.Attack);
        Assert.Equal(9885 + 1500, stats.Total.Hp);
        Assert.Equal(95, stats.Total.Speed);
    }

    [Fact]
    public void ShouldIgnoreEquipmentOfOtherMonsters()
    {
        Rune[] runes = [NewRune(1, RuneSet.Will, main: (RuneStat.Speed, 42), equippedOn: 999)];

        Assert.Equal(95, MonsterStatsCalculator.Calculate(Lapis, runes, []).Total.Speed);
    }

    private static RuneSubstat Sub(RuneStat stat, int value, int grind = 0) => new(stat, value, false, grind);

    private static Rune NewRune(
        long id,
        RuneSet set,
        (RuneStat Stat, int Value) main,
        (RuneStat Stat, int Value)? innate = null,
        RuneSubstat[]? subs = null,
        long equippedOn = MonsterId) =>
        new(
            id,
            slot: (int)(id % 6) + 1,
            stars: 6,
            isAncient: false,
            set,
            level: 15,
            rarity: 5,
            originalRarity: 5,
            new RuneStatValue(main.Stat, main.Value),
            innate is { } i ? new RuneStatValue(i.Stat, i.Value) : null,
            subs ?? [],
            equippedOn);
}
