using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts;

/// <summary>
/// Atributos de runa e efeitos de artefato vão com o código numérico do jogo (o mesmo do export),
/// que o front traduz com a tabela de rótulos.
/// </summary>
public sealed record StatCode(int Type, int Value);

public sealed record RuneSubstatDetail(int Type, int Value, bool IsGemmed, int GrindValue);

/// <param name="Efficiency">Eficiência atual em %, ver <see cref="RuneEfficiency"/>.</param>
/// <param name="MaxEfficiency">Eficiência com roll máximo nos upgrades que faltam até o +12, em %.</param>
public sealed record RuneDetail(
    long RuneId,
    int Slot,
    int Stars,
    bool IsAncient,
    RuneSet Set,
    int Level,
    int Rarity,
    int OriginalRarity,
    StatCode MainStat,
    StatCode? InnateStat,
    IReadOnlyList<RuneSubstatDetail> Substats,
    decimal Efficiency,
    decimal MaxEfficiency);

public sealed record ArtifactStatCode(int Type, decimal Value);

public sealed record ArtifactSubstatDetail(int Type, decimal Value, int Upgrades);

public sealed record ArtifactDetail(
    long ArtifactId,
    ArtifactType Type,
    Element? Element,
    Archetype? Archetype,
    int Level,
    int Rarity,
    int OriginalRarity,
    ArtifactStatCode MainStat,
    IReadOnlyList<ArtifactSubstatDetail> Substats);

/// <summary>Monstro que equipa uma runa ou artefato.</summary>
public sealed record EquippedMonster(long MonsterId, int MasterId, string? Name, Element Element);

internal static class EquipmentDetails
{
    public static RuneDetail ToDetail(Rune rune)
    {
        var efficiency = RuneEfficiency.Calculate(rune);

        return new RuneDetail(
            rune.GameId,
            rune.Slot,
            rune.Stars,
            rune.IsAncient,
            rune.Set,
            rune.Level,
            rune.Rarity,
            rune.OriginalRarity,
            new StatCode((int)rune.MainStat.Stat, rune.MainStat.Value),
            rune.InnateStat is { } innate ? new StatCode((int)innate.Stat, innate.Value) : null,
            [.. rune.Substats.Select(s => new RuneSubstatDetail((int)s.Stat, s.Value, s.IsGemmed, s.GrindValue))],
            efficiency.Current,
            efficiency.Max);
    }

    public static ArtifactDetail ToDetail(Artifact artifact) => new(
        artifact.GameId,
        artifact.Type,
        artifact.Element,
        artifact.Archetype,
        artifact.Level,
        artifact.Rarity,
        artifact.OriginalRarity,
        new ArtifactStatCode((int)artifact.MainStat.Effect, artifact.MainStat.Value),
        [.. artifact.Substats.Select(s => new ArtifactSubstatDetail((int)s.Effect, s.Value, s.Upgrades))]);

    /// <summary>Monstros do snapshot por id, com o nome do bestiário, para indicar quem equipa cada item.</summary>
    public static Dictionary<long, EquippedMonster> MonstersById(AccountSnapshot snapshot, IMonsterCatalog catalog) =>
        snapshot.Monsters.ToDictionary(
            m => m.GameId,
            m => new EquippedMonster(m.GameId, m.MasterId, catalog.Find(m.MasterId)?.Name, m.Element));
}
