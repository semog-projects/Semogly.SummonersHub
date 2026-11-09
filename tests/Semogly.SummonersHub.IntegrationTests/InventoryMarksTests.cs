using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.ListArtifacts;
using Semogly.SummonersHub.Application.Accounts.ListMonsters;
using Semogly.SummonersHub.Application.Accounts.ListRunes;
using Semogly.SummonersHub.Application.Accounts.OptimizeRunes;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class InventoryMarksTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly string[] RtaAndSiege = ["RTA", " Siege "];

    private static readonly string[] RtaAndSiegeLowercase = ["rta", "siege"];

    private static readonly string[] LapisTag = ["Lapis"];

    private static readonly string[] FarmTag = ["Farm"];

    private static readonly string[] SpeedFocus = ["Speed"];

    public async ValueTask InitializeAsync() => _client = await fixture.CreateAuthenticatedClientAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Marks_ShouldShowInListsAndSurviveNewImports()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var runeId = payload.FirstRune()["id"]!.GetValue<long>();
        var artifactId = payload["artifacts"]![0]!["id"]!.GetValue<long>();

        await PatchAsync(wizardId, new { kind = "Rune", itemIds = new[] { runeId }, locked = true, addTags = RtaAndSiege });
        await PatchAsync(wizardId, new { kind = "Artifact", itemIds = new[] { artifactId }, addTags = LapisTag });

        // Nova versão da conta: as marcações seguem pelo id do item no jogo
        payload["wizard"]!["level"] = 51;
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var rune = (await GetRunesAsync(wizardId)).Single(r => r.Rune.RuneId == runeId);
        Assert.True(rune.Locked);
        Assert.Equal(["RTA", "Siege"], rune.Tags);

        var artifact = (await _client.GetFromJsonAsync<List<ArtifactListItem>>($"/api/accounts/{wizardId}/artifacts", ApiJson.Options, Ct))!
            .Single(a => a.Artifact.ArtifactId == artifactId);
        Assert.False(artifact.Locked);
        Assert.Equal(["Lapis"], artifact.Tags);

        // Desbloquear e tirar as tags apaga a marcação
        await PatchAsync(wizardId, new { kind = "Rune", itemIds = new[] { runeId }, locked = false, removeTags = RtaAndSiegeLowercase });
        rune = (await GetRunesAsync(wizardId)).Single(r => r.Rune.RuneId == runeId);
        Assert.False(rune.Locked);
        Assert.Empty(rune.Tags);
    }

    [Fact]
    public async Task Marks_ShouldBeAppliedInBulk()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var ids = payload["runes"]!.AsArray().Select(r => r!["id"]!.GetValue<long>()).ToArray();

        await PatchAsync(wizardId, new { kind = "Rune", itemIds = ids, addTags = FarmTag });

        Assert.All(await GetRunesAsync(wizardId), r => Assert.Equal(["Farm"], r.Tags));
    }

    [Fact]
    public async Task Optimizer_ShouldNotTakeLockedRunesFromOtherMonsters()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var lapisId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        var otherId = payload["monsters"]![1]!["id"]!.GetValue<long>();
        var otherRunes = payload["runes"]!.AsArray()
            .Where(r => r!["equippedMonsterId"]?.GetValue<long>() == otherId)
            .Select(r => r!["id"]!.GetValue<long>())
            .ToArray();

        await PatchAsync(wizardId, new { kind = "Rune", itemIds = otherRunes, locked = true });

        var request = new { sets = Array.Empty<string>(), focus = SpeedFocus, includeEquippedOnOthers = true };
        var response = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{lapisId}/optimize", request, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Builds);
        Assert.All(result.Builds.SelectMany(b => b.Runes), r => Assert.DoesNotContain(r.Rune.RuneId, otherRunes));
    }

    [Fact]
    public async Task LockedMonster_ShouldLockItsEquipmentAcrossImports()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var lapisId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        var otherId = payload["monsters"]![1]!["id"]!.GetValue<long>();

        await PatchAsync(wizardId, new { kind = "Monster", itemIds = new[] { otherId }, locked = true });

        // Novo import: o bloqueio segue pelo id do monstro
        payload["wizard"]!["level"] = 51;
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var monsters = await _client.GetFromJsonAsync<List<MonsterListItem>>($"/api/accounts/{wizardId}/monsters", ApiJson.Options, Ct);
        Assert.True(monsters!.Single(m => m.MonsterId == otherId).Locked);
        Assert.False(monsters!.Single(m => m.MonsterId == lapisId).Locked);

        var runes = await GetRunesAsync(wizardId);
        Assert.All(runes.Where(r => r.EquippedOn?.MonsterId == otherId), r => Assert.True(r.LockedByMonster));
        Assert.All(runes.Where(r => r.EquippedOn?.MonsterId != otherId), r => Assert.False(r.LockedByMonster));

        // O otimizador do Lapis não usa o equipamento do monstro bloqueado...
        var request = new { sets = Array.Empty<string>(), focus = SpeedFocus, includeEquippedOnOthers = true };
        var response = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{lapisId}/optimize", request, ApiJson.Options, Ct);
        var result = await response.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);
        Assert.All(result!.Builds.SelectMany(b => b.Runes), r => Assert.NotEqual(otherId, r.EquippedOn?.MonsterId));

        // ...mas o do próprio monstro bloqueado continua disponível para ele
        var own = await _client.PostAsJsonAsync($"/api/accounts/{wizardId}/monsters/{otherId}/optimize", request, ApiJson.Options, Ct);
        var ownResult = await own.Content.ReadFromJsonAsync<OptimizeRunesResponse>(ApiJson.Options, Ct);
        Assert.Contains(ownResult!.Builds.SelectMany(b => b.Runes), r => r.EquippedOn?.MonsterId == otherId);
    }

    [Fact]
    public async Task Marks_ShouldValidateAndHideOtherUsers()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var runeId = payload.FirstRune()["id"]!.GetValue<long>();
        var url = $"/api/accounts/{wizardId}/inventory-marks";

        var nothingToChange = await _client.PatchAsJsonAsync(url, new { kind = "Rune", itemIds = new[] { runeId } }, ApiJson.Options, Ct);
        var tooLongTag = await _client.PatchAsJsonAsync(url, new { kind = "Rune", itemIds = new[] { runeId }, addTags = new[] { new string('x', 30) } }, ApiJson.Options, Ct);

        using var intruder = await fixture.CreateAuthenticatedClientAsync(Ct);
        var fromIntruder = await intruder.PatchAsJsonAsync(url, new { kind = "Rune", itemIds = new[] { runeId }, locked = true }, ApiJson.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, nothingToChange.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLongTag.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fromIntruder.StatusCode);
    }

    private async Task PatchAsync(long wizardId, object body)
    {
        var response = await _client.PatchAsJsonAsync($"/api/accounts/{wizardId}/inventory-marks", body, ApiJson.Options, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<List<RuneListItem>> GetRunesAsync(long wizardId) =>
        (await _client.GetFromJsonAsync<List<RuneListItem>>($"/api/accounts/{wizardId}/runes", ApiJson.Options, Ct))!;
}
