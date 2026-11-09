using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class ManualItemRepository(SummonersHubDbContext db) : IManualItemRepository
{
    public Task<List<ManualMonster>> ListMonstersAsync(long wizardId, CancellationToken cancellationToken) =>
        db.ManualMonsters.Where(m => m.WizardId == wizardId).ToListAsync(cancellationToken);

    public Task<List<ManualCraftItem>> ListCraftItemsAsync(long wizardId, CancellationToken cancellationToken) =>
        db.ManualCraftItems.Where(c => c.WizardId == wizardId).ToListAsync(cancellationToken);

    // Menor id já usado (monstros e itens) menos 1; ids apagados não são reaproveitados enquanto houver um menor
    public async Task<long> NextGameIdAsync(long wizardId, CancellationToken cancellationToken)
    {
        var monsters = await db.ManualMonsters.Where(m => m.WizardId == wizardId).MinAsync(m => (long?)m.GameId, cancellationToken) ?? 0;
        var items = await db.ManualCraftItems.Where(c => c.WizardId == wizardId).MinAsync(c => (long?)c.GameId, cancellationToken) ?? 0;
        return Math.Min(monsters, items) - 1;
    }

    public async Task<int> CountAsync(long wizardId, CancellationToken cancellationToken) =>
        await db.ManualMonsters.CountAsync(m => m.WizardId == wizardId, cancellationToken)
        + await db.ManualCraftItems.CountAsync(c => c.WizardId == wizardId, cancellationToken);

    public void Add(ManualMonster monster) => db.ManualMonsters.Add(monster);

    public void Add(ManualCraftItem item) => db.ManualCraftItems.Add(item);

    public void Remove(ManualMonster monster) => db.ManualMonsters.Remove(monster);

    public void Remove(ManualCraftItem item) => db.ManualCraftItems.Remove(item);
}
