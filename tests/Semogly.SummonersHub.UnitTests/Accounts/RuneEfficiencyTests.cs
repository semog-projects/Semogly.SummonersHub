using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class RuneEfficiencyTests
{
    // 4 subs com 1 roll máximo cada, uma delas com mais 4 upgrades máximos: o teto de uma runa 6★ sem inata
    private static readonly RuneSubstat[] PerfectSubs =
    [
        new(RuneStat.HpPercent, 40, false, 0),
        new(RuneStat.CriticalRate, 6, false, 0),
        new(RuneStat.CriticalDamage, 7, false, 0),
        new(RuneStat.AttackPercent, 8, false, 0),
    ];

    [Fact]
    public void PerfectRuneWithInnate_ShouldBe100Percent()
    {
        var rune = NewRune(level: 15, innate: new RuneStatValue(RuneStat.Speed, 6), subs: PerfectSubs);

        Assert.Equal(new RuneEfficiencyValue(100m, 100m), RuneEfficiency.Calculate(rune));
    }

    [Fact]
    public void WithoutInnate_ShouldMissOneRoll()
    {
        var rune = NewRune(level: 15, subs: PerfectSubs);

        // (1 + 1,6) ÷ 2,8
        Assert.Equal(92.86m, RuneEfficiency.Calculate(rune).Current);
    }

    [Fact]
    public void Grind_ShouldCountInTheSubstatValue()
    {
        var withoutGrind = NewRune(level: 15, subs: [new(RuneStat.AttackPercent, 8, false, 0)]);
        var withGrind = NewRune(level: 15, subs: [new(RuneStat.AttackPercent, 8, false, 8)]);

        // +8% de ATK = 0,2 na razão = 7,14 pontos de eficiência
        Assert.Equal(7.14m, RuneEfficiency.Calculate(withGrind).Current - RuneEfficiency.Calculate(withoutGrind).Current);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(2, 4)]
    [InlineData(3, 3)]
    [InlineData(11, 1)]
    [InlineData(12, 0)]
    [InlineData(15, 0)]
    public void Max_ShouldAddAMaxRollForEachRemainingUpgrade(int level, int remainingUpgrades)
    {
        var efficiency = RuneEfficiency.Calculate(NewRune(level, subs: PerfectSubs));

        // Atual e máxima são arredondadas separadamente: a diferença pode variar 0,01
        var expected = remainingUpgrades * 0.2m / 2.8m * 100;
        Assert.InRange(efficiency.Max - efficiency.Current, expected - 0.01m, expected + 0.01m);
    }

    [Fact]
    public void MainStat_ShouldBeProportionalToSixStarValue()
    {
        var sixStars = NewRune(level: 15, stars: 6);
        var fiveStars = NewRune(level: 15, stars: 5);

        Assert.Equal(35.71m, RuneEfficiency.Calculate(sixStars).Current); // 1 ÷ 2,8
        Assert.Equal(33.16m, RuneEfficiency.Calculate(fiveStars).Current); // (39 ÷ 42) ÷ 2,8
    }

    private static Rune NewRune(int level, int stars = 6, RuneStatValue? innate = null, RuneSubstat[]? subs = null) =>
        new(
            1,
            slot: 2,
            stars,
            isAncient: false,
            RuneSet.Swift,
            level,
            rarity: 5,
            originalRarity: 5,
            new RuneStatValue(RuneStat.Speed, stars == 6 ? 42 : 39),
            innate,
            subs ?? [],
            equippedMonsterId: null);
}
