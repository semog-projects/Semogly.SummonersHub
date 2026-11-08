using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.ListRunes;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.OptimizeRunes;

/// <summary>Builds de runas para um monstro do último import (não altera nada; só sugere).</summary>
public sealed record OptimizeRunesQuery(long WizardId, long MonsterId, OptimizeRunesRequest Request) : IQuery<OptimizeRunesResponse>;

/// <param name="Sets">Sets obrigatórios; repetir o set pede mais de um (ex.: Energy, Energy, Energy).</param>
/// <param name="MainStats">Principais aceitos por slot, com o código do jogo (ex.: { "2": [8] } = VEL no slot 2).</param>
/// <param name="Focus">Até 5 atributos a maximizar.</param>
/// <param name="Minimums">Atributos totais mínimos.</param>
/// <param name="IncludeEquippedOnOthers">Também usa runas equipadas em outros monstros.</param>
/// <param name="AssumeMaxLevel">Calcula o principal como se a runa estivesse em +15.</param>
public sealed record OptimizeRunesRequest(
    IReadOnlyList<RuneSet> Sets,
    IReadOnlyDictionary<int, IReadOnlyList<int>>? MainStats,
    IReadOnlyList<PveStat> Focus,
    IReadOnlyList<StatRequirement>? Minimums,
    bool IncludeEquippedOnOthers,
    bool AssumeMaxLevel);

/// <param name="Current">Build atual do monstro, para comparação.</param>
public sealed record OptimizeRunesResponse(OptimizedBuildResult Current, IReadOnlyList<OptimizedBuildResult> Builds);

/// <param name="Score">Soma dos atributos de foco relativos à base (ver <see cref="RuneOptimizer.Score"/>).</param>
/// <param name="Runes">Uma runa por slot, com o monstro que a usa hoje (se alguma).</param>
public sealed record OptimizedBuildResult(
    decimal Score,
    StatValues Total,
    IReadOnlyList<RuneSet> ActiveSets,
    IReadOnlyList<RuneListItem> Runes);

internal sealed class OptimizeRunesQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<OptimizeRunesQuery, OptimizeRunesResponse>
{
    public async Task<Result<OptimizeRunesResponse>> HandleAsync(OptimizeRunesQuery query, CancellationToken cancellationToken)
    {
        if (Validate(query.Request) is { } error)
        {
            return error;
        }

        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var snapshot = result.Value;
        var monster = snapshot.Monsters.FirstOrDefault(m => m.GameId == query.MonsterId);
        if (monster is null)
        {
            return AccountErrors.MonsterNotFound(query.MonsterId);
        }

        var request = query.Request;
        var optimizerRequest = new OptimizerRequest(
            request.Sets,
            (request.MainStats ?? new Dictionary<int, IReadOnlyList<int>>())
                .ToDictionary(e => e.Key, e => (IReadOnlyList<RuneStat>)[.. e.Value.Select(code => (RuneStat)code)]),
            request.Focus,
            request.Minimums ?? [],
            request.AssumeMaxLevel);

        var pool = snapshot.Runes.Where(r =>
            r.EquippedMonsterId is null || r.EquippedMonsterId == monster.GameId || request.IncludeEquippedOnOthers);

        var builds = RuneOptimizer.Optimize(monster, snapshot.Artifacts, pool, optimizerRequest);

        var monsters = EquipmentDetails.MonstersById(snapshot, catalog);
        var equipment = new EquipmentIndex(snapshot);
        var currentStats = equipment.StatsOf(monster);

        var current = new OptimizedBuildResult(
            RuneOptimizer.Score(currentStats.Total, monster.BaseStats, request.Focus),
            currentStats.Total,
            currentStats.ActiveSets,
            [.. equipment.RunesOf(monster.GameId).OrderBy(r => r.Slot).Select(r => ToItem(r, monsters))]);

        return new OptimizeRunesResponse(
            current,
            [.. builds.Select(b => new OptimizedBuildResult(
                b.Score,
                b.Stats.Total,
                b.Stats.ActiveSets,
                [.. b.Runes.Select(r => ToItem(r, monsters))]))]);
    }

    private static RuneListItem ToItem(Rune rune, Dictionary<long, EquippedMonster> monsters) =>
        new(EquipmentDetails.ToDetail(rune), rune.EquippedMonsterId is { } id ? monsters[id] : null);

    private static Error? Validate(OptimizeRunesRequest request)
    {
        if (request.Focus.Count is 0 or > RuneOptimizer.MaxFocusStats)
        {
            return AccountErrors.InvalidOptimization($"Escolha de 1 a {RuneOptimizer.MaxFocusStats} atributos de foco.");
        }

        if (request.Sets.Any(s => s.RequiredPieces() == 0))
        {
            return AccountErrors.InvalidOptimization("Intangible e Immemorial não formam set próprio.");
        }

        if (request.Sets.Sum(s => s.RequiredPieces()) > 6)
        {
            return AccountErrors.InvalidOptimization("Os sets pedidos precisam de mais de 6 runas.");
        }

        if (request.MainStats is { } mainStats
            && (mainStats.Keys.Any(slot => slot is < 1 or > 6) || mainStats.Values.SelectMany(v => v).Any(code => !Enum.IsDefined((RuneStat)code))))
        {
            return AccountErrors.InvalidOptimization("Slot ou atributo principal inválido.");
        }

        return null;
    }
}
