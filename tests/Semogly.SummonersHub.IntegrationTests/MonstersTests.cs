using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;
using Semogly.SummonersHub.Application.Accounts.ListMonsters;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class MonstersTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _client = await fixture.CreateAuthenticatedClientAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ListMonsters_ShouldReturnEveryMonsterWithTotals()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var monsters = await _client.GetFromJsonAsync<List<MonsterListItem>>($"/api/accounts/{wizardId}/monsters", ApiJson.Options, Ct);

        Assert.NotNull(monsters);
        Assert.Equal(payload["monsters"]!.AsArray().Count, monsters.Count);

        var lapis = monsters.Single(m => m.MasterId == 19811);
        Assert.Equal("Lapis", lapis.Name);
        Assert.Equal(6, lapis.Runes);
        Assert.Equal(2, lapis.Artifacts);
        Assert.NotEmpty(lapis.ActiveSets);
        Assert.True(lapis.Total.Hp > 9885);
        Assert.True(lapis.Total.EffectiveHp > lapis.Total.Hp);
        Assert.NotNull(lapis.AverageRuneEfficiency);
    }

    [Fact]
    public async Task GetMonsterDetail_ShouldReturnStatsRunesAndArtifacts()
    {
        var payload = ImportPayload.Create(out var wizardId);
        var monsterId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var detail = await _client.GetFromJsonAsync<MonsterDetailResponse>($"/api/accounts/{wizardId}/monsters/{monsterId}", ApiJson.Options, Ct);

        Assert.NotNull(detail);
        Assert.Equal("Lapis", detail.Name);
        Assert.Equal(9885, detail.Base.Hp);
        Assert.True(detail.Total.Attack > detail.Base.Attack);
        Assert.True(detail.Total.Speed > detail.Base.Speed);
        Assert.NotEmpty(detail.Skills);

        // Média da eficiência das 6 runas equipadas (a API arredonda a média, não cada runa: tolerância de 0,01)
        var average = detail.Runes.Average(r => r.Efficiency);
        Assert.InRange(detail.AverageRuneEfficiency!.Value, average - 0.01m, average + 0.01m);

        // Runas ordenadas por slot e com os códigos do jogo
        Assert.Equal([1, 2, 3, 4, 5, 6], detail.Runes.Select(r => r.Slot));
        Assert.All(detail.Runes, r => Assert.True(Enum.IsDefined((RuneStat)r.MainStat.Type)));

        Assert.Equal([ArtifactType.Element, ArtifactType.Archetype], detail.Artifacts.Select(a => a.Type));
        Assert.Equal(Element.Water, detail.Artifacts[0].Element);
        Assert.NotNull(detail.Artifacts[1].Archetype);
    }

    [Fact]
    public async Task GetMonsterDetail_ShouldReturnNotFoundForUnknownMonster()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/accounts/{wizardId}/monsters/1", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
