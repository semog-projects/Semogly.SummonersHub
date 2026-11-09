using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>
/// Monstro do bestiário (dado estático do jogo), identificado pelo <c>unit_master_id</c> do export.
/// </summary>
/// <param name="Com2usId">Id do jogo. Formato FFFAE: família (FFF), despertar (A) e elemento (E).</param>
/// <param name="AwakenLevel">0 = não desperto, 1 = desperto, 2 = segundo despertar.</param>
/// <param name="MaxLevelStats">Atributos base em 6★ nível 40 (sem runas); nulo quando o bestiário não informa.</param>
/// <param name="SkillIds">Habilidades (id do jogo), na ordem do slot.</param>
public sealed record MonsterDefinition(
    int Com2usId,
    int FamilyId,
    string Name,
    Element Element,
    Archetype Archetype,
    int NaturalStars,
    int AwakenLevel,
    string ImageFilename,
    bool IsHomunculus,
    BaseStats? MaxLevelStats = null,
    IReadOnlyList<int>? SkillIds = null);
