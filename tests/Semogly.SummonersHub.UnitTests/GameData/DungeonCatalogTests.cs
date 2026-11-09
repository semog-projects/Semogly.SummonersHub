using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.Infrastructure.GameData;

namespace Semogly.SummonersHub.UnitTests.GameData;

public class DungeonCatalogTests
{
    private static readonly EmbeddedDungeonCatalog Dungeons = new();

    [Fact]
    public void EveryDungeon_ShouldHaveStats() =>
        Assert.All(Enum.GetValues<Dungeon>(), dungeon => Assert.NotNull(Dungeons.Find(dungeon)));

    [Fact]
    public void GiantsKeep_ShouldBeTheB12FromSwarfarm()
    {
        var giants = Dungeons.Find(Dungeon.GiantsKeep);

        Assert.NotNull(giants);
        Assert.Equal(12, giants.Floor);
        Assert.Equal(70, giants.BossResistance);
        Assert.Equal(55, giants.AccuracyTarget);
    }

    [Fact]
    public void EveryTeam_ShouldHaveItsDungeonStats() =>
        Assert.All(new EmbeddedPveTeamCatalog().All(), team => Assert.NotNull(Dungeons.Find(team.Dungeon)));
}
