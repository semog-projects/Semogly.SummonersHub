using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;

/// <summary>Monstro do último import com atributos, skills, runas e artefatos equipados.</summary>
public sealed record GetMonsterDetailQuery(long WizardId, long MonsterId) : IQuery<MonsterDetailResponse>;

public sealed record MonsterDetailResponse(
    long MonsterId,
    int MasterId,
    string? Name,
    Element Element,
    Archetype? Archetype,
    int Stars,
    int Level,
    int? AwakenLevel,
    StatValues Base,
    StatValues Total,
    IReadOnlyList<RuneSet> ActiveSets,
    IReadOnlyList<MonsterSkill> Skills,
    IReadOnlyList<RuneDetail> Runes,
    IReadOnlyList<ArtifactDetail> Artifacts);

/// <summary>
/// Atributos de runa e efeitos de artefato vão com o código numérico do jogo (o mesmo do export),
/// que o front traduz com a tabela de rótulos.
/// </summary>
public sealed record StatCode(int Type, int Value);

public sealed record RuneSubstatDetail(int Type, int Value, bool IsGemmed, int GrindValue);

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
    IReadOnlyList<RuneSubstatDetail> Substats);

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

internal sealed class GetMonsterDetailQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<GetMonsterDetailQuery, MonsterDetailResponse>
{
    public async Task<Result<MonsterDetailResponse>> HandleAsync(GetMonsterDetailQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var monster = result.Value.Monsters.FirstOrDefault(m => m.GameId == query.MonsterId);
        if (monster is null)
        {
            return AccountErrors.MonsterNotFound(query.MonsterId);
        }

        var equipment = new EquipmentIndex(result.Value);
        var stats = equipment.StatsOf(monster);
        var definition = catalog.Find(monster.MasterId);

        return new MonsterDetailResponse(
            monster.GameId,
            monster.MasterId,
            definition?.Name,
            monster.Element,
            definition?.Archetype,
            monster.Stars,
            monster.Level,
            definition?.AwakenLevel,
            stats.Base,
            stats.Total,
            stats.ActiveSets,
            monster.Skills,
            [.. equipment.RunesOf(monster.GameId).OrderBy(r => r.Slot).Select(ToDetail)],
            [.. equipment.ArtifactsOf(monster.GameId).OrderBy(a => a.Type).Select(ToDetail)]);
    }

    private static RuneDetail ToDetail(Rune rune) => new(
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
        [.. rune.Substats.Select(s => new RuneSubstatDetail((int)s.Stat, s.Value, s.IsGemmed, s.GrindValue))]);

    private static ArtifactDetail ToDetail(Artifact artifact) => new(
        artifact.GameId,
        artifact.Type,
        artifact.Element,
        artifact.Archetype,
        artifact.Level,
        artifact.Rarity,
        artifact.OriginalRarity,
        new ArtifactStatCode((int)artifact.MainStat.Effect, artifact.MainStat.Value),
        [.. artifact.Substats.Select(s => new ArtifactSubstatDetail((int)s.Effect, s.Value, s.Upgrades))]);
}
