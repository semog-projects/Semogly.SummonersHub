using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.CompareSnapshots;

/// <summary>
/// O que mudou entre duas versões do invocador. <c>From</c> é a versão de referência e <c>To</c> a comparada;
/// normalmente a anterior e a mais recente, mas qualquer par de versões diferentes é aceito.
/// </summary>
public sealed record CompareSnapshotsQuery(long WizardId, int From, int To) : IQuery<SnapshotComparisonResponse>
{
    /// <summary>Quantas runas novas vão na lista de destaques.</summary>
    public const int TopNewRunesSize = 10;
}

public sealed record SnapshotComparisonResponse(
    SnapshotRef From,
    SnapshotRef To,
    Change<int> WizardLevel,
    Change<int> Monsters,
    IReadOnlyList<MonsterChangeItem> AddedMonsters,
    IReadOnlyList<MonsterChangeItem> RemovedMonsters,
    IReadOnlyList<MonsterProgress> ProgressedMonsters,
    EquipmentChanges Runes,
    EquipmentChanges Artifacts,
    Change<decimal> AverageRuneEfficiency,
    IReadOnlyList<RuneDetail> TopNewRunes);

public sealed record SnapshotRef(int Version, DateTimeOffset ImportedAt, DateTimeOffset? ExportedAt);

/// <summary>Valor na versão de referência e na comparada.</summary>
public sealed record Change<T>(T From, T To);

public sealed record MonsterChangeItem(long MonsterId, int MasterId, string? Name, Element Element, int Stars, int Level);

/// <summary>Monstro presente nas duas versões cujas estrelas, nível, despertar ou velocidade total mudaram.</summary>
public sealed record MonsterProgress(
    long MonsterId,
    int MasterId,
    string? Name,
    Element Element,
    Change<int> Stars,
    Change<int> Level,
    Change<int?> AwakenLevel,
    Change<int> Speed);

/// <summary>
/// Runas ou artefatos: total em cada versão, quantos entraram e saíram (pelo id do jogo) e quantos dos que
/// continuam subiram de nível.
/// </summary>
public sealed record EquipmentChanges(Change<int> Total, int Added, int Removed, int Upgraded);

internal sealed class CompareSnapshotsQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<CompareSnapshotsQuery, SnapshotComparisonResponse>
{
    public async Task<Result<SnapshotComparisonResponse>> HandleAsync(CompareSnapshotsQuery query, CancellationToken cancellationToken)
    {
        if (query.From == query.To)
        {
            return AccountErrors.SameSnapshot;
        }

        if (!await wizardAccounts.IsOwnedByAsync(query.WizardId, userContext.UserId, cancellationToken))
        {
            return AccountErrors.NotFound(query.WizardId);
        }

        // Duas consultas em sequência: o DbContext não aceita operações concorrentes
        var from = await repository.GetByVersionAsync(query.WizardId, query.From, cancellationToken);
        if (from is null)
        {
            return AccountErrors.SnapshotNotFound(query.From);
        }

        var to = await repository.GetByVersionAsync(query.WizardId, query.To, cancellationToken);
        if (to is null)
        {
            return AccountErrors.SnapshotNotFound(query.To);
        }

        var fromMonsters = from.Monsters.ToDictionary(m => m.GameId);
        var toMonsters = to.Monsters.ToDictionary(m => m.GameId);

        var fromRuneIds = from.Runes.Select(r => r.GameId).ToHashSet();
        var newRunes = to.Runes.Where(r => !fromRuneIds.Contains(r.GameId));

        return new SnapshotComparisonResponse(
            Ref(from),
            Ref(to),
            new Change<int>(from.WizardLevel, to.WizardLevel),
            new Change<int>(from.Monsters.Count, to.Monsters.Count),
            ToChangeItems(to.Monsters.Where(m => !fromMonsters.ContainsKey(m.GameId))),
            ToChangeItems(from.Monsters.Where(m => !toMonsters.ContainsKey(m.GameId))),
            Progress(from, to, fromMonsters),
            Compare(from.Runes, to.Runes, r => r.GameId, r => r.Level),
            Compare(from.Artifacts, to.Artifacts, a => a.GameId, a => a.Level),
            new Change<decimal>(AverageEfficiency(from.Runes), AverageEfficiency(to.Runes)),
            [.. newRunes
                .Select(EquipmentDetails.ToDetail)
                .OrderByDescending(r => r.Efficiency)
                .ThenBy(r => r.RuneId)
                .Take(CompareSnapshotsQuery.TopNewRunesSize)]);
    }

    private static SnapshotRef Ref(AccountSnapshot snapshot) => new(snapshot.Version, snapshot.ImportedAt, snapshot.ExportedAt);

    // Materiais (rainbowmons, devilmons...) entram e saem o tempo todo e só poluiriam a lista
    private bool IsRelevant(Monster monster) => catalog.Find(monster.MasterId)?.Archetype != Archetype.Material;

    private List<MonsterChangeItem> ToChangeItems(IEnumerable<Monster> monsters) =>
        [.. monsters
            .Where(IsRelevant)
            .Select(m => new MonsterChangeItem(m.GameId, m.MasterId, catalog.Find(m.MasterId)?.Name, m.Element, m.Stars, m.Level))
            .OrderByDescending(m => m.Stars)
            .ThenByDescending(m => m.Level)
            .ThenBy(m => m.Name, StringComparer.Ordinal)];

    private List<MonsterProgress> Progress(AccountSnapshot from, AccountSnapshot to, Dictionary<long, Monster> fromMonsters)
    {
        var fromEquipment = new EquipmentIndex(from);
        var toEquipment = new EquipmentIndex(to);
        var progress = new List<MonsterProgress>();

        foreach (var current in to.Monsters.Where(IsRelevant))
        {
            if (!fromMonsters.TryGetValue(current.GameId, out var previous))
            {
                continue;
            }

            var speed = new Change<int>(fromEquipment.StatsOf(previous).Total.Speed, toEquipment.StatsOf(current).Total.Speed);

            // Despertar muda o MasterId (outro registro do bestiário), então o nome segue a versão comparada
            var definition = catalog.Find(current.MasterId);
            var awaken = new Change<int?>(catalog.Find(previous.MasterId)?.AwakenLevel, definition?.AwakenLevel);

            if (previous.Stars == current.Stars && previous.Level == current.Level && awaken.From == awaken.To && speed.From == speed.To)
            {
                continue;
            }

            progress.Add(new MonsterProgress(
                current.GameId,
                current.MasterId,
                definition?.Name,
                current.Element,
                new Change<int>(previous.Stars, current.Stars),
                new Change<int>(previous.Level, current.Level),
                awaken,
                speed));
        }

        return [.. progress
            .OrderByDescending(p => p.Stars.To)
            .ThenByDescending(p => p.Speed.To - p.Speed.From)
            .ThenBy(p => p.Name, StringComparer.Ordinal)];
    }

    private static EquipmentChanges Compare<T>(IReadOnlyList<T> from, IReadOnlyList<T> to, Func<T, long> id, Func<T, int> level)
    {
        var fromById = from.ToDictionary(id);
        var toIds = to.Select(id).ToHashSet();

        return new EquipmentChanges(
            new Change<int>(from.Count, to.Count),
            Added: to.Count(item => !fromById.ContainsKey(id(item))),
            Removed: from.Count(item => !toIds.Contains(id(item))),
            Upgraded: to.Count(item => fromById.TryGetValue(id(item), out var old) && level(item) > level(old)));
    }

    private static decimal AverageEfficiency(IReadOnlyList<Rune> runes) =>
        runes.Count == 0 ? 0 : Math.Round(runes.Average(r => RuneEfficiency.Calculate(r).Current), 2);
}
