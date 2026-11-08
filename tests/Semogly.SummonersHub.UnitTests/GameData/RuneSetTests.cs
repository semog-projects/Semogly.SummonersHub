using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.GameData;

public class RuneSetTests
{
    [Theory]
    [InlineData(RuneSet.Swift, 4)]
    [InlineData(RuneSet.Violent, 4)]
    [InlineData(RuneSet.Despair, 4)]
    [InlineData(RuneSet.Energy, 2)]
    [InlineData(RuneSet.Will, 2)]
    [InlineData(RuneSet.Seal, 2)]
    [InlineData(RuneSet.Intangible, 0)]
    public void RequiredPieces_ShouldMatchSetSize(RuneSet set, int pieces) =>
        Assert.Equal(pieces, set.RequiredPieces());

    [Theory]
    [InlineData(RuneStat.Speed, true)]
    [InlineData(RuneStat.AttackPercent, true)]
    [InlineData(RuneStat.CriticalRate, false)]
    [InlineData(RuneStat.Accuracy, false)]
    public void IsGrindable_ShouldOnlyAcceptGrindableStats(RuneStat stat, bool grindable) =>
        Assert.Equal(grindable, stat.IsGrindable());
}
