using System.Text.Json;
using System.Text.Json.Serialization;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Infrastructure.GameData;

/// <summary>
/// Andar mais alto de cada dungeon, do <c>dungeons.json</c> embarcado (gerado pela ferramenta GameDataSync
/// a partir da API do SWARFARM).
/// </summary>
internal sealed class EmbeddedDungeonCatalog : IDungeonCatalog
{
    internal const string ResourceName = "Semogly.SummonersHub.GameData.dungeons.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Lazy<Dictionary<Dungeon, DungeonStats>> _dungeons = new(Load);

    public DungeonStats? Find(Dungeon dungeon) => _dungeons.Value.GetValueOrDefault(dungeon);

    internal IReadOnlyCollection<DungeonStats> All() => _dungeons.Value.Values;

    private static Dictionary<Dungeon, DungeonStats> Load()
    {
        using var stream = typeof(EmbeddedDungeonCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso embarcado '{ResourceName}' não encontrado.");

        var entries = JsonSerializer.Deserialize<List<DungeonStats>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("dungeons.json vazio ou inválido.");

        return entries.ToDictionary(d => d.Dungeon);
    }
}
