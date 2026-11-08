namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Conteúdo PvE coberto pelos times de referência.</summary>
public enum Dungeon
{
    GiantsKeep,
    DragonsLair,
    Necropolis,
    SpiritualRealm,
    PunishersCrypt,
}

/// <summary>Atributo total do monstro usado como requisito mínimo.</summary>
public enum PveStat
{
    Hp,
    Attack,
    Defense,
    Speed,
    CriticalRate,
    CriticalDamage,
    Resistance,
    Accuracy,
}

/// <summary>
/// Time de referência para uma dungeon (dado estático e editável). Pode listar só o núcleo do time:
/// as vagas restantes ficam livres para o jogador.
/// </summary>
public sealed record PveTeam(string Id, Dungeon Dungeon, string Name, string? Notes, IReadOnlyList<PveTeamSlot> Slots);

/// <summary>
/// Vaga do time: monstro pelo nome desperto e elemento (a forma 2A tem o mesmo nome e também conta),
/// o papel no time e os atributos totais mínimos.
/// </summary>
public sealed record PveTeamSlot(string MonsterName, Element Element, string Role, IReadOnlyList<StatRequirement> Requirements);

public sealed record StatRequirement(PveStat Stat, int Min);
