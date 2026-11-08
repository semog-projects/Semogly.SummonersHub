using System.Text.Json.Nodes;

namespace Semogly.SummonersHub.IntegrationTests.Infrastructure;

/// <summary>
/// Payload de import gerado pelo parser do front (Fixtures/account-import.json). Cada chamada devolve uma
/// cópia com outro id de invocador, para isolar os testes que compartilham o mesmo banco.
/// </summary>
internal static class ImportPayload
{
    private static readonly string Template = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "account-import.json"));

    private static long _nextWizardId = 90_000_000;

    public static JsonObject Create(out long wizardId)
    {
        wizardId = Interlocked.Increment(ref _nextWizardId);

        var payload = JsonNode.Parse(Template)!.AsObject();
        payload["wizard"]!["id"] = wizardId;
        return payload;
    }

    public static JsonObject FirstRune(this JsonObject payload) => payload["runes"]![0]!.AsObject();
}
