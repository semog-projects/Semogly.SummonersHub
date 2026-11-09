using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;

/// <summary>Monstro do último import com atributos, skills, runas e artefatos equipados.</summary>
/// <param name="Build">Chave da build alternativa (<c>rta</c>, <c>arena</c>, <c>siege-{deck}</c>); nula = build normal.</param>
public sealed record GetMonsterDetailQuery(long WizardId, long MonsterId, string? Build = null) : IQuery<MonsterDetailResponse>;

/// <summary>Build de RTA/Siege/Arena disponível para o monstro.</summary>
/// <param name="Key">Valor a enviar em <c>build</c> para ver o monstro com essa build.</param>
/// <param name="Number">Número da defesa de Siege na conta (1, 2...); 0 nos demais modos.</param>
public sealed record MonsterBuildOption(string Key, BuildKind Kind, int Number);

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
    bool IsManual = false,
    IReadOnlyList<MonsterBuildOption>? Builds = null,
    string? SelectedBuild = null);

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

        var equipment = new EquipmentIndex(result.Value);
        var build = equipment.FindBuild(monster.GameId, query.Build);
        if (query.Build is not null && build is null)
        {
            return AccountErrors.BuildNotFound(query.Build);
        }

        var lookup = await InventoryMarkLookup.LoadAsync(marks, query.WizardId, cancellationToken);
        return MonsterDetailBuilder.Build(result.Value, monster, catalog, equipment, build) with
        {
            Locked = lookup.IsMonsterLocked(monster.GameId),
            IsManual = ManualItem.IsManual(monster.GameId),
        };
    }
}

/// <summary>Detalhe de um monstro a partir de qualquer snapshot (último import ou o de uma build compartilhada).</summary>
internal static class MonsterDetailBuilder
{
    public static MonsterDetailResponse Build(
        AccountSnapshot snapshot,
        Monster monster,
        IMonsterCatalog catalog,
        EquipmentIndex? equipment = null,
        AlternativeBuild? build = null)
    {
        equipment ??= new EquipmentIndex(snapshot);
        var stats = equipment.StatsOf(monster, build);
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
            [.. equipment.RunesOf(monster.GameId, build).Select(EquipmentDetails.ToDetail)],
            [.. equipment.ArtifactsOf(monster.GameId, build).Select(EquipmentDetails.ToDetail)],
            equipment.AverageRuneEfficiencyOf(monster.GameId, build),
            Builds: [.. equipment.BuildsOf(monster.GameId).Select(b => new MonsterBuildOption(b.Key, b.Kind, equipment.NumberOf(b)))],
            SelectedBuild: build?.Key);
    }
}
