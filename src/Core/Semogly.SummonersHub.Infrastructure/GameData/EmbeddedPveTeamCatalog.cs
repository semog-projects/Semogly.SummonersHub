using System.Text.Json;
using System.Text.Json.Serialization;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Infrastructure.GameData;

/// <summary>
/// Times de referência do <c>pve-teams.json</c> embarcado no assembly. É um ponto de partida editável:
/// o meta muda com os patches, então o arquivo deve ser revisado quando o jogo mudar.
/// </summary>
internal sealed class EmbeddedPveTeamCatalog : IPveTeamCatalog
{
    internal const string ResourceName = "Semogly.SummonersHub.GameData.pve-teams.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Lazy<IReadOnlyList<PveTeam>> _teams = new(Load);

    public IReadOnlyList<PveTeam> All() => _teams.Value;

    private static List<PveTeam> Load()
    {
        using var stream = typeof(EmbeddedPveTeamCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso embarcado '{ResourceName}' não encontrado.");

        var entries = JsonSerializer.Deserialize<List<TeamEntry>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("pve-teams.json vazio ou inválido.");

        return [.. entries.Select(entry => new PveTeam(
            entry.Id,
            entry.Dungeon,
            entry.Name,
            entry.Notes,
            [.. entry.Slots.Select(slot => new PveTeamSlot(
                slot.Monster,
                slot.Element,
                slot.Role,
                [.. (slot.Min ?? []).Select(min => new StatRequirement(Enum.Parse<PveStat>(min.Key, ignoreCase: true), min.Value))],
                slot.Debuffer))]))];
    }

    private sealed record TeamEntry(string Id, Dungeon Dungeon, string Name, string? Notes, List<SlotEntry> Slots);

    private sealed record SlotEntry(string Monster, Element Element, string Role, Dictionary<string, int>? Min, bool Debuffer);
}
