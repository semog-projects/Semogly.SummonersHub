using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class InventoryMarkRepository(SummonersHubDbContext db) : IInventoryMarkRepository
{
    public Task<List<InventoryMark>> ListAsync(long wizardId, InventoryItemKind kind, CancellationToken cancellationToken) =>
        db.InventoryMarks.Where(m => m.WizardId == wizardId && m.Kind == kind).ToListAsync(cancellationToken);

    public Task<List<InventoryMark>> ListByWizardsAsync(IReadOnlyCollection<long> wizardIds, CancellationToken cancellationToken) =>
        db.InventoryMarks.Where(m => wizardIds.Contains(m.WizardId)).ToListAsync(cancellationToken);

    public void Add(InventoryMark mark) => db.InventoryMarks.Add(mark);

    public void Remove(InventoryMark mark) => db.InventoryMarks.Remove(mark);
}
