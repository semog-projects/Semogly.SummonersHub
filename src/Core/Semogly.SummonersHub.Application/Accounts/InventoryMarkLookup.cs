using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts;

/// <summary>Marcações (bloqueio e tags) de runas e artefatos do invocador, indexadas pelo id do item.</summary>
internal sealed class InventoryMarkLookup
{
    private readonly Dictionary<long, InventoryMark> _runes;
    private readonly Dictionary<long, InventoryMark> _artifacts;

    private InventoryMarkLookup(IEnumerable<InventoryMark> runes, IEnumerable<InventoryMark> artifacts)
    {
        _runes = runes.ToDictionary(m => m.ItemId);
        _artifacts = artifacts.ToDictionary(m => m.ItemId);
    }

    public static async Task<InventoryMarkLookup> LoadAsync(IInventoryMarkRepository repository, long wizardId, CancellationToken cancellationToken)
    {
        // Em sequência: o DbContext não aceita consultas concorrentes
        var runes = await repository.ListAsync(wizardId, InventoryItemKind.Rune, cancellationToken);
        var artifacts = await repository.ListAsync(wizardId, InventoryItemKind.Artifact, cancellationToken);
        return new InventoryMarkLookup(runes, artifacts);
    }

    public bool IsLocked(Rune rune) => _runes.GetValueOrDefault(rune.GameId)?.IsLocked ?? false;

    public bool IsLocked(Artifact artifact) => _artifacts.GetValueOrDefault(artifact.GameId)?.IsLocked ?? false;

    public IReadOnlyList<string> TagsOf(Rune rune) => _runes.GetValueOrDefault(rune.GameId)?.Tags ?? [];

    public IReadOnlyList<string> TagsOf(Artifact artifact) => _artifacts.GetValueOrDefault(artifact.GameId)?.Tags ?? [];
}
