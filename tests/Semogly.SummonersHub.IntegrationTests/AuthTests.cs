using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public class AuthTests(ApiFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/api/accounts")]
    [InlineData("/api/accounts/1")]
    [InlineData("/api/accounts/1/inventory")]
    [InlineData("/api/accounts/1/monsters")]
    [InlineData("/api/accounts/1/monsters/1")]
    [InlineData("/api/accounts/1/runes")]
    [InlineData("/api/accounts/1/artifacts")]
    [InlineData("/api/accounts/1/snapshots")]
    [InlineData("/api/accounts/1/snapshots/compare?from=1&to=2")]
    public async Task AccountEndpoints_ShouldRequireLogin(string url)
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_ShouldRequireLogin()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/imports", ImportPayload.Create(out _), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoints_ShouldNotRequireLogin()
    {
        using var client = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/info", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/game-data/monsters?ids=19811", Ct)).StatusCode);
    }

    [Fact]
    public async Task Login_ShouldExposeCurrentUser()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(Ct);

        var info = await client.GetFromJsonAsync<JsonElement>("/api/auth/manage/info", Ct);

        Assert.EndsWith("@test.local", info.GetProperty("email").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_ShouldFailWithWrongPassword()
    {
        using var client = fixture.CreateClient();
        var credentials = new { email = "wrong-password@test.local", password = ApiFixture.TestPassword };
        (await client.PostAsJsonAsync("/api/auth/register", credentials, Ct)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login?useCookies=true", credentials with { password = "Outra123senha" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnPortugueseValidationMessages()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "weak@test.local", password = "abc" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Contains("A senha deve ter pelo menos 8 caracteres.", problem!.Errors.SelectMany(e => e.Value));
    }

    [Fact]
    public async Task Logout_ShouldEndSession()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(Ct);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null, Ct)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/accounts", Ct)).StatusCode);
    }

    [Fact]
    public async Task Wizard_ShouldBelongToFirstUserWhoImportedIt()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync(Ct);
        using var intruder = await fixture.CreateAuthenticatedClientAsync(Ct);
        var payload = ImportPayload.Create(out var wizardId);
        var monsterId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        (await owner.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        // Outro usuário não consegue importar o mesmo invocador...
        var import = await intruder.PostAsJsonAsync("/api/imports", payload, Ct);
        Assert.Equal(HttpStatusCode.Conflict, import.StatusCode);
        Assert.Equal("Accounts.LinkedToAnotherUser", (await import.Content.ReadFromJsonAsync<ProblemDetails>(Ct))?.Title);

        // ...nem ler os dados dele (responde como se não existisse)
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/inventory", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/monsters", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/monsters/{monsterId}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/runes", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/artifacts", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/snapshots", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/accounts/{wizardId}/snapshots/compare?from=1&to=2", Ct)).StatusCode);
        Assert.Empty((await intruder.GetFromJsonAsync<List<AccountSummaryResponse>>("/api/accounts", Ct))!);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/accounts/{wizardId}", Ct)).StatusCode);
    }

    [Fact]
    public async Task ListAccounts_ShouldReturnLatestVersionOfEachUserWizard()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync(Ct);
        var first = ImportPayload.Create(out var firstWizardId);
        var second = ImportPayload.Create(out var secondWizardId);
        (await client.PostAsJsonAsync("/api/imports", first, Ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/imports", second, Ct)).EnsureSuccessStatusCode();
        first["wizard"]!["level"] = 51;
        (await client.PostAsJsonAsync("/api/imports", first, Ct)).EnsureSuccessStatusCode();

        var accounts = await client.GetFromJsonAsync<List<AccountSummaryResponse>>("/api/accounts", Ct);

        Assert.NotNull(accounts);
        Assert.Equal([firstWizardId, secondWizardId], accounts.Select(a => a.WizardId));
        Assert.Equal(2, accounts.Single(a => a.WizardId == firstWizardId).Version);
    }
}
