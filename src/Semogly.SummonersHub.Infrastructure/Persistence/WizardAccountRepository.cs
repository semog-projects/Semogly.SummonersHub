using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

internal sealed class WizardAccountRepository(SummonersHubDbContext db) : IWizardAccountRepository
{
    public Task<WizardAccount?> FindByWizardIdAsync(long wizardGameId, CancellationToken cancellationToken) =>
        db.WizardAccounts.FirstOrDefaultAsync(w => w.WizardId == wizardGameId, cancellationToken);

    public void Add(WizardAccount account) => db.WizardAccounts.Add(account);
}
