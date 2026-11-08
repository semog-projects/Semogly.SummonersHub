using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>Grind ou gem no inventário.</summary>
public sealed class CraftItem
{
    // Construtor do EF Core
    private CraftItem()
    {
    }

    public CraftItem(long gameId, CraftItemType type, int code, int amount)
    {
        if (amount < 1)
        {
            throw new DomainException("A quantidade de um item de craft deve ser positiva.");
        }

        GameId = gameId;
        Type = type;
        Code = code;
        Amount = amount;
    }

    public long GameId { get; private set; }

    public CraftItemType Type { get; private set; }

    /// <summary><c>craft_type_id</c> do jogo; ver <see cref="CraftItemCode"/>.</summary>
    public int Code { get; private set; }

    public int Amount { get; private set; }

    public CraftItemCode Decode() => CraftItemCode.Parse(Code);
}
