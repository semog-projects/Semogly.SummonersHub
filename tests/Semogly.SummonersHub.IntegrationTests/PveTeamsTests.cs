using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.CheckPveTeams;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class PveTeamsTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task CheckPveTeams_ShouldMatchAccountMonstersAgainstReferenceTeams()
    {
        var payload = ImportPayload.Create(out var wizardId);

        // Bernard (Vento) sem runas: tem o monstro, mas a VEL base fica abaixo do mínimo do time
        var bernard = payload["monsters"]![0]!.DeepClone().AsObject();
        bernard["id"] = 99_100_001L;
        bernard["masterId"] = 11513;
        bernard["attribute"] = 3;
        bernard["baseStats"]!["speed"] = 111;
        payload["monsters"]!.AsArray().Add(bernard);

        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var teams = await _client.GetFromJsonAsync<List<PveTeamCheck>>($"/api/accounts/{wizardId}/pve-teams", ApiJson.Options, Ct);

        Assert.NotNull(teams);
        Assert.Equal(teams.OrderBy(t => t.Dungeon).Select(t => t.Id), teams.Select(t => t.Id));

        var giants = teams.Single(t => t.Id == "gb-classic-f2p");
        Assert.False(giants.IsReady);
        Assert.Equal(0, giants.ReadySlots);

        var bernardSlot = giants.Slots.Single(s => s.MonsterName == "Bernard");
        Assert.Equal(PveSlotStatus.BelowRequirements, bernardSlot.Status);
        Assert.Equal(99_100_001L, bernardSlot.Monster?.MonsterId);
        Assert.Equal([new UnmetRequirement(PveStat.Speed, 180, 111)], bernardSlot.Unmet);

        // PRE do debuffer vem da RES do boss do GB12 (70 − 15)
        Assert.Equal(70, giants.DungeonStats?.BossResistance);
        var belladeon = giants.Slots.Single(s => s.MonsterName == "Belladeon");
        Assert.Equal(PveSlotStatus.Missing, belladeon.Status);
        Assert.True(belladeon.Debuffer);
        Assert.Equal([new StatRequirement(PveStat.Accuracy, 55)], belladeon.Requirements);
    }
}
