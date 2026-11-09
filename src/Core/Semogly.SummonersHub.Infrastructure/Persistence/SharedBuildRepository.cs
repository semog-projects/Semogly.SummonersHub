using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class SharedBuildRepository(SummonersHubDbContext db) : ISharedBuildRepository
{
    public Task<SharedBuild?> FindByTokenAsync(string token, CancellationToken cancellationToken) =>
        db.SharedBuilds.FirstOrDefaultAsync(b => b.Token == token, cancellationToken);

    public Task<List<SharedBuild>> ListAsync(Guid ownerId, long wizardId, long monsterId, CancellationToken cancellationToken) =>
        db.SharedBuilds
            .Where(b => b.OwnerId == ownerId && b.WizardId == wizardId && b.MonsterId == monsterId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(SharedBuild build) => db.SharedBuilds.Add(build);

    public void Remove(SharedBuild build) => db.SharedBuilds.Remove(build);
}
