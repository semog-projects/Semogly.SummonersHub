using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public class ImportAccountTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Import_ShouldCreateFirstVersion()
    {
        var payload = ImportPayload.Create(out var wizardId);

        var response = await _client.PostAsJsonAsync("/api/imports", payload, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/accounts/{wizardId}", response.Headers.Location?.OriginalString);

        var result = await response.Content.ReadFromJsonAsync<ImportAccountResponse>(Ct);
        Assert.NotNull(result);
        Assert.True(result.Created);
        Assert.Equal(1, result.Version);
        Assert.Equal(wizardId, result.WizardId);
        Assert.Equal(payload["monsters"]!.AsArray().Count, result.Monsters);
        Assert.Equal(payload["runes"]!.AsArray().Count, result.Runes);
        Assert.Equal(payload["artifacts"]!.AsArray().Count, result.Artifacts);
        Assert.Equal(payload["craftItems"]!.AsArray().Count, result.CraftItems);
    }

    [Fact]
    public async Task Import_ShouldBeIdempotentForSameContent()
    {
        var payload = ImportPayload.Create(out _);
        var first = await PostAsync(payload);

        // Novo export da mesma conta sem mudanças: só a data do export muda
        payload["exportedAt"] = "2030-01-01T00:00:00Z";
        var response = await _client.PostAsJsonAsync("/api/imports", payload, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var second = await response.Content.ReadFromJsonAsync<ImportAccountResponse>(Ct);
        Assert.NotNull(second);
        Assert.False(second.Created);
        Assert.Equal(first.SnapshotId, second.SnapshotId);
        Assert.Equal(first.Runes, second.Runes);
    }

    [Fact]
    public async Task Import_ShouldCreateNewVersionWhenContentChanges()
    {
        var payload = ImportPayload.Create(out var wizardId);
        await PostAsync(payload);

        payload.FirstRune()["level"] = 15;
        payload["wizard"]!["level"] = 51;
        var second = await PostAsync(payload);

        Assert.True(second.Created);
        Assert.Equal(2, second.Version);

        var summary = await _client.GetFromJsonAsync<AccountSummaryResponse>($"/api/accounts/{wizardId}", Ct);
        Assert.NotNull(summary);
        Assert.Equal(2, summary.Version);
        Assert.Equal(51, summary.WizardLevel);
        Assert.Equal("TestWizard", summary.WizardName);
        Assert.Equal(second.Runes, summary.Runes);
    }

    [Fact]
    public async Task Import_ShouldPersistRuneAndArtifactDetails()
    {
        var payload = ImportPayload.Create(out _);
        var rune = payload.FirstRune();
        rune["substats"]![0] = new JsonObject { ["type"] = 8, ["value"] = 5, ["isGemmed"] = true, ["grindValue"] = 3 };
        rune["innateStat"] = null;
        var artifact = payload["artifacts"]![0]!.AsObject();
        artifact["substats"]![0] = new JsonObject { ["type"] = 218, ["value"] = 0.2, ["upgrades"] = 1 };

        var result = await PostAsync(payload);

        await using var scope = fixture.CreateScope();
        var db = ApiFixture.DbContext(scope);
        var snapshot = await db.AccountSnapshots
            .Include(s => s.Runes)
            .Include(s => s.Artifacts)
            .Include(s => s.Monsters)
            .AsSplitQuery()
            .SingleAsync(s => s.Id == result.SnapshotId, Ct);

        var savedRune = snapshot.Runes.Single(r => r.GameId == rune["id"]!.GetValue<long>());
        Assert.Null(savedRune.InnateStat);
        Assert.Equal(RuneStat.Speed, savedRune.Substats[0].Stat);
        Assert.True(savedRune.Substats[0].IsGemmed);
        Assert.Equal(3, savedRune.Substats[0].GrindValue);

        var savedArtifact = snapshot.Artifacts.Single(a => a.GameId == artifact["id"]!.GetValue<long>());
        Assert.Equal(ArtifactEffect.AdditionalDamageByHp, savedArtifact.Substats[0].Effect);
        Assert.Equal(0.2m, savedArtifact.Substats[0].Value);

        // Runas equipadas continuam ligadas ao monstro
        var lapis = snapshot.Monsters.Single(m => m.MasterId == 19811);
        Assert.Equal(6, snapshot.Runes.Count(r => r.EquippedMonsterId == lapis.GameId));
        Assert.Equal(9885, lapis.BaseStats.Hp);
        Assert.NotEmpty(lapis.Skills);
    }

    [Fact]
    public async Task Import_ShouldRejectUnknownGameCodes()
    {
        var payload = ImportPayload.Create(out _);
        payload.FirstRune()["setId"] = 77;

        var response = await _client.PostAsJsonAsync("/api/imports", payload, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Equal("Accounts.UnknownGameCodes", problem?.Title);
        Assert.Contains("set 77", problem?.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Import_ShouldRejectRuneEquippedOnUnknownMonster()
    {
        var payload = ImportPayload.Create(out _);
        payload.FirstRune()["equippedMonsterId"] = 123;

        var response = await _client.PostAsJsonAsync("/api/imports", payload, Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Import_ShouldRejectUnsupportedSchemaVersion()
    {
        var payload = ImportPayload.Create(out _);
        payload["schemaVersion"] = 2;

        var response = await _client.PostAsJsonAsync("/api/imports", payload, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountSummary_ShouldReturnNotFoundForUnknownWizard()
    {
        var response = await _client.GetAsync("/api/accounts/1", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ImportAccountResponse> PostAsync(JsonObject payload)
    {
        var response = await _client.PostAsJsonAsync("/api/imports", payload, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportAccountResponse>(Ct))!;
    }
}
