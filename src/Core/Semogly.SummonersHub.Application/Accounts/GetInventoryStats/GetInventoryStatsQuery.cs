using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.GetInventoryStats;

/// <summary>Indicadores do inventário no último import: runas, artefatos, gems e grinds.</summary>
public sealed record GetInventoryStatsQuery(long WizardId) : IQuery<InventoryStatsResponse>
{
    public const int TopSpeedRunesSize = 20;

    /// <summary>Nível máximo de runas e artefatos.</summary>
    public const int MaxLevel = 15;
}

public sealed record InventoryStatsResponse(RuneInventory Runes, ArtifactInventory Artifacts, CraftInventory Craft);

public sealed record RuneInventory(
    int Total,
    int Equipped,
    int MaxLevel,
    int Ancient,
    IReadOnlyList<RuneSetStats> BySet,
    IReadOnlyList<CountBy<int>> BySlot,
    IReadOnlyList<CountBy<int>> ByRarity,
    IReadOnlyList<CountBy<int>> ByStars,
    IReadOnlyList<SpeedRuneEntry> TopSpeed);

/// <summary>Runas de um set: total, quantas estão equipadas e quantas estão em +15.</summary>
public sealed record RuneSetStats(RuneSet Set, int Total, int Equipped, int MaxLevel);

/// <summary>
/// Runa com velocidade nas subs. <c>Speed</c> já inclui o grind; <c>IsInnate</c> indica que a velocidade
/// é a inata (que não pode ser gemada nem grindada).
/// </summary>
public sealed record SpeedRuneEntry(
    long RuneId,
    int Slot,
    RuneSet Set,
    int Stars,
    int Level,
    bool IsAncient,
    int OriginalRarity,
    int Speed,
    int Grind,
    bool IsGemmed,
    bool IsInnate,
    long? EquippedMonsterId,
    string? EquippedMonsterName);

public sealed record ArtifactInventory(
    int Total,
    int Equipped,
    int MaxLevel,
    IReadOnlyList<CountBy<ArtifactType>> ByType,
    IReadOnlyList<CountBy<int>> ByRarity);

public sealed record CraftInventory(int Gems, int Grinds, IReadOnlyList<CraftItemStats> Items);

/// <summary>Gem ou grind decodificado. <c>Stat</c> vai com o código numérico do jogo, como nas runas.</summary>
public sealed record CraftItemStats(CraftItemType Type, RuneSet Set, int Stat, int Rarity, int Amount);

internal sealed class GetInventoryStatsQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<GetInventoryStatsQuery, InventoryStatsResponse>
{
    private static readonly int[] Slots = [1, 2, 3, 4, 5, 6];
    private static readonly int[] Rarities = [1, 2, 3, 4, 5];
    private static readonly int[] Stars = [1, 2, 3, 4, 5, 6];

    public async Task<Result<InventoryStatsResponse>> HandleAsync(GetInventoryStatsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var snapshot = result.Value;
        return new InventoryStatsResponse(Runes(snapshot), Artifacts(snapshot.Artifacts), Craft(snapshot.CraftItems));
    }

    private RuneInventory Runes(AccountSnapshot snapshot)
    {
        var runes = snapshot.Runes;
        var monsterNames = snapshot.Monsters.ToDictionary(m => m.GameId, m => catalog.Find(m.MasterId)?.Name);

        return new RuneInventory(
            runes.Count,
            runes.Count(r => r.EquippedMonsterId is not null),
            runes.Count(IsMaxLevel),
            runes.Count(r => r.IsAncient),
            [.. runes
                .GroupBy(r => r.Set)
                .Select(g => new RuneSetStats(g.Key, g.Count(), g.Count(r => r.EquippedMonsterId is not null), g.Count(IsMaxLevel)))
                .OrderByDescending(s => s.Total)
                .ThenBy(s => s.Set)],
            [.. Slots.Select(slot => new CountBy<int>(slot, runes.Count(r => r.Slot == slot)))],
            // A cor da runa (raridade original) é o que define quantas subs ela pode ganhar
            [.. Rarities.Select(rarity => new CountBy<int>(rarity, runes.Count(r => r.OriginalRarity == rarity)))],
            [.. Stars.Select(stars => new CountBy<int>(stars, runes.Count(r => r.Stars == stars)))],
            [.. runes
                .Select(r => ToSpeedEntry(r, monsterNames))
                .OfType<SpeedRuneEntry>()
                .OrderByDescending(e => e.Speed)
                .ThenByDescending(e => e.Level)
                .ThenBy(e => e.RuneId)
                .Take(GetInventoryStatsQuery.TopSpeedRunesSize)]);
    }

    private static SpeedRuneEntry? ToSpeedEntry(Rune rune, Dictionary<long, string?> monsterNames)
    {
        // Uma runa não repete atributo: a velocidade está na inata ou em uma das subs, nunca nas duas
        var sub = rune.Substats.FirstOrDefault(s => s.Stat == RuneStat.Speed);
        var innate = rune.InnateStat is { Stat: RuneStat.Speed } i ? i : null;

        if (sub is null && innate is null)
        {
            return null;
        }

        var equippedName = rune.EquippedMonsterId is { } monsterId ? monsterNames.GetValueOrDefault(monsterId) : null;

        return new SpeedRuneEntry(
            rune.GameId,
            rune.Slot,
            rune.Set,
            rune.Stars,
            rune.Level,
            rune.IsAncient,
            rune.OriginalRarity,
            sub is not null ? sub.Value + sub.GrindValue : innate!.Value,
            sub?.GrindValue ?? 0,
            sub?.IsGemmed ?? false,
            sub is null,
            rune.EquippedMonsterId,
            equippedName);
    }

    private static ArtifactInventory Artifacts(IReadOnlyCollection<Artifact> artifacts) => new(
        artifacts.Count,
        artifacts.Count(a => a.EquippedMonsterId is not null),
        artifacts.Count(a => a.Level >= GetInventoryStatsQuery.MaxLevel),
        [.. Enum.GetValues<ArtifactType>().Select(type => new CountBy<ArtifactType>(type, artifacts.Count(a => a.Type == type)))],
        [.. Rarities.Select(rarity => new CountBy<int>(rarity, artifacts.Count(a => a.OriginalRarity == rarity)))]);

    private static CraftInventory Craft(IReadOnlyCollection<CraftItem> items)
    {
        var decoded = items
            .Select(item => (Item: item, Code: item.Decode()))
            .GroupBy(x => (x.Item.Type, x.Code.Set, x.Code.Stat, x.Code.Rarity))
            .Select(g => new CraftItemStats(g.Key.Type, g.Key.Set, (int)g.Key.Stat, g.Key.Rarity, g.Sum(x => x.Item.Amount)))
            .OrderBy(c => c.Type.IsGem() ? 0 : 1)
            .ThenBy(c => c.Set)
            .ThenBy(c => c.Stat)
            .ThenByDescending(c => c.Rarity)
            .ToList();

        return new CraftInventory(
            decoded.Where(c => c.Type.IsGem()).Sum(c => c.Amount),
            decoded.Where(c => !c.Type.IsGem()).Sum(c => c.Amount),
            decoded);
    }

    private static bool IsMaxLevel(Rune rune) => rune.Level >= GetInventoryStatsQuery.MaxLevel;
}
