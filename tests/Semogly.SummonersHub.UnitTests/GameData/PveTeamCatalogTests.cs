using Semogly.SummonersHub.Infrastructure.GameData;

namespace Semogly.SummonersHub.UnitTests.GameData;

public class PveTeamCatalogTests
{
    private static readonly EmbeddedPveTeamCatalog Teams = new();

    private static readonly EmbeddedMonsterCatalog Monsters = new();

    [Fact]
    public void Catalog_ShouldLoadTeamsWithUniqueIds()
    {
        var teams = Teams.All();

        Assert.NotEmpty(teams);
        Assert.Equal(teams.Count, teams.Select(t => t.Id).Distinct().Count());
        Assert.All(teams, t => Assert.NotEmpty(t.Slots));
    }

    [Fact]
    public void Requirements_ShouldBeParsed()
    {
        var bernard = Teams.All().Single(t => t.Id == "gb-classic-f2p").Slots[0];

        Assert.Equal("Bernard", bernard.MonsterName);
        Assert.Equal([new Domain.GameData.StatRequirement(Domain.GameData.PveStat.Speed, 180)], bernard.Requirements);
    }

    [Fact]
    public void EverySlot_ShouldExistInTheBestiary()
    {
        // Nome desperto + elemento: um erro de digitação no JSON deixaria a vaga sempre "faltando"
        var known = Monsters.All().Select(m => (m.Name, m.Element)).ToHashSet();

        var unknown = Teams.All()
            .SelectMany(t => t.Slots)
            .Where(s => !known.Contains((s.MonsterName, s.Element)))
            .Select(s => $"{s.MonsterName} ({s.Element})")
            .ToList();

        Assert.Empty(unknown);
    }
}
