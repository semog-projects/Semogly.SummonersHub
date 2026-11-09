using System.Net.Http.Json;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class GameDataTests(ApiFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetDungeons_ShouldBePublicAndReturnEnemyStats()
    {
        using var anonymous = fixture.CreateClient();

        var dungeons = await anonymous.GetFromJsonAsync<List<DungeonStats>>("/api/game-data/dungeons", ApiJson.Options, Ct);

        Assert.NotNull(dungeons);
        Assert.Equal(Enum.GetValues<Dungeon>(), dungeons.Select(d => d.Dungeon));
        var giants = dungeons.Single(d => d.Dungeon == Dungeon.GiantsKeep);
        Assert.Equal(12, giants.Floor);
        Assert.Equal(169, giants.MaxSpeed);
    }
}
