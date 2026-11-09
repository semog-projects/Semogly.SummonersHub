using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
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

    [Fact]
    public async Task Bestiary_ShouldListOnlyObtainableMonstersWithoutLogin()
    {
        using var anonymous = fixture.CreateClient();

        var monsters = await anonymous.GetFromJsonAsync<List<JsonObject>>("/api/game-data/bestiary", ApiJson.Options, Ct);

        Assert.NotNull(monsters);
        Assert.True(monsters.Count > 1_500);
        var ids = monsters.Select(m => m["com2usId"]!.GetValue<int>()).ToHashSet();
        Assert.Contains(19811, ids); // Lapis
        Assert.DoesNotContain(19823, ids); // Imperfect Magic Knight: NPC, não obtenível
    }

    [Fact]
    public async Task BestiaryMonster_ShouldReturnStatsSkillsLeaderAndFamily()
    {
        using var anonymous = fixture.CreateClient();

        var lapis = await anonymous.GetFromJsonAsync<JsonObject>("/api/game-data/bestiary/19811", ApiJson.Options, Ct);

        Assert.NotNull(lapis);
        Assert.Equal("Lapis", lapis["monster"]!["name"]!.GetValue<string>());
        Assert.True(lapis["maxLevelStats"]!["hp"]!.GetValue<int>() > 0);
        Assert.Equal("Attack Power", lapis["leaderSkill"]!["attribute"]!.GetValue<string>());
        Assert.Contains("Water Scroll", lapis["sources"]!.AsArray().Select(s => s!.GetValue<string>()));

        var skills = lapis["skills"]!.AsArray();
        Assert.Equal([1, 2, 3], skills.Select(s => s!["slot"]!.GetValue<int>()));
        Assert.All(skills, skill => Assert.False(string.IsNullOrWhiteSpace(skill!["description"]?.GetValue<string>())));
        Assert.Contains(skills, skill => skill!["iconUrl"]!.GetValue<string>().StartsWith("https://swarfarm.com/", StringComparison.Ordinal));

        // Os 4 outros elementos desperto + 5 não despertos; o NPC fica de fora
        var family = lapis["family"]!.AsArray().Select(m => m!["com2usId"]!.GetValue<int>()).ToList();
        Assert.Equal(9, family.Count);
        Assert.DoesNotContain(19823, family);
    }

    [Theory]
    [InlineData(19823)] // NPC
    [InlineData(1)] // id inexistente
    public async Task BestiaryMonster_ShouldReturn404ForUnknownOrUnobtainable(int com2usId)
    {
        using var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync($"/api/game-data/bestiary/{com2usId}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
