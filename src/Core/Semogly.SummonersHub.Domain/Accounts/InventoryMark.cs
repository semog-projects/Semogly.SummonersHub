using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Domain.Accounts;

public enum InventoryItemKind
{
    Rune = 1,
    Artifact = 2,
}

/// <summary>
/// Marcações do usuário sobre uma runa ou artefato (bloqueio e tags). A chave é o id do item no jogo, então
/// a marcação continua valendo nos imports seguintes enquanto o item existir na conta.
/// </summary>
public sealed class InventoryMark
{
    public const int MaxTags = 10;

    public const int MaxTagLength = 24;

    private List<string> _tags = [];

    // Construtor do EF Core
    private InventoryMark()
    {
    }

    private InventoryMark(Guid id, long wizardId, InventoryItemKind kind, long itemId)
    {
        Id = id;
        WizardId = wizardId;
        Kind = kind;
        ItemId = itemId;
    }

    public Guid Id { get; private set; }

    /// <summary>Id do invocador no jogo.</summary>
    public long WizardId { get; private set; }

    public InventoryItemKind Kind { get; private set; }

    /// <summary>Id da runa (<c>rune_id</c>) ou do artefato (<c>rid</c>) no jogo.</summary>
    public long ItemId { get; private set; }

    /// <summary>Item reservado: o otimizador não o tira de quem já o usa.</summary>
    public bool IsLocked { get; private set; }

    /// <summary>Tags livres, sem repetição (ignorando maiúsculas), na ordem em que foram adicionadas.</summary>
    public IReadOnlyList<string> Tags => _tags;

    /// <summary>Sem bloqueio nem tags: não precisa ser guardada.</summary>
    public bool IsEmpty => !IsLocked && _tags.Count == 0;

    public static InventoryMark Create(long wizardId, InventoryItemKind kind, long itemId)
    {
        if (wizardId <= 0 || itemId <= 0)
        {
            throw new DomainException("Invocador e item precisam de ids válidos.");
        }

        return new InventoryMark(Guid.CreateVersion7(), wizardId, kind, itemId);
    }

    public void SetLocked(bool locked) => IsLocked = locked;

    public void AddTag(string tag)
    {
        var normalized = Normalize(tag);
        if (_tags.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (_tags.Count >= MaxTags)
        {
            throw new DomainException($"Um item pode ter no máximo {MaxTags} tags.");
        }

        _tags = [.. _tags, normalized];
    }

    public void RemoveTag(string tag)
    {
        var normalized = Normalize(tag);
        _tags = [.. _tags.Where(t => !string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>Tag sem espaços nas pontas, com espaços internos colapsados e até <see cref="MaxTagLength"/> caracteres.</summary>
    public static string Normalize(string tag)
    {
        var normalized = string.Join(' ', (tag ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (normalized.Length is 0 or > MaxTagLength)
        {
            throw new DomainException($"A tag deve ter de 1 a {MaxTagLength} caracteres.");
        }

        return normalized;
    }
}

public interface IInventoryMarkRepository
{
    /// <summary>Marcações do invocador, de um tipo de item.</summary>
    Task<List<InventoryMark>> ListAsync(long wizardId, InventoryItemKind kind, CancellationToken cancellationToken);

    void Add(InventoryMark mark);

    void Remove(InventoryMark mark);
}
