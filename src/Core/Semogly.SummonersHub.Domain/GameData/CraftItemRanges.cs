namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Faixa de valor que um grind ou gem pode sortear.</summary>
public readonly record struct ValueRange(int Min, int Max);

/// <summary>
/// Faixas de valor de grinds e gems por atributo e raridade (1 comum a 5 lendária), copiadas do SW Exporter
/// (<c>app/mapping.js</c>, versão de 2025-09-29). Itens ancient não estão na fonte e ficam sem faixa.
/// </summary>
public static class CraftItemRanges
{
    // Índice 0 = raridade 1 (comum)
    private static readonly Dictionary<RuneStat, ValueRange[]> Grindstone = new()
    {
        [RuneStat.HpFlat] = [new(80, 120), new(100, 200), new(180, 250), new(230, 450), new(430, 550)],
        [RuneStat.HpPercent] = [new(1, 3), new(2, 5), new(3, 6), new(4, 7), new(5, 10)],
        [RuneStat.AttackFlat] = [new(4, 8), new(6, 12), new(10, 18), new(12, 22), new(18, 30)],
        [RuneStat.AttackPercent] = [new(1, 3), new(2, 5), new(3, 6), new(4, 7), new(5, 10)],
        [RuneStat.DefenseFlat] = [new(4, 8), new(6, 12), new(10, 18), new(12, 22), new(18, 30)],
        [RuneStat.DefensePercent] = [new(1, 3), new(2, 5), new(3, 6), new(4, 7), new(5, 10)],
        [RuneStat.Speed] = [new(1, 2), new(1, 2), new(2, 3), new(3, 4), new(4, 5)],
    };

    private static readonly Dictionary<RuneStat, ValueRange[]> EnchantedGem = new()
    {
        [RuneStat.HpFlat] = [new(100, 150), new(130, 220), new(200, 310), new(290, 420), new(400, 580)],
        [RuneStat.HpPercent] = [new(2, 4), new(3, 7), new(5, 9), new(7, 11), new(9, 13)],
        [RuneStat.AttackFlat] = [new(8, 12), new(10, 16), new(15, 23), new(20, 30), new(28, 40)],
        [RuneStat.AttackPercent] = [new(2, 4), new(3, 7), new(5, 9), new(7, 11), new(9, 13)],
        [RuneStat.DefenseFlat] = [new(8, 12), new(10, 16), new(15, 23), new(20, 30), new(28, 40)],
        [RuneStat.DefensePercent] = [new(2, 4), new(3, 7), new(5, 9), new(7, 11), new(9, 13)],
        [RuneStat.Speed] = [new(1, 3), new(2, 4), new(3, 6), new(5, 8), new(7, 10)],
        [RuneStat.CriticalRate] = [new(1, 3), new(2, 4), new(3, 5), new(4, 7), new(6, 9)],
        [RuneStat.CriticalDamage] = [new(1, 3), new(3, 5), new(4, 6), new(5, 8), new(7, 10)],
        [RuneStat.Resistance] = [new(2, 4), new(3, 6), new(5, 8), new(6, 9), new(8, 11)],
        [RuneStat.Accuracy] = [new(2, 4), new(3, 6), new(5, 8), new(6, 9), new(8, 11)],
    };

    /// <summary>Faixa do item, ou nulo quando desconhecida (ancient) ou inválida.</summary>
    public static ValueRange? For(CraftItemType type, RuneStat stat, int rarity)
    {
        if (type.IsAncient() || rarity is < 1 or > 5)
        {
            return null;
        }

        var table = type.IsGem() ? EnchantedGem : Grindstone;
        return table.TryGetValue(stat, out var ranges) ? ranges[rarity - 1] : null;
    }
}
