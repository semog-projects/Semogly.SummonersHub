using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class ManualItemsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ManualCraftItem_ShouldEncodeTheGameCode()
    {
        var gem = ManualCraftItem.Create(1, -1, CraftItemType.EnchantedGem, RuneSet.Will, RuneStat.CriticalRate, 5, 3, Now);

        Assert.Equal(150905, gem.Code); // Will, Taxa Crít., lendária (mesmo formato do jogo)
        Assert.Equal(new CraftItemCode(RuneSet.Will, RuneStat.CriticalRate, 5), gem.ToCraftItem().Decode());
    }

    [Theory]
    [InlineData(CraftItemType.Grindstone, RuneSet.Violent, RuneStat.CriticalRate, 5, 1)] // grind não tem Taxa Crít.
    [InlineData(CraftItemType.EnchantedGem, RuneSet.Immemorial, RuneStat.Speed, 5, 1)] // immemorial não
    [InlineData(CraftItemType.AncientGem, RuneSet.Violent, RuneStat.Speed, 5, 1)] // ancient não
    [InlineData(CraftItemType.EnchantedGem, RuneSet.Violent, RuneStat.Speed, 6, 1)] // raridade até 5
    [InlineData(CraftItemType.EnchantedGem, RuneSet.Violent, RuneStat.Speed, 5, 0)] // quantidade positiva
    public void ManualCraftItem_ShouldRejectItemsThatDoNotExist(CraftItemType type, RuneSet set, RuneStat stat, int rarity, int amount) =>
        Assert.Throws<DomainException>(() => ManualCraftItem.Create(1, -1, type, set, stat, rarity, amount, Now));

    [Fact]
    public void Snapshot_WithManualItems_ShouldKeepTheIdentityAndOnlyAddNegativeIds()
    {
        var stats = new BaseStats(10000, 700, 600, 100, 15, 0, 15, 50);
        var imported = new Monster(10, 19811, 40, 6, Element.Water, stats, [], false, null);
        var snapshot = AccountSnapshot.Create(new WizardProfile(1, "Teste", 50, null), 3, "hash", null, Now, [imported], [], [], []);

        var manual = ManualMonster.Create(1, -1, 19811, Now).ToMonster(Element.Water, stats, [], false);
        var withManual = snapshot.WithManualItems([manual, imported], [new CraftItem(-2, CraftItemType.Grindstone, 130805, 1)]);

        Assert.Equal((snapshot.Id, snapshot.Version), (withManual.Id, withManual.Version));
        Assert.Equal([10, -1], withManual.Monsters.Select(m => m.GameId));
        Assert.Single(withManual.CraftItems);
        Assert.Single(snapshot.Monsters); // o original não muda
    }
}
