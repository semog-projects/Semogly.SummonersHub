using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Semogly.SummonersHub.Application.Accounts.ListRunes;
using Semogly.SummonersHub.Application.Accounts.OptimizerPresets;
using Semogly.SummonersHub.Application.Settings;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.IntegrationTests.Infrastructure;

namespace Semogly.SummonersHub.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class SettingsTests(ApiFixture fixture) : IAsyncLifetime
{
    private static readonly string[] SpeedFocus = ["Speed"];

    private static readonly string[] RtaTag = ["RTA"];

    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _client = await fixture.CreateAuthenticatedClientAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Preferences_ShouldDefaultToDarkAndBeSaved()
    {
        var initial = await _client.GetFromJsonAsync<PreferencesResponse>("/api/settings/preferences", ApiJson.Options, Ct);
        Assert.Equal(ThemePreference.Dark, initial!.Theme);

        var saved = await _client.PutAsJsonAsync("/api/settings/preferences", new { theme = "Light" }, ApiJson.Options, Ct);
        saved.EnsureSuccessStatusCode();

        var current = await _client.GetFromJsonAsync<PreferencesResponse>("/api/settings/preferences", ApiJson.Options, Ct);
        Assert.Equal(ThemePreference.Light, current!.Theme);
    }

    [Fact]
    public async Task Backup_ShouldBeRestoredIntoAnotherUserWithTheSameAccount()
    {
        // Usuário A: tema, preset e marcação numa runa
        var payload = ImportPayload.Create(out var wizardId);
        (await _client.PostAsJsonAsync("/api/imports", payload, Ct)).EnsureSuccessStatusCode();
        var runeId = payload.FirstRune()["id"]!.GetValue<long>();

        (await _client.PutAsJsonAsync("/api/settings/preferences", new { theme = "System" }, ApiJson.Options, Ct)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/optimizer-presets", new { name = "Violent VEL", criteria = new { focus = SpeedFocus } }, ApiJson.Options, Ct))
            .EnsureSuccessStatusCode();
        (await _client.PatchAsJsonAsync($"/api/accounts/{wizardId}/inventory-marks", new { kind = "Rune", itemIds = new[] { runeId }, locked = true, addTags = RtaTag }, ApiJson.Options, Ct))
            .EnsureSuccessStatusCode();

        var backup = await _client.GetFromJsonAsync<JsonObject>("/api/settings/backup", ApiJson.Options, Ct);
        Assert.NotNull(backup);
        Assert.Equal(SettingsBackup.FormatName, backup["format"]!.GetValue<string>());
        Assert.DoesNotContain("session", backup.ToJsonString(), StringComparison.OrdinalIgnoreCase);

        // Apaga a marcação no A: restaurar o próprio backup traz de volta
        (await _client.PatchAsJsonAsync($"/api/accounts/{wizardId}/inventory-marks", new { kind = "Rune", itemIds = new[] { runeId }, locked = false, removeTags = RtaTag }, ApiJson.Options, Ct))
            .EnsureSuccessStatusCode();
        var restored = await RestoreAsync(_client, backup);
        Assert.Equal(new RestoreSettingsResponse(Presets: 1, Marks: 1, SkippedPresets: 0, SkippedMarks: 0), restored);

        var rune = (await _client.GetFromJsonAsync<List<RuneListItem>>($"/api/accounts/{wizardId}/runes", ApiJson.Options, Ct))!
            .Single(r => r.Rune.RuneId == runeId);
        Assert.True(rune.Locked);
        Assert.Equal(["RTA"], rune.Tags);

        // Usuário B não é dono do invocador: recebe tema e preset, e as marcações são ignoradas
        using var other = await fixture.CreateAuthenticatedClientAsync(Ct);
        var fromOther = await RestoreAsync(other, backup);
        Assert.Equal(new RestoreSettingsResponse(Presets: 1, Marks: 0, SkippedPresets: 0, SkippedMarks: 1), fromOther);

        var otherPrefs = await other.GetFromJsonAsync<PreferencesResponse>("/api/settings/preferences", ApiJson.Options, Ct);
        Assert.Equal(ThemePreference.System, otherPrefs!.Theme);
        var otherPresets = await other.GetFromJsonAsync<List<OptimizerPresetResponse>>("/api/optimizer-presets", ApiJson.Options, Ct);
        Assert.Equal(["Violent VEL"], otherPresets!.Select(p => p.Name));
    }

    [Fact]
    public async Task Restore_ShouldRejectUnknownFilesAndInvalidItemsWithoutSavingAnything()
    {
        var notABackup = await _client.PostAsJsonAsync("/api/settings/backup", new { format = "outro", version = 1, exportedAt = DateTimeOffset.UnixEpoch }, ApiJson.Options, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, notABackup.StatusCode);

        // Primeiro preset válido, segundo com nome vazio: nenhum é gravado
        var invalid = new
        {
            format = SettingsBackup.FormatName,
            version = SettingsBackup.CurrentVersion,
            exportedAt = DateTimeOffset.UnixEpoch,
            optimizerPresets = new object[]
            {
                new { name = "Bom", criteria = new { focus = SpeedFocus } },
                new { name = "   ", criteria = new { focus = SpeedFocus } },
            },
        };
        var response = await _client.PostAsJsonAsync("/api/settings/backup", invalid, ApiJson.Options, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var presets = await _client.GetFromJsonAsync<List<OptimizerPresetResponse>>("/api/optimizer-presets", ApiJson.Options, Ct);
        Assert.Empty(presets!);
    }

    private static async Task<RestoreSettingsResponse> RestoreAsync(HttpClient client, JsonObject backup)
    {
        var response = await client.PostAsJsonAsync("/api/settings/backup", backup, ApiJson.Options, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RestoreSettingsResponse>(ApiJson.Options, Ct))!;
    }
}
