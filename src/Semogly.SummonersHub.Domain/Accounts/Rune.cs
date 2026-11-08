using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

public sealed class Rune
{
    public const int MaxLevel = 15;

    // Construtor do EF Core
    private Rune()
    {
        MainStat = null!;
        Substats = null!;
    }

    public Rune(
        long gameId,
        int slot,
        int stars,
        bool isAncient,
        RuneSet set,
        int level,
        int rarity,
        int originalRarity,
        RuneStatValue mainStat,
        RuneStatValue? innateStat,
        IReadOnlyList<RuneSubstat> substats,
        long? equippedMonsterId)
    {
        if (slot is < 1 or > 6)
        {
            throw new DomainException($"Slot de runa inválido: {slot}.");
        }

        if (stars is < 1 or > 6)
        {
            throw new DomainException($"Estrelas de runa inválidas: {stars}.");
        }

        if (level is < 0 or > MaxLevel)
        {
            throw new DomainException($"Nível de runa inválido: {level}.");
        }

        if (substats.Count > 4)
        {
            throw new DomainException("Uma runa tem no máximo 4 subs.");
        }

        GameId = gameId;
        Slot = slot;
        Stars = stars;
        IsAncient = isAncient;
        Set = set;
        Level = level;
        Rarity = rarity;
        OriginalRarity = originalRarity;
        MainStat = mainStat;
        InnateStat = innateStat;
        Substats = [.. substats];
        EquippedMonsterId = equippedMonsterId;
    }

    /// <summary>Id da runa no jogo (<c>rune_id</c>).</summary>
    public long GameId { get; private set; }

    public int Slot { get; private set; }

    public int Stars { get; private set; }

    public bool IsAncient { get; private set; }

    public RuneSet Set { get; private set; }

    public int Level { get; private set; }

    /// <summary>Raridade atual (1 comum a 5 lendária).</summary>
    public int Rarity { get; private set; }

    /// <summary>Raridade com que a runa caiu.</summary>
    public int OriginalRarity { get; private set; }

    public RuneStatValue MainStat { get; private set; }

    public RuneStatValue? InnateStat { get; private set; }

    public List<RuneSubstat> Substats { get; private set; }

    /// <summary><see cref="Monster.GameId"/> do monstro que usa a runa, ou nulo se está no inventário.</summary>
    public long? EquippedMonsterId { get; private set; }
}

public sealed record RuneStatValue(RuneStat Stat, int Value);

/// <param name="IsGemmed">A sub foi trocada por gem.</param>
/// <param name="GrindValue">Valor somado por grind.</param>
public sealed record RuneSubstat(RuneStat Stat, int Value, bool IsGemmed, int GrindValue);
