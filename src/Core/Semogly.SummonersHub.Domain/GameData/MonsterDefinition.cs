using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>
/// Monstro do bestiário (dado estático do jogo), identificado pelo <c>unit_master_id</c> do export.
/// </summary>
/// <param name="Com2usId">Id do jogo. Formato FFFAE: família (FFF), despertar (A) e elemento (E).</param>
/// <param name="AwakenLevel">0 = não desperto, 1 = desperto, 2 = segundo despertar.</param>
/// <param name="MaxLevelStats">Atributos base em 6★ nível 40 (sem runas); nulo quando o bestiário não informa.</param>
/// <param name="SkillIds">Habilidades (id do jogo), na ordem do slot.</param>
/// <param name="Obtainable">Falso para monstros de NPC/boss que o jogador não consegue obter.</param>
/// <param name="AwakenBonus">Bônus de despertar, como no bestiário (ex.: "Increases the Critical Rate").</param>
/// <param name="Sources">Onde obter (pergaminhos, invocações, craft...), como no SWARFARM.</param>
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
    IReadOnlyList<int>? SkillIds = null,
    bool Obtainable = true,
    LeaderSkill? LeaderSkill = null,
    string? AwakenBonus = null,
    IReadOnlyList<string>? Sources = null);

/// <summary>Skill de líder.</summary>
/// <param name="Attribute">Atributo bonificado, como no SWARFARM (ex.: "Attack Power", "Attack Speed").</param>
/// <param name="Area">Onde vale: General, Element, Arena, Guild, Dungeon, Raid...</param>
/// <param name="Element">Elemento exigido quando <paramref name="Area"/> é Element.</param>
public sealed record LeaderSkill(string Attribute, int Amount, string Area, string? Element);
