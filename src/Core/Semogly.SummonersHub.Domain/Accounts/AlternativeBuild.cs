using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>Modo de jogo com equipamento próprio, separado da build normal do monstro.</summary>
public enum BuildKind
{
    /// <summary>Arena em tempo real (<c>world_arena_rune_equip_list</c>).</summary>
    Rta = 1,

    /// <summary>Defesa de Siege (<c>guildsiege_defense_deck_equip_list</c>), uma por deck.</summary>
    Siege = 2,

    /// <summary>Defesa da Arena (<c>defense_deck_info</c>).</summary>
    Arena = 3,
}

/// <summary>
/// Build de um monstro num modo com equipamento próprio. Guarda só os ids: as runas e artefatos são os do
/// mesmo snapshot (podem estar equipados em outro monstro na build normal). Slots sem item na build
/// alternativa usam o item da build normal.
/// </summary>
public sealed class AlternativeBuild
{
    public const int MaxRunes = 6;
    public const int MaxArtifacts = 2;

    // Construtor do EF Core
    private AlternativeBuild()
    {
        RuneIds = null!;
        ArtifactIds = null!;
    }

    /// <param name="deckId">Id do deck de Siege; 0 nos modos com um só deck.</param>
    public AlternativeBuild(long monsterId, BuildKind kind, long deckId, IEnumerable<long> runeIds, IEnumerable<long> artifactIds)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("Modo de build inválido.");
        }

        MonsterId = monsterId;
        Kind = kind;
        DeckId = kind == BuildKind.Siege ? deckId : 0;
        RuneIds = [.. runeIds.Distinct()];
        ArtifactIds = [.. artifactIds.Distinct()];

        if (RuneIds.Count > MaxRunes || ArtifactIds.Count > MaxArtifacts)
        {
            throw new DomainException("A build alternativa tem runas ou artefatos demais.");
        }
    }

    public long MonsterId { get; private set; }

    public BuildKind Kind { get; private set; }

    public long DeckId { get; private set; }

    public List<long> RuneIds { get; private set; }

    public List<long> ArtifactIds { get; private set; }

    /// <summary>Chave estável usada pela API: <c>rta</c>, <c>arena</c> ou <c>siege-{deck}</c>.</summary>
    public string Key => Kind switch
    {
        BuildKind.Siege => $"siege-{DeckId}",
        _ => Kind.ToString().ToLowerInvariant(),
    };

    /// <summary>Runas da build: as da build alternativa substituem as normais no mesmo slot.</summary>
    public IReadOnlyList<Rune> ResolveRunes(IEnumerable<Rune> normal, IReadOnlyDictionary<long, Rune> runesById)
    {
        var bySlot = normal.GroupBy(r => r.Slot).ToDictionary(g => g.Key, g => g.First());
        foreach (var rune in RuneIds.Select(id => runesById.GetValueOrDefault(id)).OfType<Rune>())
        {
            bySlot[rune.Slot] = rune;
        }

        return [.. bySlot.Values.OrderBy(r => r.Slot)];
    }

    /// <summary>Artefatos da build: os da build alternativa substituem os normais do mesmo tipo.</summary>
    public IReadOnlyList<Artifact> ResolveArtifacts(IEnumerable<Artifact> normal, IReadOnlyDictionary<long, Artifact> artifactsById)
    {
        var byType = normal.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.First());
        foreach (var artifact in ArtifactIds.Select(id => artifactsById.GetValueOrDefault(id)).OfType<Artifact>())
        {
            byType[artifact.Type] = artifact;
        }

        return [.. byType.Values.OrderBy(a => a.Type)];
    }
}
