using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;
using Semogly.SummonersHub.Application.Accounts.ListMonsters;
using Semogly.SummonersHub.Application.Accounts.OptimizeRunes;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

/// <summary>Builds de RTA e das defesas de Siege/Arena vindas do import.</summary>
[Collection(ApiTestGroup.Name)]
public sealed class AlternativeBuildsTests(ApiFixture fixture) : IAsyncLifetime
{
    // Ids da fixture: Lapis (A) e o outro monstro (B), cada um com 6 runas equipadas
    private const long MonsterA = 33165492671;
    private const long MonsterB = 37220294772;
    private const long LooseSlot1Rune = 76922693351;
    private const long BSlot2Rune = 76824941371;

    private static readonly string[] SpeedFocus = ["Speed"];

    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _client = await fixture.CreateAuthenticatedClientAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Import_ShouldKeepTheBuildsAndShowThemPerMonster()
    {
        var wizardId = await ImportWithBuildsAsync();

        var monsters = await _client.GetFromJsonAsync<List<MonsterListItem>>($"/api/accounts/{wizardId}/monsters", ApiJson.Options, Ct);
        Assert.Equal([BuildKind.Rta], monsters!.Single(m => m.MonsterId == MonsterA).BuildKinds);
        Assert.Equal([BuildKind.Siege], monsters!.Single(m => m.MonsterId == MonsterB).BuildKinds);

        var normal = await GetDetailAsync(wizardId, MonsterA, null);
        Assert.Null(normal.SelectedBuild);
        Assert.Equal([new MonsterBuildOption("rta", BuildKind.Rta, 0)], normal.Builds);

        // RTA: troca os slots 1 e 2 (runa solta e runa emprestada do B); os demais seguem a build normal
        var rta = await GetDetailAsync(wizardId, MonsterA, "rta");
        Assert.Equal("rta", rta.SelectedBuild);
        Assert.Equal(6, rta.Runes.Count);
        Assert.Equal(LooseSlot1Rune, rta.Runes.Single(r => r.Slot == 1).RuneId);
        Assert.Equal(BSlot2Rune, rta.Runes.Single(r => r.Slot == 2).RuneId);
        Assert.Equal(
            normal.Runes.Where(r => r.Slot > 2).Select(r => r.RuneId),
            rta.Runes.Where(r => r.Slot > 2).Select(r => r.RuneId));
        Assert.NotEqual(normal.Total, rta.Total);

        var siege = await GetDetailAsync(wizardId, MonsterB, "siege-900");
        Assert.Equal([new MonsterBuildOption("siege-900", BuildKind.Siege, 1)], siege.Builds);

        var missing = await _client.GetAsync($"/api/accounts/{wizardId}/monsters/{MonsterA}?build=siege-900", Ct);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Optimize_ShouldCompareWithTheChosenBuild()
    {
        var wizardId = await ImportWithBuildsAsync();

        var request = new { sets = Array.Empty<string>(), focus = SpeedFocus, includeEquippedOnOthers = false, assumeMaxLevel = false, compareWith = "rta" };
        var response = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{MonsterA}/optimize", request, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);

        Assert.Equal(BSlot2Rune, result!.Current.Runes.Single(r => r.Rune.Slot == 2).Rune.RuneId);
        Assert.Equal(MonsterB, result.Current.Runes.Single(r => r.Rune.Slot == 2).EquippedOn!.MonsterId);
    }

    [Fact]
    public async Task Import_ShouldRejectBuildsWithItemsOutsideTheExport()
    {
        var payload = ImportPayload.Create(out _);
        payload["builds"] = new JsonArray(Build(MonsterA, 1, 0, [123]));

        var response = await _client.PostAsJsonAsync("/api/imports", payload, Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task<long> ImportWithBuildsAsync()
    {
        var payload = ImportPayload.Create(out var wizardId);
        payload["builds"] = new JsonArray(
            Build(MonsterA, 1, 0, [LooseSlot1Rune, BSlot2Rune]),
            Build(MonsterB, 2, 900, []));
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        return wizardId;
    }

    private async Task<MonsterDetailResponse> GetDetailAsync(long wizardId, long monsterId, string? build)
    {
        var query = build is null ? string.Empty : $"?build={build}";
        return (await _client.GetFromJsonAsync<MonsterDetailResponse>($"/api/accounts/{wizardId}/monsters/{monsterId}{query}", ApiJson.Options, Ct))!;
    }

    private static JsonObject Build(long monsterId, int kind, long deckId, long[] runeIds) => new()
    {
        ["monsterId"] = monsterId,
        ["kind"] = kind,
        ["deckId"] = deckId,
        ["runeIds"] = new JsonArray([.. runeIds.Select(id => (JsonNode)id)]),
        ["artifactIds"] = new JsonArray(),
    };
}
