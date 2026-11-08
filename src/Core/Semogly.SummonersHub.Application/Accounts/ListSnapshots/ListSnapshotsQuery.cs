using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts.ListSnapshots;

/// <summary>Linha do tempo dos imports do invocador: o resumo de cada versão, da mais recente para a mais antiga.</summary>
public sealed record ListSnapshotsQuery(long WizardId) : IQuery<IReadOnlyList<AccountSummaryResponse>>;

internal sealed class ListSnapshotsQueryHandler(
    IAccountReadService readService,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext) : IQueryHandler<ListSnapshotsQuery, IReadOnlyList<AccountSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<AccountSummaryResponse>>> HandleAsync(ListSnapshotsQuery query, CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(query.WizardId, userContext.UserId, cancellationToken))
        {
            return AccountErrors.NotFound(query.WizardId);
        }

        var summaries = await readService.ListSummariesAsync(query.WizardId, cancellationToken);
        return summaries.Count == 0 ? AccountErrors.NotFound(query.WizardId) : Result.Success(summaries);
    }
}
