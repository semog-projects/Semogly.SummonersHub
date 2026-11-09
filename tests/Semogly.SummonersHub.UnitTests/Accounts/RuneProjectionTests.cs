using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class RuneProjectionTests
{
    [Fact]
    public void FourSubs_ShouldSpreadTheRemainingUpgradesAsAverageRolls()
    {
        // +6 com 4 subs: faltam +9 e +12 (2 rolls), ½ roll médio em cada sub
        var rune = NewRune(level: 6, [Sub(RuneStat.Speed, 5), Sub(RuneStat.HpPercent, 6), Sub(RuneStat.CriticalRate, 4), Sub(RuneStat.HpFlat, 200)]);

        var projected = RuneProjection.ToMaxLevel(rune, equippedMonsterId: 7);

        Assert.Equal(Rune.MaxLevel, projected.Level);
        Assert.Equal(42, projected.MainStat.Value);
        Assert.Equal(7, projected.EquippedMonsterId);
        Assert.Equal(rune.GameId, projected.GameId);

        // VEL: 5 + 2 × 5 ÷ 4 = 7,5 → 8; HP%: 6 + 2 × 6,5 ÷ 4 = 9,25 → 9; Taxa Crít.: 4 + 2,5 → 7 (6,5 arredonda para cima); HP: 200 + 127,5 → 328
        Assert.Equal([8, 9, 7, 328], projected.Substats.Select(s => s.Value));
    }

    [Fact]
    public void MissingSubs_ShouldConsumeUpgradesWithoutGuessingNewStats()
    {
        // +0 com 2 subs: os rolls de +3 e +6 criam subs novas (ignoradas); +9 e +12 caem nas existentes
        var rune = NewRune(level: 0, [Sub(RuneStat.Speed, 5), Sub(RuneStat.Accuracy, 4)]);

        var projected = RuneProjection.ToMaxLevel(rune, null);

        Assert.Equal(2, projected.Substats.Count);
        Assert.Equal([10, 10], projected.Substats.Select(s => s.Value)); // 5 + 2 × 5 ÷ 2; 4 + 2 × 6 ÷ 2
    }

    [Fact]
    public void AlreadyAt12_ShouldOnlyRaiseTheMainStat()
    {
        var rune = NewRune(level: 12, [Sub(RuneStat.Speed, 18)]);

        var projected = RuneProjection.ToMaxLevel(rune, null);

        Assert.Equal(18, projected.Substats[0].Value);
        Assert.Equal(42, projected.MainStat.Value);
    }

    [Fact]
    public void GrindAndGem_ShouldBeKept()
    {
        var rune = NewRune(level: 9, [new RuneSubstat(RuneStat.Speed, 5, true, 3), Sub(RuneStat.HpPercent, 5), Sub(RuneStat.AttackPercent, 5), Sub(RuneStat.DefensePercent, 5)]);

        var speed = RuneProjection.ToMaxLevel(rune, null).Substats[0];

        Assert.True(speed.IsGemmed);
        Assert.Equal(3, speed.GrindValue);
        Assert.Equal(6, speed.Value); // 5 + 1 × 5 ÷ 4 = 6,25
    }

    private static RuneSubstat Sub(RuneStat stat, int value) => new(stat, value, false, 0);

    private static Rune NewRune(int level, RuneSubstat[] subs) =>
        new(1, 2, 6, false, RuneSet.Swift, level, 5, 5, new RuneStatValue(RuneStat.Speed, 12), null, subs, null);
}
