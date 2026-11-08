using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class AccountReadService(SummonersHubDbContext db) : IAccountReadService
{
    public Task<AccountSummaryResponse?> GetLatestSummaryAsync(long wizardId, CancellationToken cancellationToken) =>
        db.AccountSnapshots
            .AsNoTracking()
            .Where(s => s.WizardId == wizardId)
            .OrderByDescending(s => s.Version)
            .Select(s => new AccountSummaryResponse(
                s.WizardId,
                s.WizardName,
                s.WizardLevel,
                s.Id,
                s.Version,
                s.ImportedAt,
                s.ExportedAt,
                s.Monsters.Count,
                s.Runes.Count,
                s.Artifacts.Count,
                s.CraftItems.Count))
            .FirstOrDefaultAsync(cancellationToken);
}
