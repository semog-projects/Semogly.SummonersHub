using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts.ListArtifacts;

/// <summary>Todos os artefatos do último import com o monstro que os equipa; o front filtra e ordena.</summary>
public sealed record ListArtifactsQuery(long WizardId) : IQuery<IReadOnlyList<ArtifactListItem>>;

/// <param name="EquippedOn">Nulo quando o artefato está no inventário.</param>
/// <param name="Locked">Bloqueado pelo usuário: o otimizador não o tira de quem o usa.</param>
/// <param name="Tags">Tags do usuário.</param>
/// <param name="LockedByMonster">Equipado num monstro bloqueado (conta como bloqueado para o otimizador).</param>
public sealed record ArtifactListItem(ArtifactDetail Artifact, EquippedMonster? EquippedOn, bool Locked, IReadOnlyList<string> Tags, bool LockedByMonster = false);

internal sealed class ListArtifactsQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog,
    IInventoryMarkRepository marks) : IQueryHandler<ListArtifactsQuery, IReadOnlyList<ArtifactListItem>>
{
    public async Task<Result<IReadOnlyList<ArtifactListItem>>> HandleAsync(ListArtifactsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var monsters = EquipmentDetails.MonstersById(result.Value, catalog);
        var lookup = await InventoryMarkLookup.LoadAsync(marks, query.WizardId, cancellationToken);

        return result.Value.Artifacts
            .OrderBy(a => a.Type)
            .ThenByDescending(a => a.OriginalRarity)
            .ThenByDescending(a => a.Level)
            .ThenBy(a => a.GameId)
            .Select(artifact => new ArtifactListItem(
                EquipmentDetails.ToDetail(artifact),
                artifact.EquippedMonsterId is { } id ? monsters[id] : null,
                lookup.IsLocked(artifact),
                lookup.TagsOf(artifact),
                lookup.IsLockedByMonster(artifact.EquippedMonsterId)))
            .ToList();
    }
}
