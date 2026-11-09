using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Itens criados pelo usuário para planejar (monstros que ainda não tem, gems e grinds). Ficam fora das versões
/// do import e entram na leitura da conta junto do último import. Usam ids negativos, que nunca colidem com os
/// ids do jogo.
/// </summary>
public static class ManualItem
{
    public const int MaxPerWizard = 200;

    public static bool IsManual(long gameId) => gameId < 0;
}

/// <summary>Monstro manual: sempre 6★ nível 40, com os atributos base do bestiário e skills no máximo.</summary>
public sealed class ManualMonster
{
    public const int Level = 40;

    public const int Stars = 6;

    // Construtor do EF Core
    private ManualMonster()
    {
    }

    private ManualMonster(Guid id, long wizardId, long gameId, int masterId, DateTimeOffset createdAt)
    {
        Id = id;
        WizardId = wizardId;
        GameId = gameId;
        MasterId = masterId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    /// <summary>Id do invocador no jogo.</summary>
    public long WizardId { get; private set; }

    /// <summary>Id negativo, no lugar do <c>unit_id</c> do jogo.</summary>
    public long GameId { get; private set; }

    /// <summary>Monstro do bestiário (<c>unit_master_id</c>).</summary>
    public int MasterId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ManualMonster Create(long wizardId, long gameId, int masterId, DateTimeOffset now)
    {
        if (wizardId <= 0 || !ManualItem.IsManual(gameId) || masterId <= 0)
        {
            throw new DomainException("Monstro manual com ids inválidos.");
        }

        return new ManualMonster(Guid.CreateVersion7(now), wizardId, gameId, masterId, now);
    }

    /// <param name="baseStats">Atributos base em 6★ nível 40, do bestiário.</param>
    /// <param name="skills">Habilidades no nível máximo.</param>
    public Monster ToMonster(Element element, BaseStats baseStats, IReadOnlyList<MonsterSkill> skills, bool isHomunculus) =>
        new(GameId, MasterId, Level, Stars, element, baseStats, skills, isHomunculus, CreatedAt.UtcDateTime);
}

/// <summary>Gem ou grind manual, somada ao inventário do último import.</summary>
public sealed class ManualCraftItem
{
    public const int MaxAmount = 999;

    // Construtor do EF Core
    private ManualCraftItem()
    {
    }

    private ManualCraftItem(Guid id, long wizardId, long gameId, CraftItemType type, int code, int amount, DateTimeOffset createdAt)
    {
        Id = id;
        WizardId = wizardId;
        GameId = gameId;
        Type = type;
        Code = code;
        Amount = amount;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public long WizardId { get; private set; }

    /// <summary>Id negativo, no lugar do id do item no jogo.</summary>
    public long GameId { get; private set; }

    public CraftItemType Type { get; private set; }

    /// <summary>Código no formato do jogo (<see cref="CraftItemCode"/>).</summary>
    public int Code { get; private set; }

    public int Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gem (Enchanted) ou grind de um set, atributo e raridade (1 comum a 5 lendária).</summary>
    public static ManualCraftItem Create(long wizardId, long gameId, CraftItemType type, RuneSet set, RuneStat stat, int rarity, int amount, DateTimeOffset now)
    {
        if (wizardId <= 0 || !ManualItem.IsManual(gameId))
        {
            throw new DomainException("Item manual com ids inválidos.");
        }

        if (type is not (CraftItemType.EnchantedGem or CraftItemType.Grindstone))
        {
            throw new DomainException("Só é possível criar gems e grinds comuns (não ancient nem immemorial).");
        }

        if (!Enum.IsDefined(set) || set.RequiredPieces() == 0)
        {
            throw new DomainException("Set inválido para gem ou grind.");
        }

        if (!Enum.IsDefined(stat) || rarity is < 1 or > 5 || CraftItemRanges.For(type, stat, rarity) is null)
        {
            throw new DomainException(type == CraftItemType.Grindstone
                ? "Grind só existe para HP, ATK, DEF e VEL, com raridade de 1 a 5."
                : "Atributo ou raridade inválidos (raridade de 1 a 5).");
        }

        if (amount is < 1 or > MaxAmount)
        {
            throw new DomainException($"A quantidade deve ser de 1 a {MaxAmount}.");
        }

        var code = ((int)set * 10000) + ((int)stat * 100) + rarity;
        return new ManualCraftItem(Guid.CreateVersion7(now), wizardId, gameId, type, code, amount, now);
    }

    public CraftItem ToCraftItem() => new(GameId, Type, Code, Amount);
}

public interface IManualItemRepository
{
    Task<List<ManualMonster>> ListMonstersAsync(long wizardId, CancellationToken cancellationToken);

    Task<List<ManualCraftItem>> ListCraftItemsAsync(long wizardId, CancellationToken cancellationToken);

    /// <summary>Próximo id negativo livre do invocador (comum a monstros e itens).</summary>
    Task<long> NextGameIdAsync(long wizardId, CancellationToken cancellationToken);

    /// <summary>Quantos itens manuais (monstros + gems/grinds) o invocador tem.</summary>
    Task<int> CountAsync(long wizardId, CancellationToken cancellationToken);

    void Add(ManualMonster monster);

    void Add(ManualCraftItem item);

    void Remove(ManualMonster monster);

    void Remove(ManualCraftItem item);
}
