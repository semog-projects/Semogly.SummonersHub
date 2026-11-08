using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.GetAccountOverview;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class AccountOverviewTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task GetOverview_ShouldSummarizeLatestImport()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var monsters = payload["monsters"]!.AsArray();

        var overview = await _client.GetFromJsonAsync<AccountOverviewResponse>($"/api/accounts/{wizardId}/overview", ApiJson.Options, Ct);

        Assert.NotNull(overview);
        Assert.Equal(wizardId, overview.Wizard.WizardId);
        Assert.Equal("TestWizard", overview.Wizard.Name);
        Assert.Equal(1, overview.Wizard.Version);
        Assert.Equal(monsters.Count, overview.Monsters);

        // Todas as chaves aparecem e as contagens fecham com o total
        Assert.Equal([1, 2, 3, 4, 5, 6], overview.MonstersByStars.Select(c => c.Key));
        Assert.Equal(monsters.Count, overview.MonstersByStars.Sum(c => c.Count));
        Assert.Equal(Enum.GetValues<Element>(), overview.MonstersByElement.Select(c => c.Key));
        Assert.Equal(monsters.Count, overview.MonstersByElement.Sum(c => c.Count));
        Assert.Equal([0, 1, 2], overview.MonstersByAwakening.Select(c => c.Key));

        // Ranking em ordem decrescente de velocidade total, com o Lapis (6 runas) acima da velocidade base
        Assert.InRange(overview.SpeedRanking.Count, 1, GetAccountOverviewQuery.SpeedRankingSize);
        Assert.Equal(overview.SpeedRanking.OrderByDescending(e => e.TotalSpeed).Select(e => e.MonsterId), overview.SpeedRanking.Select(e => e.MonsterId));
        var lapis = overview.SpeedRanking.Single(e => e.MasterId == 19811);
        Assert.Equal("Lapis", lapis.Name);
        Assert.True(lapis.TotalSpeed > lapis.BaseSpeed);
    }

    [Fact]
    public async Task GetOverview_ShouldHideOtherUsersWizards()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        using var intruder = await fixture.CreateAuthenticatedClientAsync(Ct);

        var response = await intruder.GetAsync($"/api/accounts/{wizardId}/overview", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
