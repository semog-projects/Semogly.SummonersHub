using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Critérios do otimizador salvos com um nome pelo usuário (ex.: "Violent VEL"), para reaplicar em qualquer
/// monstro e em qualquer dispositivo. Os critérios são guardados como JSON opaco: o formato é do formulário.
/// </summary>
public sealed class OptimizerPreset
{
    public const int MaxNameLength = 40;

    public const int MaxCriteriaLength = 8000;

    public const int MaxPerUser = 50;

    // Construtor do EF Core
    private OptimizerPreset()
    {
        Name = null!;
        Criteria = null!;
    }

    private OptimizerPreset(Guid id, Guid ownerId, string name, string criteria, DateTimeOffset updatedAt)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Criteria = criteria;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    /// <summary>Nome único por usuário (sem diferenciar maiúsculas no banco).</summary>
    public string Name { get; private set; }

    /// <summary>Critérios em JSON.</summary>
    public string Criteria { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static OptimizerPreset Create(Guid ownerId, string name, string criteria, DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException("O preset precisa de um usuário.");
        }

        return new OptimizerPreset(Guid.CreateVersion7(now), ownerId, NormalizeName(name), ValidateCriteria(criteria), now);
    }

    public void Update(string criteria, DateTimeOffset now)
    {
        Criteria = ValidateCriteria(criteria);
        UpdatedAt = now;
    }

    public static string NormalizeName(string name)
    {
        var normalized = string.Join(' ', (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (normalized.Length is 0 or > MaxNameLength)
        {
            throw new DomainException($"O nome do preset deve ter de 1 a {MaxNameLength} caracteres.");
        }

        return normalized;
    }

    private static string ValidateCriteria(string criteria)
    {
        if (string.IsNullOrWhiteSpace(criteria) || criteria.Length > MaxCriteriaLength)
        {
            throw new DomainException($"Os critérios do preset devem ter até {MaxCriteriaLength} caracteres.");
        }

        return criteria;
    }
}

public interface IOptimizerPresetRepository
{
    Task<List<OptimizerPreset>> ListAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<OptimizerPreset?> FindAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    Task<OptimizerPreset?> FindByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken);

    Task<int> CountAsync(Guid ownerId, CancellationToken cancellationToken);

    void Add(OptimizerPreset preset);

    void Remove(OptimizerPreset preset);
}
