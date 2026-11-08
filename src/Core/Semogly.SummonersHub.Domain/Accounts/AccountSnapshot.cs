using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Foto da conta de um invocador em um import. Cada import com conteúdo diferente gera uma nova versão;
/// as versões anteriores são mantidas para o histórico.
/// </summary>
public sealed class AccountSnapshot
{
    private readonly List<Monster> _monsters = [];
    private readonly List<Rune> _runes = [];
    private readonly List<Artifact> _artifacts = [];
    private readonly List<CraftItem> _craftItems = [];

    // Construtor do EF Core
    private AccountSnapshot()
    {
        WizardName = null!;
        ContentHash = null!;
    }

    private AccountSnapshot(
        Guid id,
        WizardProfile wizard,
        int version,
        string contentHash,
        DateTimeOffset? exportedAt,
        DateTimeOffset importedAt)
    {
        Id = id;
        WizardId = wizard.GameId;
        WizardName = wizard.Name;
        WizardLevel = wizard.Level;
        WizardServerId = wizard.ServerId;
        Version = version;
        ContentHash = contentHash;
        ExportedAt = exportedAt;
        ImportedAt = importedAt;
    }

    public Guid Id { get; private set; }

    /// <summary>Id do invocador no jogo (<c>wizard_id</c>).</summary>
    public long WizardId { get; private set; }

    public string WizardName { get; private set; }

    public int WizardLevel { get; private set; }

    public int? WizardServerId { get; private set; }

    /// <summary>Versão sequencial por invocador, começando em 1.</summary>
    public int Version { get; private set; }

    /// <summary>Hash SHA-256 do conteúdo importado: identifica reenvios do mesmo export.</summary>
    public string ContentHash { get; private set; }

    public DateTimeOffset? ExportedAt { get; private set; }

    public DateTimeOffset ImportedAt { get; private set; }

    public IReadOnlyList<Monster> Monsters => _monsters;

    public IReadOnlyList<Rune> Runes => _runes;

    public IReadOnlyList<Artifact> Artifacts => _artifacts;

    public IReadOnlyList<CraftItem> CraftItems => _craftItems;

    public static AccountSnapshot Create(
        WizardProfile wizard,
        int version,
        string contentHash,
        DateTimeOffset? exportedAt,
        DateTimeOffset importedAt,
        IEnumerable<Monster> monsters,
        IEnumerable<Rune> runes,
        IEnumerable<Artifact> artifacts,
        IEnumerable<CraftItem> craftItems)
    {
        if (wizard.GameId <= 0)
        {
            throw new DomainException("O id do invocador é inválido.");
        }

        if (version < 1)
        {
            throw new DomainException("A versão do snapshot deve começar em 1.");
        }

        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new DomainException("O hash do conteúdo é obrigatório.");
        }

        var snapshot = new AccountSnapshot(Guid.CreateVersion7(importedAt), wizard, version, contentHash, exportedAt, importedAt);

        snapshot._monsters.AddRange(EnsureUnique(monsters, m => m.GameId, "monstros"));
        snapshot._runes.AddRange(EnsureUnique(runes, r => r.GameId, "runas"));
        snapshot._artifacts.AddRange(EnsureUnique(artifacts, a => a.GameId, "artefatos"));
        snapshot._craftItems.AddRange(EnsureUnique(craftItems, c => c.GameId, "itens de craft"));

        var monsterIds = snapshot._monsters.Select(m => m.GameId).ToHashSet();
        if (snapshot._runes.Any(r => r.EquippedMonsterId is { } id && !monsterIds.Contains(id))
            || snapshot._artifacts.Any(a => a.EquippedMonsterId is { } id && !monsterIds.Contains(id)))
        {
            throw new DomainException("Há runas ou artefatos equipados em monstros que não estão no import.");
        }

        return snapshot;
    }

    private static List<T> EnsureUnique<T>(IEnumerable<T> items, Func<T, long> key, string description)
    {
        var list = items.ToList();
        if (list.Select(key).Distinct().Count() != list.Count)
        {
            throw new DomainException($"O import contém {description} duplicados.");
        }

        return list;
    }
}

/// <summary>Dados do invocador (conta do jogo) no momento do import.</summary>
public sealed record WizardProfile(long GameId, string Name, int Level, int? ServerId);
