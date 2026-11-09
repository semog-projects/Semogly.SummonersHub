using System.Text.Json.Serialization;
using Semogly.SummonersHub.Application.Abstractions.Messaging;

namespace Semogly.SummonersHub.Application.Accounts.ImportAccount;

/// <summary>
/// Conta exportada pelo SW Exporter, já reduzida e sanitizada no navegador
/// (mesmo contrato de <c>AccountImport</c> em src/Apps/web/src/features/import/types.ts).
/// </summary>
public sealed record ImportAccountCommand(
    int SchemaVersion,
    DateTimeOffset? ExportedAt,
    ImportWizard Wizard,
    IReadOnlyList<ImportMonster> Monsters,
    IReadOnlyList<ImportRune> Runes,
    IReadOnlyList<ImportArtifact> Artifacts,
    IReadOnlyList<ImportCraftItem> CraftItems,
    // Fora do JSON quando ausente: imports antigos (sem builds) mantêm o mesmo hash de conteúdo
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ImportBuild>? Builds = null) : ICommand<ImportAccountResponse>
{
    public const int SupportedSchemaVersion = 1;
}

public sealed record ImportWizard(long Id, string Name, int Level, int? ServerId);

public sealed record ImportMonster(
    long Id,
    int MasterId,
    int Level,
    int Stars,
    int Attribute,
    ImportBaseStats BaseStats,
    IReadOnlyList<ImportSkill> Skills,
    bool IsHomunculus,
    DateTime? CreatedAt);

public sealed record ImportBaseStats(
    int Hp,
    int Attack,
    int Defense,
    int Speed,
    int Resistance,
    int Accuracy,
    int CriticalRate,
    int CriticalDamage);

public sealed record ImportSkill(int Id, int Level);

public sealed record ImportStat(int Type, decimal Value);

public sealed record ImportRuneSubstat(int Type, int Value, bool IsGemmed, int GrindValue);

public sealed record ImportRune(
    long Id,
    int Slot,
    int Stars,
    bool IsAncient,
    int SetId,
    int Level,
    int Rarity,
    int OriginalRarity,
    ImportStat MainStat,
    ImportStat? InnateStat,
    IReadOnlyList<ImportRuneSubstat> Substats,
    long? EquippedMonsterId);

public sealed record ImportArtifactSubstat(int Type, decimal Value, int Upgrades);

public sealed record ImportArtifact(
    long Id,
    int Type,
    int Attribute,
    int Archetype,
    int Rarity,
    int OriginalRarity,
    int Level,
    ImportStat MainStat,
    IReadOnlyList<ImportArtifactSubstat> Substats,
    long? EquippedMonsterId);

/// <summary>Build de RTA (1), Siege (2) ou Arena (3); ver <c>BuildKind</c>.</summary>
/// <param name="DeckId">Id do deck de Siege; 0 nos demais modos.</param>
public sealed record ImportBuild(long MonsterId, int Kind, long DeckId, IReadOnlyList<long> RuneIds, IReadOnlyList<long> ArtifactIds);

public sealed record ImportCraftItem(long Id, int CraftType, int CraftTypeId, int Amount);

/// <param name="Created">Falso quando o mesmo conteúdo já tinha sido importado (nenhuma versão nova).</param>
public sealed record ImportAccountResponse(
    Guid SnapshotId,
    long WizardId,
    int Version,
    bool Created,
    int Monsters,
    int Runes,
    int Artifacts,
    int CraftItems);
