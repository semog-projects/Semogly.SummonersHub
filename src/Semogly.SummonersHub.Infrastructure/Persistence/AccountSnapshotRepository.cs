using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class AccountSnapshotRepository(SummonersHubDbContext db) : IAccountSnapshotRepository
{
    // Carrega o agregado completo; uma query por coleção evita a explosão cartesiana dos joins
    public Task<AccountSnapshot?> FindByContentHashAsync(long wizardGameId, string contentHash, CancellationToken cancellationToken) =>
        db.AccountSnapshots
            .Include(s => s.Monsters)
            .Include(s => s.Runes)
            .Include(s => s.Artifacts)
            .Include(s => s.CraftItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.WizardId == wizardGameId && s.ContentHash == contentHash, cancellationToken);

    public async Task<int> GetLatestVersionAsync(long wizardGameId, CancellationToken cancellationToken) =>
        await db.AccountSnapshots
            .Where(s => s.WizardId == wizardGameId)
            .MaxAsync(s => (int?)s.Version, cancellationToken) ?? 0;

    public void Add(AccountSnapshot snapshot) => db.AccountSnapshots.Add(snapshot);
}
