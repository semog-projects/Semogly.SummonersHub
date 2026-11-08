using System.Net.Http.Json;
using Semogly.SummonersHub.Application.Accounts.ListArtifacts;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class ArtifactsTests(ApiFixture fixture) : IAsyncLifetime
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
    public async Task ListArtifacts_ShouldReturnEveryArtifactWithOwner()
    {
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();

        var artifacts = await _client.GetFromJsonAsync<List<ArtifactListItem>>($"/api/accounts/{wizardId}/artifacts", ApiJson.Options, Ct);

        Assert.NotNull(artifacts);

        // Elemento antes de arquétipo; dentro do tipo, da raridade maior para a menor
        Assert.Equal([1174341733L, 1164104861L, 1176211402L, 1146719736L], artifacts.Select(a => a.Artifact.ArtifactId));

        var waterElement = artifacts[0];
        Assert.Equal(ArtifactType.Element, waterElement.Artifact.Type);
        Assert.Equal(Element.Water, waterElement.Artifact.Element);
        Assert.Equal((int)ArtifactEffect.AdditionalDamageBySpeed, waterElement.Artifact.Substats[0].Type);
        Assert.Equal("Lapis", waterElement.EquippedOn?.Name);

        Assert.Null(artifacts[1].EquippedOn);
    }
}
