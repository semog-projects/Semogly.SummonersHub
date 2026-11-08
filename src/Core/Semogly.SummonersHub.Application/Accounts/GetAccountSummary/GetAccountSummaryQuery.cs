using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts.GetAccountSummary;

/// <summary>Resumo do último import de um invocador do usuário.</summary>
public sealed record GetAccountSummaryQuery(long WizardId) : IQuery<AccountSummaryResponse>;

public sealed record AccountSummaryResponse(
    long WizardId,
    string WizardName,
    int WizardLevel,
    Guid SnapshotId,
    int Version,
    DateTimeOffset ImportedAt,
    DateTimeOffset? ExportedAt,
    int Monsters,
    int Runes,
    int Artifacts,
    int CraftItems);

/// <summary>Consultas de leitura sobre os snapshots (projeções, sem carregar o agregado).</summary>
public interface IAccountReadService
{
    Task<AccountSummaryResponse?> GetLatestSummaryAsync(long wizardId, CancellationToken cancellationToken);

    /// <summary>Resumo do último import de cada invocador do usuário, do import mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<AccountSummaryResponse>> ListLatestSummariesAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>Resumo de todas as versões do invocador, da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<AccountSummaryResponse>> ListSummariesAsync(long wizardId, CancellationToken cancellationToken);
}

internal sealed class GetAccountSummaryQueryHandler(
    IAccountReadService readService,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext) : IQueryHandler<GetAccountSummaryQuery, AccountSummaryResponse>
{
    public async Task<Result<AccountSummaryResponse>> HandleAsync(GetAccountSummaryQuery query, CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(query.WizardId, userContext.UserId, cancellationToken))
        {
            return AccountErrors.NotFound(query.WizardId);
        }

        var summary = await readService.GetLatestSummaryAsync(query.WizardId, cancellationToken);
        return summary is null ? AccountErrors.NotFound(query.WizardId) : summary;
    }
}
