using System.Security.Cryptography;
using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Link público, somente leitura, para a build de um monstro na versão do import em que foi criado: importar
/// de novo não muda o que o link mostra. Revogar apaga o link.
/// </summary>
public sealed class SharedBuild
{
    /// <summary>16 bytes aleatórios em base64url: 22 caracteres, impossível de adivinhar.</summary>
    public const int TokenLength = 22;

    // Construtor do EF Core
    private SharedBuild()
    {
        Token = null!;
    }

    private SharedBuild(Guid id, string token, Guid ownerId, long wizardId, long monsterId, Guid snapshotId, DateTimeOffset createdAt)
    {
        Id = id;
        Token = token;
        OwnerId = ownerId;
        WizardId = wizardId;
        MonsterId = monsterId;
        SnapshotId = snapshotId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Token { get; private set; }

    public Guid OwnerId { get; private set; }

    /// <summary>Id do invocador no jogo (não é exposto na página pública).</summary>
    public long WizardId { get; private set; }

    /// <summary>Id da instância do monstro no jogo.</summary>
    public long MonsterId { get; private set; }

    /// <summary>Snapshot (versão do import) mostrado pelo link.</summary>
    public Guid SnapshotId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SharedBuild Create(Guid ownerId, long wizardId, long monsterId, Guid snapshotId, DateTimeOffset now)
    {
        if (ownerId == Guid.Empty || wizardId <= 0 || monsterId <= 0 || snapshotId == Guid.Empty)
        {
            throw new DomainException("Link de build inválido.");
        }

        return new SharedBuild(Guid.CreateVersion7(now), NewToken(), ownerId, wizardId, monsterId, snapshotId, now);
    }

    private static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public interface ISharedBuildRepository
{
    Task<SharedBuild?> FindByTokenAsync(string token, CancellationToken cancellationToken);

    /// <summary>Links do usuário para um monstro, do mais novo para o mais antigo.</summary>
    Task<List<SharedBuild>> ListAsync(Guid ownerId, long wizardId, long monsterId, CancellationToken cancellationToken);

    void Add(SharedBuild build);

    void Remove(SharedBuild build);
}
