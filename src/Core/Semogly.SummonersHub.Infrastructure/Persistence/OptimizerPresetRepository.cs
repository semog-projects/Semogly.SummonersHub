using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class OptimizerPresetRepository(SummonersHubDbContext db) : IOptimizerPresetRepository
{
    public Task<List<OptimizerPreset>> ListAsync(Guid ownerId, CancellationToken cancellationToken) =>
        db.OptimizerPresets.Where(p => p.OwnerId == ownerId).OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public Task<OptimizerPreset?> FindAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        db.OptimizerPresets.FirstOrDefaultAsync(p => p.OwnerId == ownerId && p.Id == id, cancellationToken);

    // Nome sem diferenciar maiúsculas ("violent vel" = "Violent VEL"); % e _ escapados para não virarem curinga do ILIKE
    public Task<OptimizerPreset?> FindByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken)
    {
        var pattern = name.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);

        return db.OptimizerPresets.FirstOrDefaultAsync(p => p.OwnerId == ownerId && EF.Functions.ILike(p.Name, pattern), cancellationToken);
    }

    public Task<int> CountAsync(Guid ownerId, CancellationToken cancellationToken) =>
        db.OptimizerPresets.CountAsync(p => p.OwnerId == ownerId, cancellationToken);

    public void Add(OptimizerPreset preset) => db.OptimizerPresets.Add(preset);

    public void Remove(OptimizerPreset preset) => db.OptimizerPresets.Remove(preset);
}
