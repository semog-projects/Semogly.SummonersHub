using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.ListArtifacts;
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
/// <param name="AssumeMaxLevel">Calcula cada runa em +15: principal no máximo e subs pelo ganho médio esperado.</param>
/// <param name="OptimizeArtifacts">Troca os artefatos pelos compatíveis cujo principal mais ajuda o foco.</param>
/// <param name="Maximums">Atributos totais máximos.</param>
/// <param name="Weights">Peso de cada atributo de foco (0,1 a 10); os ausentes valem 1.</param>
/// <param name="SimulateGrinds">Considera o grind lendário máximo em cada sub grindável.</param>
/// <param name="TeamSets">Sets de equipe do time (Fight, Determination, Enhance, Accuracy, Tolerance), um por set completo.</param>
/// <param name="SimulateGems">Considera trocar uma sub de cada runa por uma gem lendária.</param>
/// <param name="CompareWith">Build usada na comparação e mantida nos artefatos (<c>rta</c>, <c>arena</c>, <c>siege-{deck}</c>); nula = build normal.</param>
public sealed record OptimizeRunesRequest(
    IReadOnlyList<RuneSet> Sets,
    IReadOnlyDictionary<int, IReadOnlyList<int>>? MainStats,
    IReadOnlyList<PveStat> Focus,
    IReadOnlyList<StatRequirement>? Minimums,
    bool IncludeEquippedOnOthers,
    bool AssumeMaxLevel,
    bool OptimizeArtifacts = false,
    IReadOnlyList<StatCap>? Maximums = null,
    IReadOnlyDictionary<PveStat, decimal>? Weights = null,
    bool SimulateGrinds = false,
    IReadOnlyList<RuneSet>? TeamSets = null,
    bool SimulateGems = false,
    string? CompareWith = null)
{
    /// <summary>Sets de equipe informados (o time tem no máximo 5 monstros com até 3 sets cada).</summary>
    public const int MaxTeamSets = 15;

    public const decimal MinWeight = 0.1m;

    public const decimal MaxWeight = 10;
}

/// <param name="Current">Build atual do monstro, para comparação.</param>
public sealed record OptimizeRunesResponse(OptimizedBuildResult Current, IReadOnlyList<OptimizedBuildResult> Builds);

/// <param name="Score">Soma dos atributos de foco relativos à base (ver <see cref="RuneOptimizer.Score"/>).</param>
/// <param name="Runes">Uma runa por slot, com o monstro que a usa hoje (se alguma).</param>
/// <param name="Artifacts">Artefatos da build, com o monstro que os usa hoje (se algum).</param>
/// <param name="Gems">Gems simuladas na build (vazio sem simulação ou na build atual).</param>
public sealed record OptimizedBuildResult(
    decimal Score,
    StatValues Total,
    IReadOnlyList<RuneSet> ActiveSets,
    IReadOnlyList<RuneListItem> Runes,
    IReadOnlyList<ArtifactListItem> Artifacts,
    IReadOnlyList<SimulatedGemResult>? Gems = null);

/// <summary>Gem simulada: na runa <paramref name="RuneId"/>, a sub <paramref name="From"/> vira <paramref name="To"/> (códigos do jogo).</summary>
public sealed record SimulatedGemResult(long RuneId, int From, int To, int Value);

internal sealed class OptimizeRunesQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog,
    IInventoryMarkRepository marks) : IQueryHandler<OptimizeRunesQuery, OptimizeRunesResponse>
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
        var equipment = new EquipmentIndex(snapshot);
        var compared = equipment.FindBuild(monster.GameId, request.CompareWith);
        if (request.CompareWith is not null && compared is null)
        {
            return AccountErrors.BuildNotFound(request.CompareWith);
        }

        var currentRunes = equipment.RunesOf(monster.GameId, compared);
        var currentArtifacts = equipment.ArtifactsOf(monster.GameId, compared);
        var currentRuneIds = currentRunes.Select(r => r.GameId).ToHashSet();
        var currentArtifactIds = currentArtifacts.Select(a => a.GameId).ToHashSet();

        var optimizerRequest = new OptimizerRequest(
            request.Sets,
            (request.MainStats ?? new Dictionary<int, IReadOnlyList<int>>())
                .ToDictionary(e => e.Key, e => (IReadOnlyList<RuneStat>)[.. e.Value.Select(code => (RuneStat)code)]),
            request.Focus,
            request.Minimums ?? [],
            request.AssumeMaxLevel,
            Maximums: request.Maximums,
            Weights: request.Weights,
            SimulateGrinds: request.SimulateGrinds,
            TeamSets: request.TeamSets,
            SimulateGems: request.SimulateGems);

        var lookup = await InventoryMarkLookup.LoadAsync(marks, query.WizardId, cancellationToken);

        // Itens bloqueados ficam com quem os usa: só entram se já estiverem no próprio monstro (ou na build comparada)
        bool Available(long? equippedMonsterId, bool locked) =>
            equippedMonsterId == monster.GameId
            || (!locked && (equippedMonsterId is null || request.IncludeEquippedOnOthers));

        var pool = snapshot.Runes.Where(r => currentRuneIds.Contains(r.GameId) || Available(r.EquippedMonsterId, lookup.IsEffectivelyLocked(r)));

        var artifacts = request.OptimizeArtifacts
            ? RuneOptimizer.BestArtifacts(
                monster,
                catalog.Find(monster.MasterId)?.Archetype,
                snapshot.Artifacts.Where(a => currentArtifactIds.Contains(a.GameId) || Available(a.EquippedMonsterId, lookup.IsEffectivelyLocked(a))),
                request.Focus,
                request.Weights)
            : currentArtifacts;

        // O cálculo só considera artefatos equipados no monstro: os escolhidos entram como cópias equipadas nele
        var equippedArtifacts = artifacts
            .Select(a => new Artifact(a.GameId, a.Type, a.Element, a.Archetype, a.Rarity, a.OriginalRarity, a.Level, a.MainStat, a.Substats, monster.GameId))
            .ToList();

        var builds = RuneOptimizer.Optimize(monster, equippedArtifacts, pool, optimizerRequest);

        var monsters = EquipmentDetails.MonstersById(snapshot, catalog);
        var currentStats = equipment.StatsOf(monster, compared);

        var current = new OptimizedBuildResult(
            RuneOptimizer.Score(currentStats.Total, monster.BaseStats, request.Focus, request.Weights),
            currentStats.Total,
            currentStats.ActiveSets,
            [.. currentRunes.Select(r => ToItem(r, monsters, lookup))],
            [.. currentArtifacts.Select(a => ToItem(a, monsters, lookup))]);

        return new OptimizeRunesResponse(
            current,
            [.. builds.Select(b => new OptimizedBuildResult(
                b.Score,
                b.Stats.Total,
                b.Stats.ActiveSets,
                [.. b.Runes.Select(r => ToItem(r, monsters, lookup))],
                [.. artifacts.Select(a => ToItem(a, monsters, lookup))],
                [.. b.Gems.Select(g => new SimulatedGemResult(g.RuneId, (int)g.From, (int)g.To, g.Value))]))]);
    }

    private static RuneListItem ToItem(Rune rune, Dictionary<long, EquippedMonster> monsters, InventoryMarkLookup lookup) =>
        new(EquipmentDetails.ToDetail(rune), rune.EquippedMonsterId is { } id ? monsters[id] : null, lookup.IsLocked(rune), lookup.TagsOf(rune), lookup.IsLockedByMonster(rune.EquippedMonsterId));

    private static ArtifactListItem ToItem(Artifact artifact, Dictionary<long, EquippedMonster> monsters, InventoryMarkLookup lookup) =>
        new(EquipmentDetails.ToDetail(artifact), artifact.EquippedMonsterId is { } id ? monsters[id] : null, lookup.IsLocked(artifact), lookup.TagsOf(artifact), lookup.IsLockedByMonster(artifact.EquippedMonsterId));

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

        if (request.Weights is { } weights
            && weights.Any(w => !request.Focus.Contains(w.Key) || w.Value < OptimizeRunesRequest.MinWeight || w.Value > OptimizeRunesRequest.MaxWeight))
        {
            return AccountErrors.InvalidOptimization(
                $"Os pesos devem ser de atributos do foco, entre {OptimizeRunesRequest.MinWeight} e {OptimizeRunesRequest.MaxWeight}.");
        }

        if (request.Maximums is { } maximums
            && maximums.Any(max => (request.Minimums ?? []).Any(min => min.Stat == max.Stat && min.Min > max.Max)))
        {
            return AccountErrors.InvalidOptimization("O máximo de um atributo não pode ser menor que o mínimo.");
        }

        if (request.TeamSets is { } teamSets && (teamSets.Count > OptimizeRunesRequest.MaxTeamSets || teamSets.Any(s => !s.IsTeamSet())))
        {
            return AccountErrors.InvalidOptimization(
                $"Sets de equipe: até {OptimizeRunesRequest.MaxTeamSets}, só Fight, Determination, Enhance, Accuracy e Tolerance.");
        }

        if (request.MainStats is { } mainStats
            && (mainStats.Keys.Any(slot => slot is < 1 or > 6) || mainStats.Values.SelectMany(v => v).Any(code => !Enum.IsDefined((RuneStat)code))))
        {
            return AccountErrors.InvalidOptimization("Slot ou atributo principal inválido.");
        }

        return null;
    }
}
