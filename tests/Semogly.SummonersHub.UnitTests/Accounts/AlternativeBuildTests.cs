using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class AlternativeBuildTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    private static Rune RuneAt(long id, int slot, long? monsterId) =>
        new(id, slot, 6, false, RuneSet.Swift, 15, 5, 5, new RuneStatValue(RuneStat.HpFlat, 100), null, [], monsterId);

    [Fact]
    public void ResolveRunes_ShouldReplaceOnlyTheSlotsOfTheAlternativeBuild()
    {
        var normal = Enumerable.Range(1, 6).Select(slot => RuneAt(slot, slot, 10)).ToList();
        var borrowed = RuneAt(99, 2, 20);
        var byId = normal.Append(borrowed).ToDictionary(r => r.GameId);

        var build = new AlternativeBuild(10, BuildKind.Rta, 0, [99], []);

        Assert.Equal([1, 99, 3, 4, 5, 6], build.ResolveRunes(normal, byId).Select(r => r.GameId));
    }

    [Fact]
    public void Key_ShouldIdentifyTheModeAndTheSiegeDeck()
    {
        Assert.Equal("rta", new AlternativeBuild(1, BuildKind.Rta, 55, [], []).Key);
        Assert.Equal("arena", new AlternativeBuild(1, BuildKind.Arena, 0, [], []).Key);
        Assert.Equal("siege-55", new AlternativeBuild(1, BuildKind.Siege, 55, [], []).Key);
    }

    [Fact]
    public void Snapshot_ShouldRejectBuildsOutsideTheImportOrDuplicated()
    {
        var stats = new BaseStats(10000, 700, 600, 100, 15, 0, 15, 50);
        Monster[] monsters = [new(10, 19811, 40, 6, Element.Water, stats, [], false, null)];
        Rune[] runes = [RuneAt(1, 1, 10)];

        AccountSnapshot Create(params AlternativeBuild[] builds) =>
            AccountSnapshot.Create(new WizardProfile(1, "Teste", 50, null), 1, "hash", null, Now, monsters, runes, [], [], builds);

        Assert.Single(Create(new AlternativeBuild(10, BuildKind.Rta, 0, [1], [])).Builds);
        Assert.Throws<DomainException>(() => Create(new AlternativeBuild(11, BuildKind.Rta, 0, [], [])));
        Assert.Throws<DomainException>(() => Create(new AlternativeBuild(10, BuildKind.Rta, 0, [2], [])));
        Assert.Throws<DomainException>(() => Create(new AlternativeBuild(10, BuildKind.Rta, 0, [], []), new AlternativeBuild(10, BuildKind.Rta, 0, [1], [])));
        Assert.Throws<DomainException>(() => new AlternativeBuild(10, BuildKind.Rta, 0, [1, 2, 3, 4, 5, 6, 7], []));
    }
}
