namespace Semogly.SummonersHub.Domain.Accounts;

public interface IWizardAccountRepository
{
    /// <summary>Vínculo do invocador com um usuário, se já existir.</summary>
    Task<WizardAccount?> FindByWizardIdAsync(long wizardGameId, CancellationToken cancellationToken);

    /// <summary>Ids no jogo dos invocadores do usuário.</summary>
    Task<List<long>> ListWizardIdsAsync(Guid ownerId, CancellationToken cancellationToken);

    void Add(WizardAccount account);
}
