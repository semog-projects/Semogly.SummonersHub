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
/// <param name="Debuffer">A vaga aplica debuffs: a PRE mínima vem da RES do boss da dungeon (<see cref="DungeonStats"/>).</param>
public sealed record PveTeamSlot(
    string MonsterName,
    Element Element,
    string Role,
    IReadOnlyList<StatRequirement> Requirements,
    bool Debuffer = false);

public sealed record StatRequirement(PveStat Stat, int Min);

/// <summary>
/// Inimigos do andar mais alto de uma dungeon (dados do SWARFARM): RES e VEL do boss e de todo o andar.
/// </summary>
public sealed record DungeonStats(
    Dungeon Dungeon,
    string Name,
    int Floor,
    int BossResistance,
    int MaxResistance,
    int BossSpeed,
    int MaxSpeed)
{
    /// <summary>A resistência efetiva do alvo nunca cai abaixo de 15%, qualquer que seja a PRE.</summary>
    public const int ResistanceFloor = 15;

    /// <summary>
    /// PRE que leva a resistência efetiva do boss ao piso: RES efetiva = máx(15, RES − PRE),
    /// então PRE ≥ RES − 15 já dá a maior chance de acerto dos debuffs.
    /// </summary>
    public int AccuracyTarget => Math.Max(0, BossResistance - ResistanceFloor);
}
