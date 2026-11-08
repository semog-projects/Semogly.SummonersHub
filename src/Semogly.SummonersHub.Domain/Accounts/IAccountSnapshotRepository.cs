namespace Semogly.SummonersHub.Domain.Accounts;

public interface IAccountSnapshotRepository
{
    /// <summary>Snapshot do invocador com o mesmo conteúdo, se já importado.</summary>
    Task<AccountSnapshot?> FindByContentHashAsync(long wizardGameId, string contentHash, CancellationToken cancellationToken);

    /// <summary>Última versão importada do invocador, ou 0 se nunca importou.</summary>
    Task<int> GetLatestVersionAsync(long wizardGameId, CancellationToken cancellationToken);

    void Add(AccountSnapshot snapshot);
}
