using System.Net;
using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.SharedBuilds;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class SharedBuildsTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task SharedBuild_ShouldBePublicAndFrozenOnItsImportVersion()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var lapisId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        var sharesUrl = $"/api/accounts/{wizardId}/monsters/{lapisId}/shares";

        var link = await CreateAsync(sharesUrl);
        Assert.Equal(22, link.Token.Length);
        Assert.Equal(1, link.Version);

        // Pedir de novo na mesma versão devolve o mesmo link
        Assert.Equal(link.Token, (await CreateAsync(sharesUrl)).Token);

        // Nova versão com o Lapis no nível 39: o link continua mostrando a versão 1
        payload["monsters"]![0]!["level"] = 39;
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        using var anonymous = fixture.CreateClient();
        var shared = await anonymous.GetFromJsonAsync<SharedBuildResponse>($"/api/shared/{link.Token}", ApiJson.Options, Ct);
        Assert.NotNull(shared);
        Assert.Equal("Lapis", shared.Monster.Name);
        Assert.Equal(40, shared.Monster.Level);
        Assert.Equal(6, shared.Monster.Runes.Count);

        // Na versão 2 sai outro link; a lista traz os dois, do mais novo para o mais antigo
        var second = await CreateAsync(sharesUrl);
        Assert.NotEqual(link.Token, second.Token);
        var links = await _client.GetFromJsonAsync<List<ShareLinkResponse>>(sharesUrl, ApiJson.Options, Ct);
        Assert.Equal([2, 1], links!.Select(l => l.Version));

        // Revogado, o link deixa de existir
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/shares/{link.Token}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/shared/{link.Token}", Ct)).StatusCode);
    }

    [Fact]
    public async Task ShareLinks_ShouldBelongToTheOwner()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var lapisId = payload["monsters"]![0]!["id"]!.GetValue<long>();
        var link = await CreateAsync($"/api/accounts/{wizardId}/monsters/{lapisId}/shares");
        using var intruder = await fixture.CreateAuthenticatedClientAsync(Ct);
        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsync($"/api/accounts/{wizardId}/monsters/{lapisId}/shares", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/shares/{link.Token}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync($"/api/accounts/{wizardId}/monsters/{lapisId}/shares", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync($"/api/accounts/{wizardId}/monsters/1/shares", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/shared/token-que-nao-existe-xx", Ct)).StatusCode);
    }

    private async Task<ShareLinkResponse> CreateAsync(string url)
    {
        var response = await _client.PostAsync(url, null, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShareLinkResponse>(ApiJson.Options, Ct))!;
    }
}
