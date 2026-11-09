using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;
using Semogly.SummonersHub.Application.Accounts.ListMonsters;
using Semogly.SummonersHub.Application.Accounts.ManualItems;
using Semogly.SummonersHub.Application.Accounts.OptimizeRunes;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class ManualItemsTests(ApiFixture fixture) : IAsyncLifetime
{
    // Lapis (Água, desperta): 6★ nível 40 com HP 9885 e VEL 95 no bestiário
    private const int LapisMasterId = 19811;

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
    public async Task ManualMonster_ShouldJoinTheAccountAndSurviveNewImports()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var created = await PostAsync<ManualMonsterResponse>($"/api/accounts/{wizardId}/manual-items/monsters", new { masterId = LapisMasterId });
        Assert.True(created.GameId < 0);
        Assert.Equal("Lapis", created.Name);

        // Novo import: o monstro manual continua
        payload["wizard"]!["level"] = 51;
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var monsters = await _client.GetFromJsonAsync<List<MonsterListItem>>($"/api/accounts/{wizardId}/monsters", ApiJson.Options, Ct);
        var manual = Assert.Single(monsters!, m => m.IsManual);
        Assert.Equal(created.GameId, manual.MonsterId);

        var detail = await _client.GetFromJsonAsync<MonsterDetailResponse>($"/api/accounts/{wizardId}/monsters/{created.GameId}", ApiJson.Options, Ct);
        Assert.True(detail!.IsManual);
        Assert.Equal(9885, detail.Base.Hp);
        Assert.Equal(6, detail.Stars);

        // Otimizador: monta build do monstro manual com runas do inventário/de outros
        var optimize = await _client.PostAsJsonAsync(
            $"/api/accounts/{wizardId}/monsters/{created.GameId}/optimize",
            new { sets = Array.Empty<string>(), focus = SpeedFocus, includeEquippedOnOthers = true },
            ApiJson.Options,
            Ct);
        optimize.EnsureSuccessStatusCode();
        var result = await optimize.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);
        Assert.NotEmpty(result!.Builds);

        // Não pode ser compartilhado (não está no import gravado)
        var share = await _client.PostAsync($"/api/accounts/{wizardId}/monsters/{created.GameId}/shares", null, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, share.StatusCode);

        // Excluir tira o monstro da conta
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/accounts/{wizardId}/manual-items/{created.GameId}", Ct)).StatusCode);
        monsters = await _client.GetFromJsonAsync<List<MonsterListItem>>($"/api/accounts/{wizardId}/monsters", ApiJson.Options, Ct);
        Assert.DoesNotContain(monsters!, m => m.IsManual);
    }

    [Fact]
    public async Task ManualCraftItems_ShouldBeValidatedListedAndHiddenFromOtherUsers()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var url = $"/api/accounts/{wizardId}/manual-items";

        var gem = await PostAsync<ManualCraftItemResponse>($"{url}/craft-items", new { type = "EnchantedGem", set = "Violent", stat = 8, rarity = 5, amount = 2 });
        Assert.Equal((8, 5, 2), (gem.Stat, gem.Rarity, gem.Amount));

        // Grind de Taxa Crít. não existe; monstro desconhecido também é recusado
        var invalidGrind = await _client.PostAsJsonAsync($"{url}/craft-items", new { type = "Grindstone", set = "Violent", stat = 9, rarity = 5, amount = 1 }, ApiJson.Options, Ct);
        var unknownMonster = await _client.PostAsJsonAsync($"{url}/monsters", new { masterId = 1 }, ApiJson.Options, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, invalidGrind.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownMonster.StatusCode);

        var items = await _client.GetFromJsonAsync<ManualItemsResponse>(url, ApiJson.Options, Ct);
        Assert.Equal(gem.GameId, Assert.Single(items!.CraftItems).GameId);

        using var intruder = await fixture.CreateAuthenticatedClientAsync(Ct);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync(url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"{url}/{gem.GameId}", Ct)).StatusCode);
    }

    [Fact]
    public async Task MonsterSearch_ShouldFindMonstersWithStats()
    {
        var found = await _client.GetFromJsonAsync<List<SearchResult>>("/api/game-data/monsters/search?name=lapis", ApiJson.Options, Ct);

        Assert.Contains(found!, m => m.Com2usId == LapisMasterId);
        Assert.All(found!, m => Assert.Contains("lapis", m.Name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<T> PostAsync<T>(string url, object body)
    {
        var response = await _client.PostAsJsonAsync(url, body, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, Ct))!;
    }

    private sealed record SearchResult(int Com2usId, string Name);
}
