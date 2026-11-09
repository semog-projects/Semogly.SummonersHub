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
    IReadOnlyList<ArtifactDetail> Artifacts,
    decimal? AverageRuneEfficiency,
    bool Locked = false,
    bool IsManual = false);

internal sealed class GetMonsterDetailQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog,
    IInventoryMarkRepository marks) : IQueryHandler<GetMonsterDetailQuery, MonsterDetailResponse>
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

        var lookup = await InventoryMarkLookup.LoadAsync(marks, query.WizardId, cancellationToken);
        return MonsterDetailBuilder.Build(result.Value, monster, catalog) with
        {
            Locked = lookup.IsMonsterLocked(monster.GameId),
            IsManual = ManualItem.IsManual(monster.GameId),
        };
    }
}

/// <summary>Detalhe de um monstro a partir de qualquer snapshot (último import ou o de uma build compartilhada).</summary>
internal static class MonsterDetailBuilder
{
    public static MonsterDetailResponse Build(AccountSnapshot snapshot, Monster monster, IMonsterCatalog catalog)
    {
        var equipment = new EquipmentIndex(snapshot);
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
            [.. equipment.RunesOf(monster.GameId).OrderBy(r => r.Slot).Select(EquipmentDetails.ToDetail)],
            [.. equipment.ArtifactsOf(monster.GameId).OrderBy(a => a.Type).Select(EquipmentDetails.ToDetail)],
            equipment.AverageRuneEfficiencyOf(monster.GameId));
    }
}
