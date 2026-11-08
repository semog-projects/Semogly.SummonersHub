using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

public sealed class Artifact
{
    // Construtor do EF Core
    private Artifact()
    {
        MainStat = null!;
        Substats = null!;
    }

    public Artifact(
        long gameId,
        ArtifactType type,
        Element? element,
        Archetype? archetype,
        int rarity,
        int originalRarity,
        int level,
        ArtifactStatValue mainStat,
        IReadOnlyList<ArtifactSubstat> substats,
        long? equippedMonsterId)
    {
        GameId = gameId;
        Type = type;
        Element = element;
        Archetype = archetype;
        Rarity = rarity;
        OriginalRarity = originalRarity;
        Level = level;
        MainStat = mainStat;
        Substats = [.. substats];
        EquippedMonsterId = equippedMonsterId;
    }

    /// <summary>Id do artefato no jogo (<c>rid</c>).</summary>
    public long GameId { get; private set; }

    public ArtifactType Type { get; private set; }

    /// <summary>Preenchido nos artefatos de elemento.</summary>
    public Element? Element { get; private set; }

    /// <summary>Preenchido nos artefatos de arquétipo.</summary>
    public Archetype? Archetype { get; private set; }

    public int Rarity { get; private set; }

    public int OriginalRarity { get; private set; }

    public int Level { get; private set; }

    public ArtifactStatValue MainStat { get; private set; }

    public List<ArtifactSubstat> Substats { get; private set; }

    public long? EquippedMonsterId { get; private set; }
}

/// <remarks>Valores de artefato podem ser fracionários (ex.: dano adicional de 0,2% do HP).</remarks>
public sealed record ArtifactStatValue(ArtifactEffect Effect, decimal Value);

public sealed record ArtifactSubstat(ArtifactEffect Effect, decimal Value, int Upgrades);
