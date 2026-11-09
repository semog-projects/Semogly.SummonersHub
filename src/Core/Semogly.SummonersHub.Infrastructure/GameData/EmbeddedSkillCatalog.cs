using System.Collections.Frozen;
using System.Text.Json;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Infrastructure.GameData;

/// <summary>Habilidades do <c>skills.json</c> embarcado (gerado pela ferramenta GameDataSync a partir do SWARFARM).</summary>
internal sealed class EmbeddedSkillCatalog : ISkillCatalog
{
    internal const string ResourceName = "Semogly.SummonersHub.GameData.skills.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Lazy<FrozenDictionary<int, SkillDefinition>> _skills = new(Load);

    public SkillDefinition? Find(int skillId) => _skills.Value.GetValueOrDefault(skillId);

    internal int Count => _skills.Value.Count;

    private static FrozenDictionary<int, SkillDefinition> Load()
    {
        using var stream = typeof(EmbeddedSkillCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso embarcado '{ResourceName}' não encontrado.");

        var skills = JsonSerializer.Deserialize<List<SkillDefinition>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("skills.json vazio ou inválido.");

        return skills.ToFrozenDictionary(s => s.Id);
    }
}
