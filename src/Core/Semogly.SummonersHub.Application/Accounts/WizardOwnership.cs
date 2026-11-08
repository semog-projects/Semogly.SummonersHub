using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts;

internal static class WizardOwnership
{
    /// <summary>
    /// Indica se o usuário é dono do invocador. Consultas de quem não é dono respondem como "não encontrado",
    /// para não revelar quais invocadores existem no site.
    /// </summary>
    public static async Task<bool> IsOwnedByAsync(
        this IWizardAccountRepository repository,
        long wizardId,
        Guid userId,
        CancellationToken cancellationToken) =>
        await repository.FindByWizardIdAsync(wizardId, cancellationToken) is { } link && link.IsOwnedBy(userId);

    /// <summary>Último snapshot completo do invocador, se o usuário for o dono; senão, NotFound.</summary>
    public static async Task<Result<AccountSnapshot>> GetOwnedLatestAsync(
        this IAccountSnapshotRepository snapshots,
        IWizardAccountRepository wizardAccounts,
        long wizardId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(wizardId, userId, cancellationToken))
        {
            return AccountErrors.NotFound(wizardId);
        }

        var snapshot = await snapshots.GetLatestAsync(wizardId, cancellationToken);
        return snapshot is null ? AccountErrors.NotFound(wizardId) : snapshot;
    }
}
