using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.OptimizeRunes;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class OptimizeRunesTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _client = await fixture.CreateAuthenticatedClientAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    // Lapis da fixture usa Vampire (4) + Revenge (2); o pool sem outros monstros tem as runas dele e as do inventário
    private static readonly object LapisRequest = new
    {
        sets = new[] { "Vampire", "Revenge" },
        mainStats = new Dictionary<string, int[]>(),
        focus = new[] { "Speed", "Attack" },
        minimums = Array.Empty<object>(),
        includeEquippedOnOthers = false,
        assumeMaxLevel = false,
    };

    [Fact]
    public async Task Optimize_ShouldReturnBuildsAtLeastAsGoodAsTheCurrentOne()
    {
        var (wizardId, monsterId) = await ImportAsync();

        var response = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{monsterId}/optimize", LapisRequest, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);

        Assert.NotNull(result);
        Assert.Equal(6, result.Current.Runes.Count);
        Assert.NotEmpty(result.Builds);

        // A build atual é uma das combinações possíveis: a melhor encontrada não pode ser pior
        var best = result.Builds[0];
        Assert.True(best.Score >= result.Current.Score, $"{best.Score} < {result.Current.Score}");
        Assert.Equal([1, 2, 3, 4, 5, 6], best.Runes.Select(r => r.Rune.Slot));
        Assert.Contains(RuneSet.Vampire, best.ActiveSets);
        Assert.Contains(RuneSet.Revenge, best.ActiveSets);

        // Sem runas de outros monstros: só do Lapis ou do inventário
        Assert.All(best.Runes, r => Assert.True(r.EquippedOn is null || r.EquippedOn.MonsterId == monsterId));
    }

    [Fact]
    public async Task Optimize_WithArtifacts_ShouldOnlyUseCompatibleOnes()
    {
        var (wizardId, monsterId) = await ImportAsync();
        var request = new
        {
            sets = new[] { "Vampire" },
            focus = new[] { "Attack" },
            includeEquippedOnOthers = false,
            assumeMaxLevel = true,
            optimizeArtifacts = true,
        };

        var response = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{monsterId}/optimize", request, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);

        Assert.NotNull(result);
        Assert.Equal(2, result.Current.Artifacts.Count);

        // Os artefatos do inventário são de Vento e de arquétipo Defesa: o Lapis (Água, Ataque) fica com os dele
        var best = result.Builds[0];
        Assert.Equal(
            result.Current.Artifacts.Select(a => a.Artifact.ArtifactId).Order(),
            best.Artifacts.Select(a => a.Artifact.ArtifactId).Order());
    }

    [Fact]
    public async Task Optimize_ShouldValidateTheRequest()
    {
        var (wizardId, monsterId) = await ImportAsync();
        var invalid = new { sets = new[] { "Violent", "Swift" }, focus = new[] { "Speed" }, includeEquippedOnOthers = false, assumeMaxLevel = false };

        var response = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{monsterId}/optimize", invalid, ApiJson.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Optimize_ShouldValidateWeightsAndMaximums()
    {
        var (wizardId, monsterId) = await ImportAsync();
        var url = $"/api/accounts/{wizardId}/monsters/{monsterId}/optimize";

        // Peso de um atributo fora do foco
        var weightOutsideFocus = new { sets = Array.Empty<string>(), focus = new[] { "Speed" }, weights = new Dictionary<string, decimal> { ["Hp"] = 2 } };

        // Máximo menor que o mínimo
        var maxBelowMin = new
        {
            sets = Array.Empty<string>(),
            focus = new[] { "Speed" },
            minimums = new[] { new { stat = "Speed", min = 200 } },
            maximums = new[] { new { stat = "Speed", max = 150 } },
        };

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(url, weightOutsideFocus, ApiJson.Options, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(url, maxBelowMin, ApiJson.Options, Ct)).StatusCode);
    }

    [Fact]
    public async Task Optimize_WithWeightsAndMaximum_ShouldRespectTheCap()
    {
        var (wizardId, monsterId) = await ImportAsync();
        var request = new
        {
            sets = new[] { "Vampire" },
            focus = new[] { "Speed", "EffectiveHp" },
            weights = new Dictionary<string, decimal> { ["Speed"] = 3 },
            maximums = new[] { new { stat = "Speed", max = 140 } },
        };

        var response = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{monsterId}/optimize", request, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);

        Assert.NotNull(result);
        Assert.All(result.Builds, b => Assert.True(b.Total.Speed <= 140, $"VEL {b.Total.Speed}"));
    }

    [Fact]
    public async Task Optimize_ShouldHideOtherUsersAndUnknownMonsters()
    {
        var (wizardId, monsterId) = await ImportAsync();
        using var intruder = await fixture.CreateAuthenticatedClientAsync(Ct);

        var fromIntruder = await intruder.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{monsterId}/optimize", LapisRequest, ApiJson.Options, Ct);
        var unknownMonster = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/1/optimize", LapisRequest, ApiJson.Options, Ct);

        Assert.Equal(HttpStatusCode.NotFound, fromIntruder.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownMonster.StatusCode);
    }

    private async Task<(long WizardId, long MonsterId)> ImportAsync()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        return (wizardId, payload["monsters"]![0]!["id"]!.GetValue<long>());
    }
}
