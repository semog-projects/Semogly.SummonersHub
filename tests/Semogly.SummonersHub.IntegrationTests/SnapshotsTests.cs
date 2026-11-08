using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Semogly.SummonersHub.Application.Accounts.CompareSnapshots;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class SnapshotsTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task ListSnapshots_ShouldReturnEveryVersionNewestFirst()
    {
        var wizardId = await ImportTwoVersionsAsync();

        var snapshots = await _client.GetFromJsonAsync<List<AccountSummaryResponse>>($"/api/accounts/{wizardId}/snapshots", ApiJson.Options, Ct);

        Assert.NotNull(snapshots);
        Assert.Equal([2, 1], snapshots.Select(s => s.Version));
        Assert.Equal([3, 2], snapshots.Select(s => s.Monsters));
    }

    [Fact]
    public async Task CompareSnapshots_ShouldDescribeWhatChanged()
    {
        var wizardId = await ImportTwoVersionsAsync();

        var diff = await _client.GetFromJsonAsync<SnapshotComparisonResponse>(
            $"/api/accounts/{wizardId}/snapshots/compare?from=1&to=2", ApiJson.Options, Ct);

        Assert.NotNull(diff);
        Assert.Equal(1, diff.From.Version);
        Assert.Equal(2, diff.To.Version);
        Assert.Equal(new Change<int>(50, 51), diff.WizardLevel);
        Assert.Equal(new Change<int>(2, 3), diff.Monsters);

        var added = Assert.Single(diff.AddedMonsters);
        Assert.Equal(NewMonsterId, added.MonsterId);
        Assert.Equal("Lapis", added.Name);
        Assert.Empty(diff.RemovedMonsters);

        // Só o monstro que subiu de nível; o Lapis original não mudou
        var progressed = Assert.Single(diff.ProgressedMonsters);
        Assert.Equal(new Change<int>(35, 40), progressed.Level);
        Assert.Equal(progressed.Speed.From, progressed.Speed.To);

        Assert.Equal(new EquipmentChanges(new Change<int>(16, 16), Added: 1, Removed: 1, Upgraded: 1), diff.Runes);
        Assert.Equal(new EquipmentChanges(new Change<int>(4, 4), Added: 0, Removed: 0, Upgraded: 0), diff.Artifacts);

        var newRune = Assert.Single(diff.TopNewRunes);
        Assert.Equal(NewRuneId, newRune.RuneId);
    }

    [Fact]
    public async Task CompareSnapshots_ShouldRejectSameVersionAndUnknownVersion()
    {
        var wizardId = await ImportTwoVersionsAsync();

        var same = await _client.GetAsync($"/api/accounts/{wizardId}/snapshots/compare?from=2&to=2", Ct);
        var unknown = await _client.GetAsync($"/api/accounts/{wizardId}/snapshots/compare?from=1&to=9", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    private const long NewMonsterId = 99_000_001;

    private const long NewRuneId = 99_000_002;

    /// <summary>
    /// Versão 1 = fixture. Versão 2: invocador sobe de nível, o segundo monstro vai do 35 ao 40, entra um Lapis
    /// novo sem equipamento, uma runa do inventário sobe de +0 para +12, outra sai e uma nova entra.
    /// </summary>
    private async Task<long> ImportTwoVersionsAsync()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        payload["wizard"]!["level"] = 51;

        var monsters = payload["monsters"]!.AsArray();
        monsters[1]!["level"] = 40;
        var newMonster = monsters[0]!.DeepClone().AsObject();
        newMonster["id"] = NewMonsterId;
        monsters.Add(newMonster);

        var runes = payload["runes"]!.AsArray();
        runes[12]!["level"] = 12;
        var newRune = runes[13]!.DeepClone().AsObject();
        newRune["id"] = NewRuneId;
        runes.RemoveAt(15);
        runes.Add((JsonNode)newRune);

        var second = await _client.PostAsJsonAsync("/api/imports", payload, Ct);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        return wizardId;
    }
}
