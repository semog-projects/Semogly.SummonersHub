using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.GetMonsterSkills;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class MonsterSkillsTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task GetMonsterSkills_ShouldReturnFormulasAndSkillUpBonus()
    {
        var payload = ImportPayload.Create(out var wizardId);
        var lapisId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var result = await _client.GetFromJsonAsync<MonsterSkillsResponse>($"/api/accounts/{wizardId}/monsters/{lapisId}/skills", ApiJson.Options, Ct);

        Assert.NotNull(result);
        Assert.Equal("Lapis", result.Name);
        Assert.True(result.Total.Attack > 736);

        // Ordenadas pelo slot; todas no nível 5 (máximo) na fixture
        Assert.Equal(["Blade Surge", "Magic Shot", "Retrieve Magic"], result.Skills.Select(s => s.Name));
        var magicShot = result.Skills[1];
        Assert.Equal(4, magicShot.Hits);
        Assert.Equal(5, magicShot.Level);
        Assert.Equal(35, magicShot.DamageBonus);
        Assert.NotNull(magicShot.Formula);
    }

    [Fact]
    public async Task GetMonsterSkills_ShouldHideOtherUsersAndUnknownMonsters()
    {
        var payload = ImportPayload.Create(out var wizardId);
        var lapisId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        using var intruder = await fixture.CreateAuthenticatedClientAsync(Ct);

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/monsters/{lapisId}/skills", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/accounts/{wizardId}/monsters/1/skills", Ct)).StatusCode);
    }
}
