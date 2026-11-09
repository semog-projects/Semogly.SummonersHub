using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class UserPreferencesRepository(SummonersHubDbContext db) : IUserPreferencesRepository
{
    public Task<UserPreferences?> FindAsync(Guid ownerId, CancellationToken cancellationToken) =>
        db.UserPreferences.FirstOrDefaultAsync(p => p.OwnerId == ownerId, cancellationToken);

    public void Add(UserPreferences preferences) => db.UserPreferences.Add(preferences);
}
