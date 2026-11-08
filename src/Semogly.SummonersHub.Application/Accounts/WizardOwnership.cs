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
}
