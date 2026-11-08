using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.ListRunes;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class RunesTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task ListRunes_ShouldReturnEveryRuneWithEfficiencyAndOwner()
    {
        var payload = ImportPayload.Create(out var wizardId);
        var runeId = payload.FirstRune()["id"]!.GetValue<long>();
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var runes = await _client.GetFromJsonAsync<List<RuneListItem>>($"/api/accounts/{wizardId}/runes", ApiJson.Options, Ct);

        Assert.NotNull(runes);
        Assert.Equal(payload["runes"]!.AsArray().Count, runes.Count);

        // Ordenadas pela eficiência atual, da maior para a menor
        Assert.Equal(runes.OrderByDescending(r => r.Rune.Efficiency).Select(r => r.Rune.RuneId), runes.Select(r => r.Rune.RuneId));
        Assert.All(runes, r => Assert.True(r.Rune.MaxEfficiency >= r.Rune.Efficiency));

        // Revenge slot 1, ATK +160, subs Dano Crít. 14, Taxa Crít. 12, RES 8 e PRE 7: (1 + 0,4 + 0,4 + 0,2 + 0,175) ÷ 2,8
        var first = runes.Single(r => r.Rune.RuneId == runeId);
        Assert.Equal(RuneSet.Revenge, first.Rune.Set);
        Assert.Equal(77.68m, first.Rune.Efficiency);
        Assert.Equal(first.Rune.Efficiency, first.Rune.MaxEfficiency);
        Assert.NotNull(first.EquippedOn);
        Assert.Equal("Lapis", first.EquippedOn.Name);
    }
}
