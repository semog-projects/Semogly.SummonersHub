using System.Collections.Frozen;
using System.Text.Json;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Infrastructure.GameData;

/// <summary>
/// Bestiário carregado do <c>monsters.json</c> embarcado no assembly (gerado pela ferramenta GameDataSync
/// a partir da API do SWARFARM). Dado estático e pequeno: fica em memória, sem banco nem chamadas externas.
/// </summary>
internal sealed class EmbeddedMonsterCatalog : IMonsterCatalog
{
    internal const string ResourceName = "Semogly.SummonersHub.GameData.monsters.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Lazy<FrozenDictionary<int, MonsterDefinition>> _monsters = new(Load);

    public MonsterDefinition? Find(int com2usId) => _monsters.Value.GetValueOrDefault(com2usId);

    internal int Count => _monsters.Value.Count;

    private static FrozenDictionary<int, MonsterDefinition> Load()
    {
        using var stream = typeof(EmbeddedMonsterCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso embarcado '{ResourceName}' não encontrado.");

        var entries = JsonSerializer.Deserialize<List<MonsterEntry>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("monsters.json vazio ou inválido.");

        return entries.ToFrozenDictionary(entry => entry.Com2usId, ToDefinition);
    }

    private static MonsterDefinition ToDefinition(MonsterEntry entry) => new(
        entry.Com2usId,
        entry.FamilyId,
        entry.Name,
        Enum.Parse<Element>(entry.Element, ignoreCase: true),
        Enum.TryParse<Archetype>(entry.Archetype, ignoreCase: true, out var archetype) ? archetype : Archetype.None,
        entry.NaturalStars,
        entry.AwakenLevel,
        entry.ImageFilename ?? string.Empty,
        entry.IsHomunculus);

    private sealed record MonsterEntry(
        int Com2usId,
        int FamilyId,
        string Name,
        string Element,
        string Archetype,
        int NaturalStars,
        int AwakenLevel,
        string? ImageFilename,
        bool IsHomunculus);
}
