using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Application.Accounts.ManualItems;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class AccountSnapshotRepository(
    SummonersHubDbContext db,
    IMonsterCatalog monsterCatalog,
    ISkillCatalog skillCatalog) : IAccountSnapshotRepository
{
    public Task<AccountSnapshot?> FindByContentHashAsync(long wizardGameId, string contentHash, CancellationToken cancellationToken) =>
        WithChildren().FirstOrDefaultAsync(s => s.WizardId == wizardGameId && s.ContentHash == contentHash, cancellationToken);

    public Task<AccountSnapshot?> GetLatestAsync(long wizardGameId, CancellationToken cancellationToken) =>
        WithChildren()
            .Where(s => s.WizardId == wizardGameId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AccountSnapshot?> GetLatestWithManualItemsAsync(long wizardGameId, CancellationToken cancellationToken)
    {
        var snapshot = await GetLatestAsync(wizardGameId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var monsters = await db.ManualMonsters.Where(m => m.WizardId == wizardGameId).OrderBy(m => m.GameId).ToListAsync(cancellationToken);
        var craftItems = await db.ManualCraftItems.Where(c => c.WizardId == wizardGameId).OrderBy(c => c.GameId).ToListAsync(cancellationToken);
        return ManualItemsMerge.Merge(snapshot, monsters, craftItems, monsterCatalog, skillCatalog);
    }

    public Task<AccountSnapshot?> GetByVersionAsync(long wizardGameId, int version, CancellationToken cancellationToken) =>
        WithChildren().FirstOrDefaultAsync(s => s.WizardId == wizardGameId && s.Version == version, cancellationToken);

    public Task<AccountSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken) =>
        WithChildren().FirstOrDefaultAsync(s => s.Id == snapshotId, cancellationToken);

    public async Task<int> GetLatestVersionAsync(long wizardGameId, CancellationToken cancellationToken) =>
        await db.AccountSnapshots
            .Where(s => s.WizardId == wizardGameId)
            .MaxAsync(s => (int?)s.Version, cancellationToken) ?? 0;

    public void Add(AccountSnapshot snapshot) => db.AccountSnapshots.Add(snapshot);

    // Carrega o agregado completo; uma query por coleção evita a explosão cartesiana dos joins
    private IQueryable<AccountSnapshot> WithChildren() =>
        db.AccountSnapshots
            .Include(s => s.Monsters)
            .Include(s => s.Runes)
            .Include(s => s.Artifacts)
            .Include(s => s.CraftItems)
            .Include(s => s.Builds)
            .AsSplitQuery();
}
