namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>
/// Monstro do bestiário (dado estático do jogo), identificado pelo <c>unit_master_id</c> do export.
/// </summary>
/// <param name="Com2usId">Id do jogo. Formato FFFAE: família (FFF), despertar (A) e elemento (E).</param>
/// <param name="AwakenLevel">0 = não desperto, 1 = desperto, 2 = segundo despertar.</param>
public sealed record MonsterDefinition(
    int Com2usId,
    int FamilyId,
    string Name,
    Element Element,
    Archetype Archetype,
    int NaturalStars,
    int AwakenLevel,
    string ImageFilename,
    bool IsHomunculus);
