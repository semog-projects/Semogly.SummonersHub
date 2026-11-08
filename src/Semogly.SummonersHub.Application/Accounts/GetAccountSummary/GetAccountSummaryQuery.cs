using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Application.Accounts.GetAccountSummary;

/// <summary>Resumo do último import de um invocador.</summary>
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
}

internal sealed class GetAccountSummaryQueryHandler(IAccountReadService readService)
    : IQueryHandler<GetAccountSummaryQuery, AccountSummaryResponse>
{
    public async Task<Result<AccountSummaryResponse>> HandleAsync(GetAccountSummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await readService.GetLatestSummaryAsync(query.WizardId, cancellationToken);
        return summary is null ? AccountErrors.NotFound(query.WizardId) : summary;
    }
}
