using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.GameData;

public class CraftItemCodeTests
{
    [Theory]
    [InlineData(150905, RuneSet.Will, RuneStat.CriticalRate, 5)]
    [InlineData(10104, RuneSet.Energy, RuneStat.HpFlat, 4)]
    [InlineData(20804, RuneSet.Guard, RuneStat.Speed, 4)]
    [InlineData(990805, RuneSet.Immemorial, RuneStat.Speed, 5)]
    public void Parse_ShouldDecodeSetStatAndRarity(int code, RuneSet set, RuneStat stat, int rarity) =>
        Assert.Equal(new CraftItemCode(set, stat, rarity), CraftItemCode.Parse(code));

    [Fact]
    public void Parse_ShouldRemoveAncientOffsetFromRarity() =>
        Assert.Equal(5, CraftItemCode.Parse(40915).Rarity);

    [Theory]
    [InlineData(CraftItemType.EnchantedGem, true, false)]
    [InlineData(CraftItemType.Grindstone, false, false)]
    [InlineData(CraftItemType.ImmemorialGem, true, false)]
    [InlineData(CraftItemType.AncientGem, true, true)]
    [InlineData(CraftItemType.AncientGrindstone, false, true)]
    public void CraftItemType_ShouldClassifyGemAndAncient(CraftItemType type, bool isGem, bool isAncient)
    {
        Assert.Equal(isGem, type.IsGem());
        Assert.Equal(isAncient, type.IsAncient());
    }
}
