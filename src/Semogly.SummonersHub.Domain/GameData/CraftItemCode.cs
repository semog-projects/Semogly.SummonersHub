namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Tipo de item de craft. Os valores seguem o campo <c>craft_type</c> do jogo.</summary>
public enum CraftItemType
{
    EnchantedGem = 1,
    Grindstone = 2,
    ImmemorialGem = 3,
    ImmemorialGrindstone = 4,
    AncientGem = 5,
    AncientGrindstone = 6,
}

public static class CraftItemTypeExtensions
{
    public static bool IsGem(this CraftItemType type) =>
        type is CraftItemType.EnchantedGem or CraftItemType.ImmemorialGem or CraftItemType.AncientGem;

    public static bool IsAncient(this CraftItemType type) =>
        type is CraftItemType.AncientGem or CraftItemType.AncientGrindstone;
}

/// <summary>
/// Código <c>craft_type_id</c> de um grind/gem, no formato <c>SSEERR</c>:
/// set × 10000 + atributo × 100 + raridade. Itens ancient somam 10 à raridade (ex.: 15 = lendário ancient).
/// </summary>
/// <example>150905 = Will, Crit Rate, lendário.</example>
public readonly record struct CraftItemCode(RuneSet Set, RuneStat Stat, int Rarity)
{
    private const int AncientRarityOffset = 10;

    public static CraftItemCode Parse(int code)
    {
        var rarity = code % 100;

        return new CraftItemCode(
            (RuneSet)(code / 10000),
            (RuneStat)(code / 100 % 100),
            rarity > AncientRarityOffset ? rarity - AncientRarityOffset : rarity);
    }
}
