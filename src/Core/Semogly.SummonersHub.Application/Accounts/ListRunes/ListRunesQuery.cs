using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts.ListRunes;

/// <summary>
/// Todas as runas do último import com a eficiência e o monstro que as equipa. Como na lista de monstros,
/// vai tudo de uma vez (uma conta tem no máximo alguns milhares) e o front filtra e ordena.
/// </summary>
public sealed record ListRunesQuery(long WizardId) : IQuery<IReadOnlyList<RuneListItem>>;

/// <param name="EquippedOn">Nulo quando a runa está no inventário.</param>
public sealed record RuneListItem(RuneDetail Rune, EquippedMonster? EquippedOn);

internal sealed class ListRunesQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<ListRunesQuery, IReadOnlyList<RuneListItem>>
{
    public async Task<Result<IReadOnlyList<RuneListItem>>> HandleAsync(ListRunesQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var monsters = EquipmentDetails.MonstersById(result.Value, catalog);

        return result.Value.Runes
            .Select(rune => new RuneListItem(
                EquipmentDetails.ToDetail(rune),
                rune.EquippedMonsterId is { } id ? monsters[id] : null))
            .OrderByDescending(r => r.Rune.Efficiency)
            .ThenBy(r => r.Rune.RuneId)
            .ToList();
    }
}
