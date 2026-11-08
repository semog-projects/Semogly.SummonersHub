using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Application.Accounts.ListAccounts;

/// <summary>Invocadores do usuário autenticado, com o resumo do último import de cada um.</summary>
public sealed record ListAccountsQuery : IQuery<IReadOnlyList<AccountSummaryResponse>>;

internal sealed class ListAccountsQueryHandler(IAccountReadService readService, IUserContext userContext)
    : IQueryHandler<ListAccountsQuery, IReadOnlyList<AccountSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<AccountSummaryResponse>>> HandleAsync(ListAccountsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await readService.ListLatestSummariesAsync(userContext.UserId, cancellationToken));
}
