using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Semogly.SummonersHub.Application.Accounts.GetCraftSuggestions;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class CraftSuggestionsTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task GetCraftSuggestions_ShouldRankRunesByEfficiencyGain()
    {
        var payload = ImportPayload.Create(out var wizardId);

        // 2 grinds lendários de VEL do set Vampire (11 × 10000 + 8 × 100 + 5): a fixture tem 5 runas Vampire com VEL sem grind
        payload["craftItems"]!.AsArray().Add(new JsonObject
        {
            ["id"] = 99_200_001L,
            ["craftType"] = 2,
            ["craftTypeId"] = 110805,
            ["amount"] = 2,
        });
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var groups = await _client.GetFromJsonAsync<List<CraftSuggestionGroup>>(
            $"/api/accounts/{wizardId}/craft-suggestions", ApiJson.Options, Ct);

        Assert.NotNull(groups);
        Assert.Equal(4, groups.Count);

        // O grind é o item de maior ganho: VEL +4 a +5 em qualquer runa Vampire com VEL
        var grind = groups[0];
        Assert.Equal((CraftItemType.Grindstone, RuneSet.Vampire, (int)RuneStat.Speed, 2), (grind.Type, grind.Set, grind.Stat, grind.Amount));
        Assert.Equal(new ValueRange(4, 5), grind.Range);
        Assert.Equal(5, grind.Suggestions.Count);
        Assert.All(grind.Suggestions, s => Assert.Equal(5.95m, s.MaxEfficiencyGain)); // 5 ÷ 30 ÷ 2,8
        Assert.All(grind.Suggestions, s => Assert.NotNull(s.EquippedOn));

        // Gem Fight PRE heroica (6 a 9): troca a pior sub de uma das 2 runas Fight
        var gem = groups[1];
        Assert.Equal((CraftItemType.EnchantedGem, RuneSet.Fight, (int)RuneStat.Accuracy), (gem.Type, gem.Set, gem.Stat));
        Assert.Equal(2, gem.Suggestions.Count);
        Assert.Equal(76824941371L, gem.Suggestions[0].Rune.RuneId);
        Assert.Equal((int)RuneStat.HpFlat, gem.Suggestions[0].TargetStat);

        // Gems de sets que a conta não tem ficam no fim, sem sugestão
        Assert.All(groups.Skip(2), g => Assert.Empty(g.Suggestions));
    }
}
