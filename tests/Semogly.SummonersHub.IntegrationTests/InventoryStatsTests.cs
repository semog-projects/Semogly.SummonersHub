using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Semogly.SummonersHub.Application.Accounts.GetInventoryStats;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class InventoryStatsTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task GetInventory_ShouldSummarizeRunesArtifactsAndCraftItems()
    {
        var payload = ImportPayload.Create(out var wizardId);
        var runes = payload["runes"]!.AsArray();

        // Runa com a maior velocidade da fixture: 30 de sub + 4 de grind, gemada
        var fastest = payload.FirstRune();
        fastest["substats"]![0] = new JsonObject { ["type"] = 8, ["value"] = 30, ["isGemmed"] = true, ["grindValue"] = 4 };
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var inventory = await _client.GetFromJsonAsync<InventoryStatsResponse>($"/api/accounts/{wizardId}/inventory", ApiJson.Options, Ct);

        Assert.NotNull(inventory);

        // Runas: as contagens fecham com o total em todas as distribuições
        Assert.Equal(runes.Count, inventory.Runes.Total);
        Assert.Equal(runes.Count(r => r!["equippedMonsterId"] is not null), inventory.Runes.Equipped);
        Assert.Equal(runes.Count(r => r!["level"]!.GetValue<int>() >= 15), inventory.Runes.MaxLevel);
        Assert.Equal(runes.Count, inventory.Runes.BySet.Sum(s => s.Total));
        Assert.Equal([1, 2, 3, 4, 5, 6], inventory.Runes.BySlot.Select(c => c.Key));
        Assert.Equal(runes.Count, inventory.Runes.BySlot.Sum(c => c.Count));
        Assert.Equal(runes.Count, inventory.Runes.ByRarity.Sum(c => c.Count));
        Assert.Equal(runes.Count, inventory.Runes.ByStars.Sum(c => c.Count));

        // Top velocidade: ordem decrescente, com grind somado
        var top = inventory.Runes.TopSpeed;
        Assert.Equal(top.OrderByDescending(e => e.Speed).Select(e => e.Speed), top.Select(e => e.Speed));
        Assert.Equal(fastest["id"]!.GetValue<long>(), top[0].RuneId);
        Assert.Equal(34, top[0].Speed);
        Assert.Equal(4, top[0].Grind);
        Assert.True(top[0].IsGemmed);
        Assert.False(top[0].IsInnate);

        // Artefatos
        Assert.Equal(payload["artifacts"]!.AsArray().Count, inventory.Artifacts.Total);
        Assert.Equal(inventory.Artifacts.Total, inventory.Artifacts.ByType.Sum(c => c.Count));

        // Itens de craft decodificados: 150905 = gem Will, Taxa Crít., lendária
        Assert.Equal(3, inventory.Craft.Gems);
        Assert.Equal(0, inventory.Craft.Grinds);
        Assert.Contains(
            new CraftItemStats(CraftItemType.EnchantedGem, RuneSet.Will, (int)RuneStat.CriticalRate, 5, 1),
            inventory.Craft.Items);
    }
}
