using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.GetMonsterStats;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class MonsterStatsTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync() => _client = await fixture.CreateAuthenticatedClientAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetMonsterStats_ShouldCalculateTotalsFromLatestImport()
    {
        var payload = ImportPayload.Create(out var wizardId);
        var monsterId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var stats = await _client.GetFromJsonAsync<MonsterStatsResponse>($"/api/accounts/{wizardId}/monsters/{monsterId}/stats", ApiJson.Options, Ct);

        Assert.NotNull(stats);
        Assert.Equal("Lapis", stats.Name);
        Assert.Equal(9885, stats.Base.Hp);

        // O Lapis da fixture tem 6 runas e 2 artefatos equipados: todos os atributos principais sobem
        Assert.True(stats.Total.Hp > stats.Base.Hp);
        Assert.True(stats.Total.Attack > stats.Base.Attack);
        Assert.True(stats.Total.Speed > stats.Base.Speed);
    }

    [Fact]
    public async Task GetMonsterStats_ShouldReturnNotFoundForUnknownMonster()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/accounts/{wizardId}/monsters/1/stats", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
