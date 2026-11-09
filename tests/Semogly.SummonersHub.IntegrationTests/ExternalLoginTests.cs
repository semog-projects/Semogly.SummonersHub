using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

/// <summary>Login com Google: só o lado do site (o Google em si não é chamado).</summary>
[Collection(ApiTestGroup.Name)]
public sealed class ExternalLoginTests(ApiFixture fixture)
{
    private static readonly string[] GoogleOnly = ["Google"];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Providers_ShouldListGoogleWhenConfigured()
    {
        using var client = fixture.CreateClient();

        var providers = await client.GetFromJsonAsync<string[]>("/api/auth/providers", Ct);

        Assert.Equal(GoogleOnly, providers);
    }

    [Fact]
    public async Task Challenge_ShouldRedirectToGoogleWithTheCallbackUnderApi()
    {
        using var client = fixture.CreateNonRedirectingClient();

        var response = await client.GetAsync("/api/auth/external/google?returnUrl=/contas", Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("accounts.google.com", location.Host);
        Assert.Contains(Uri.EscapeDataString("/api/auth/signin-google"), location.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Challenge_ShouldRejectUnknownProviders()
    {
        using var client = fixture.CreateNonRedirectingClient();

        var response = await client.GetAsync("/api/auth/external/facebook", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Callback_WithoutGoogleResult_ShouldGoBackToLoginWithAnError()
    {
        using var client = fixture.CreateNonRedirectingClient();

        var response = await client.GetAsync("/api/auth/external/callback?returnUrl=/contas", Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?erro=failed", response.Headers.Location!.OriginalString);
    }
}
