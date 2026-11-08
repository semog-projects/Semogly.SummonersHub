using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>Monstro da conta (instância), com os atributos base sem runas e artefatos.</summary>
public sealed class Monster
{
    // Construtor do EF Core
    private Monster()
    {
        BaseStats = null!;
        Skills = null!;
    }

    public Monster(
        long gameId,
        int masterId,
        int level,
        int stars,
        Element element,
        BaseStats baseStats,
        IReadOnlyList<MonsterSkill> skills,
        bool isHomunculus,
        DateTime? createdAt)
    {
        GameId = gameId;
        MasterId = masterId;
        Level = level;
        Stars = stars;
        Element = element;
        BaseStats = baseStats;
        Skills = [.. skills];
        IsHomunculus = isHomunculus;
        CreatedAt = createdAt;
    }

    /// <summary>Id da instância no jogo (<c>unit_id</c>).</summary>
    public long GameId { get; private set; }

    /// <summary>Id do bestiário (<c>unit_master_id</c>), ver <see cref="MonsterDefinition"/>.</summary>
    public int MasterId { get; private set; }

    public int Level { get; private set; }

    public int Stars { get; private set; }

    public Element Element { get; private set; }

    public BaseStats BaseStats { get; private set; }

    public List<MonsterSkill> Skills { get; private set; }

    public bool IsHomunculus { get; private set; }

    /// <summary>
    /// Quando o monstro foi obtido, no horário do servidor do jogo (o export não informa o fuso).
    /// </summary>
    public DateTime? CreatedAt { get; private set; }
}

public sealed record BaseStats(
    int Hp,
    int Attack,
    int Defense,
    int Speed,
    int Resistance,
    int Accuracy,
    int CriticalRate,
    int CriticalDamage);

/// <param name="SkillId">Id da habilidade no jogo.</param>
public sealed record MonsterSkill(int SkillId, int Level);
