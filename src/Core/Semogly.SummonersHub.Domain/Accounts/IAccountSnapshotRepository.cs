namespace Semogly.SummonersHub.Domain.Accounts;

public interface IAccountSnapshotRepository
{
    /// <summary>Snapshot do invocador com o mesmo conteúdo, se já importado.</summary>
    Task<AccountSnapshot?> FindByContentHashAsync(long wizardGameId, string contentHash, CancellationToken cancellationToken);

    /// <summary>Última versão importada do invocador, ou 0 se nunca importou.</summary>
    Task<int> GetLatestVersionAsync(long wizardGameId, CancellationToken cancellationToken);

    /// <summary>Último snapshot do invocador com monstros, runas, artefatos e itens de craft.</summary>
    Task<AccountSnapshot?> GetLatestAsync(long wizardGameId, CancellationToken cancellationToken);

    /// <summary>Snapshot completo de uma versão do invocador.</summary>
    Task<AccountSnapshot?> GetByVersionAsync(long wizardGameId, int version, CancellationToken cancellationToken);

    /// <summary>Snapshot completo pelo id.</summary>
    Task<AccountSnapshot?> GetByIdAsync(Guid snapshotId, CancellationToken cancellationToken);

    void Add(AccountSnapshot snapshot);
}
