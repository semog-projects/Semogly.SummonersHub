using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts;

/// <summary>
/// Marcações (bloqueio e tags) de runas, artefatos e monstros do invocador, indexadas pelo id do jogo. Uma runa
/// ou artefato conta como bloqueado pelo próprio bloqueio ou por estar equipado num monstro bloqueado.
/// </summary>
internal sealed class InventoryMarkLookup
{
    private readonly Dictionary<long, InventoryMark> _runes;
    private readonly Dictionary<long, InventoryMark> _artifacts;
    private readonly HashSet<long> _lockedMonsters;

    private InventoryMarkLookup(IEnumerable<InventoryMark> runes, IEnumerable<InventoryMark> artifacts, IEnumerable<InventoryMark> monsters)
    {
        _runes = runes.ToDictionary(m => m.ItemId);
        _artifacts = artifacts.ToDictionary(m => m.ItemId);
        _lockedMonsters = [.. monsters.Where(m => m.IsLocked).Select(m => m.ItemId)];
    }

    public static async Task<InventoryMarkLookup> LoadAsync(IInventoryMarkRepository repository, long wizardId, CancellationToken cancellationToken)
    {
        // Em sequência: o DbContext não aceita consultas concorrentes
        var runes = await repository.ListAsync(wizardId, InventoryItemKind.Rune, cancellationToken);
        var artifacts = await repository.ListAsync(wizardId, InventoryItemKind.Artifact, cancellationToken);
        var monsters = await repository.ListAsync(wizardId, InventoryItemKind.Monster, cancellationToken);
        return new InventoryMarkLookup(runes, artifacts, monsters);
    }

    /// <summary>Bloqueio do próprio item.</summary>
    public bool IsLocked(Rune rune) => _runes.GetValueOrDefault(rune.GameId)?.IsLocked ?? false;

    public bool IsLocked(Artifact artifact) => _artifacts.GetValueOrDefault(artifact.GameId)?.IsLocked ?? false;

    public bool IsMonsterLocked(long monsterId) => _lockedMonsters.Contains(monsterId);

    /// <summary>Equipado num monstro bloqueado.</summary>
    public bool IsLockedByMonster(long? equippedMonsterId) => equippedMonsterId is { } id && IsMonsterLocked(id);

    /// <summary>Bloqueio que vale para o otimizador: o do item ou o do monstro que o usa.</summary>
    public bool IsEffectivelyLocked(Rune rune) => IsLocked(rune) || IsLockedByMonster(rune.EquippedMonsterId);

    public bool IsEffectivelyLocked(Artifact artifact) => IsLocked(artifact) || IsLockedByMonster(artifact.EquippedMonsterId);

    public IReadOnlyList<string> TagsOf(Rune rune) => _runes.GetValueOrDefault(rune.GameId)?.Tags ?? [];

    public IReadOnlyList<string> TagsOf(Artifact artifact) => _artifacts.GetValueOrDefault(artifact.GameId)?.Tags ?? [];
}
