using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.OptimizeRunes;
using Semogly.SummonersHub.Application.Accounts.OptimizerPresets;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class OptimizerPresetsTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string Url = "/api/optimizer-presets";

    private static readonly string[] SpeedFocus = ["Speed"];

    private static readonly string[] SpeedAndAttack = ["Speed", "Attack"];

    private static readonly string[] FightTwice = ["Fight", "Fight"];

    private static readonly string[] ViolentWill = ["Violent", "Will"];

    private static readonly string[] SwiftOnly = ["Swift"];

    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _client = await fixture.CreateAuthenticatedClientAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Presets_ShouldBeSavedReplacedByNameListedAndDeleted()
    {
        var first = await SaveAsync("Violent VEL", new { focus = SpeedFocus, sets = ViolentWill });
        await SaveAsync("Bruiser", new { focus = SpeedAndAttack });

        // Mesmo nome com outra caixa substitui o preset, sem criar outro
        var replaced = await SaveAsync("violent vel", new { focus = SpeedAndAttack });
        Assert.Equal(first.Id, replaced.Id);
        Assert.Equal("Violent VEL", replaced.Name);

        var presets = await _client.GetFromJsonAsync<List<OptimizerPresetResponse>>(Url, ApiJson.Options, Ct);
        Assert.NotNull(presets);
        Assert.Equal(["Bruiser", "Violent VEL"], presets.Select(p => p.Name));
        Assert.Equal(2, presets[1].Criteria.GetProperty("focus").GetArrayLength());

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{Url}/{first.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"{Url}/{first.Id}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Presets_ShouldBelongToTheirUserAndBeValidated()
    {
        var preset = await SaveAsync("Meu", new { focus = SpeedFocus });
        using var other = await fixture.CreateAuthenticatedClientAsync(Ct);

        Assert.Empty((await other.GetFromJsonAsync<List<OptimizerPresetResponse>>(Url, ApiJson.Options, Ct))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"{Url}/{preset.Id}", Ct)).StatusCode);

        var emptyName = await _client.PostAsJsonAsync(Url, new { name = "  ", criteria = new { } }, ApiJson.Options, Ct);
        var notAnObject = await _client.PostAsJsonAsync(Url, new { name = "x", criteria = SpeedFocus }, ApiJson.Options, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, emptyName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, notAnObject.StatusCode);
    }

    [Fact]
    public async Task Optimize_ShouldAcceptTeamSetsAndSimulatedGrinds()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var lapisId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        var url = $"/api/accounts/{wizardId}/monsters/{lapisId}/optimize";

        var plain = await OptimizeAsync(url, new { sets = Array.Empty<string>(), focus = SpeedAndAttack });
        var boosted = await OptimizeAsync(url, new { sets = Array.Empty<string>(), focus = SpeedAndAttack, simulateGrinds = true, teamSets = FightTwice });

        Assert.True(boosted.Builds[0].Total.Attack > plain.Builds[0].Total.Attack);
        Assert.True(boosted.Builds[0].Score > plain.Builds[0].Score);

        // Swift não é set de equipe
        var invalid = await _client.PostAsJsonAsync(url, new { sets = Array.Empty<string>(), focus = SpeedFocus, teamSets = SwiftOnly }, ApiJson.Options, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    private async Task<OptimizerPresetResponse> SaveAsync(string name, object criteria)
    {
        var response = await _client.PostAsJsonAsync(Url, new { name, criteria }, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OptimizerPresetResponse>(ApiJson.Options, Ct))!;
    }

    private async Task<OptimizeRunesResponse> OptimizeAsync(string url, object request)
    {
        var response = await _client.PostAsJsonAsync(url, request, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct))!;
    }
}
