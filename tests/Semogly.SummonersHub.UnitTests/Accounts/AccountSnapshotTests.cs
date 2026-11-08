using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class AccountSnapshotTests
{
    private static readonly WizardProfile Wizard = new(12345678, "TestWizard", 50, 4);

    [Fact]
    public void Create_ShouldKeepWizardAndChildren()
    {
        var snapshot = Create(monsters: [NewMonster(1)], runes: [NewRune(10, equippedOn: 1)]);

        Assert.Equal(12345678, snapshot.WizardId);
        Assert.Equal("TestWizard", snapshot.WizardName);
        Assert.Single(snapshot.Monsters);
        Assert.Single(snapshot.Runes);
        Assert.NotEqual(Guid.Empty, snapshot.Id);
    }

    [Fact]
    public void Create_ShouldRejectDuplicatedRunes() =>
        Assert.Throws<DomainException>(() => Create(runes: [NewRune(10), NewRune(10)]));

    [Fact]
    public void Create_ShouldRejectRuneEquippedOnMissingMonster() =>
        Assert.Throws<DomainException>(() => Create(monsters: [NewMonster(1)], runes: [NewRune(10, equippedOn: 2)]));

    [Fact]
    public void Create_ShouldRejectInvalidVersion() =>
        Assert.Throws<DomainException>(() => AccountSnapshot.Create(Wizard, 0, "hash", null, DateTimeOffset.UtcNow, [], [], [], []));

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Rune_ShouldRejectInvalidSlot(int slot) =>
        Assert.Throws<DomainException>(() => new Rune(1, slot, 6, false, RuneSet.Swift, 15, 5, 5, new RuneStatValue(RuneStat.Speed, 42), null, [], null));

    [Fact]
    public void CraftItem_ShouldDecodeCode() =>
        Assert.Equal(RuneSet.Will, new CraftItem(1, CraftItemType.EnchantedGem, 150905, 1).Decode().Set);

    private static AccountSnapshot Create(IEnumerable<Monster>? monsters = null, IEnumerable<Rune>? runes = null) =>
        AccountSnapshot.Create(Wizard, 1, "hash", null, DateTimeOffset.UtcNow, monsters ?? [], runes ?? [], [], []);

    private static Monster NewMonster(long id) =>
        new(id, 19811, 40, 6, Element.Water, new BaseStats(9885, 736, 582, 95, 15, 0, 15, 50), [new MonsterSkill(10616, 5)], false, null);

    private static Rune NewRune(long id, long? equippedOn = null) =>
        new(id, 2, 6, false, RuneSet.Swift, 15, 5, 5, new RuneStatValue(RuneStat.Speed, 42), null, [], equippedOn);
}
