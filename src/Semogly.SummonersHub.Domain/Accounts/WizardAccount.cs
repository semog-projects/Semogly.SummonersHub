using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Vínculo entre um invocador do jogo e o usuário do site. O primeiro usuário que importa o invocador
/// fica como dono; só ele pode importar novas versões e ler os dados da conta.
/// </summary>
public sealed class WizardAccount
{
    // Construtor do EF Core
    private WizardAccount()
    {
    }

    private WizardAccount(Guid id, long wizardId, Guid ownerId, DateTimeOffset linkedAt)
    {
        Id = id;
        WizardId = wizardId;
        OwnerId = ownerId;
        LinkedAt = linkedAt;
    }

    public Guid Id { get; private set; }

    /// <summary>Id do invocador no jogo (<c>wizard_id</c>). Único: um invocador tem um só dono.</summary>
    public long WizardId { get; private set; }

    /// <summary>Id do usuário dono do invocador.</summary>
    public Guid OwnerId { get; private set; }

    public DateTimeOffset LinkedAt { get; private set; }

    public static WizardAccount Link(long wizardId, Guid ownerId, DateTimeOffset linkedAt)
    {
        if (wizardId <= 0)
        {
            throw new DomainException("O id do invocador deve ser positivo.");
        }

        if (ownerId == Guid.Empty)
        {
            throw new DomainException("O vínculo precisa de um usuário.");
        }

        return new WizardAccount(Guid.CreateVersion7(linkedAt), wizardId, ownerId, linkedAt);
    }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
