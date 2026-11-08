using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.GetMonsterStats;

/// <summary>Atributos totais de um monstro no último import do invocador.</summary>
public sealed record GetMonsterStatsQuery(long WizardId, long MonsterId) : IQuery<MonsterStatsResponse>;

public sealed record MonsterStatsResponse(
    long MonsterId,
    int MasterId,
    string? Name,
    int Stars,
    int Level,
    StatValues Base,
    StatValues Total,
    IReadOnlyList<RuneSet> ActiveSets);

internal sealed class GetMonsterStatsQueryHandler(IAccountSnapshotRepository repository, IMonsterCatalog catalog)
    : IQueryHandler<GetMonsterStatsQuery, MonsterStatsResponse>
{
    public async Task<Result<MonsterStatsResponse>> HandleAsync(GetMonsterStatsQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetLatestAsync(query.WizardId, cancellationToken);
        if (snapshot is null)
        {
            return AccountErrors.NotFound(query.WizardId);
        }

        var monster = snapshot.Monsters.FirstOrDefault(m => m.GameId == query.MonsterId);
        if (monster is null)
        {
            return AccountErrors.MonsterNotFound(query.MonsterId);
        }

        var stats = MonsterStatsCalculator.Calculate(monster, snapshot.Runes, snapshot.Artifacts);

        return new MonsterStatsResponse(
            monster.GameId,
            monster.MasterId,
            catalog.Find(monster.MasterId)?.Name,
            monster.Stars,
            monster.Level,
            stats.Base,
            stats.Total,
            stats.ActiveSets);
    }
}
