using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class AccountReadService(SummonersHubDbContext db) : IAccountReadService
{
    private static readonly Expression<Func<AccountSnapshot, AccountSummaryResponse>> ToSummary = s => new AccountSummaryResponse(
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
        s.CraftItems.Count);

    public Task<AccountSummaryResponse?> GetLatestSummaryAsync(long wizardId, CancellationToken cancellationToken) =>
        db.AccountSnapshots
            .AsNoTracking()
            .Where(s => s.WizardId == wizardId)
            .OrderByDescending(s => s.Version)
            .Select(ToSummary)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AccountSummaryResponse>> ListLatestSummariesAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.AccountSnapshots
            .AsNoTracking()
            .Where(s => db.WizardAccounts.Any(w => w.OwnerId == ownerId && w.WizardId == s.WizardId))
            .Where(s => s.Version == db.AccountSnapshots.Where(x => x.WizardId == s.WizardId).Max(x => x.Version))
            .OrderByDescending(s => s.ImportedAt)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AccountSummaryResponse>> ListSummariesAsync(long wizardId, CancellationToken cancellationToken) =>
        await db.AccountSnapshots
            .AsNoTracking()
            .Where(s => s.WizardId == wizardId)
            .OrderByDescending(s => s.Version)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);
}
