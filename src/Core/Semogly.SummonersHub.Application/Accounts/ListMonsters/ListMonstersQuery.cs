using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.ListMonsters;

/// <summary>
/// Todos os monstros do último import com os atributos totais. A lista vai inteira (uma conta tem no máximo
/// alguns milhares) e o front filtra e ordena sem novas chamadas.
/// </summary>
public sealed record ListMonstersQuery(long WizardId) : IQuery<IReadOnlyList<MonsterListItem>>;

public sealed record MonsterListItem(
    long MonsterId,
    int MasterId,
    string? Name,
    Element Element,
    Archetype? Archetype,
    int Stars,
    int Level,
    int? AwakenLevel,
    StatValues Total,
    IReadOnlyList<RuneSet> ActiveSets,
    int Runes,
    int Artifacts,
    decimal? AverageRuneEfficiency,
    bool Locked = false);

internal sealed class ListMonstersQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog,
    IInventoryMarkRepository marks) : IQueryHandler<ListMonstersQuery, IReadOnlyList<MonsterListItem>>
{
    public async Task<Result<IReadOnlyList<MonsterListItem>>> HandleAsync(ListMonstersQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var equipment = new EquipmentIndex(result.Value);
        var lookup = await InventoryMarkLookup.LoadAsync(marks, query.WizardId, cancellationToken);

        return result.Value.Monsters
            .Select(monster =>
            {
                var definition = catalog.Find(monster.MasterId);
                var stats = equipment.StatsOf(monster);
                return new MonsterListItem(
                    monster.GameId,
                    monster.MasterId,
                    definition?.Name,
                    monster.Element,
                    definition?.Archetype,
                    monster.Stars,
                    monster.Level,
                    definition?.AwakenLevel,
                    stats.Total,
                    stats.ActiveSets,
                    equipment.RunesOf(monster.GameId).Count(),
                    equipment.ArtifactsOf(monster.GameId).Count(),
                    equipment.AverageRuneEfficiencyOf(monster.GameId),
                    lookup.IsMonsterLocked(monster.GameId));
            })
            .OrderByDescending(m => m.Stars)
            .ThenByDescending(m => m.Level)
            .ThenBy(m => m.Name, StringComparer.Ordinal)
            .ToList();
    }
}
