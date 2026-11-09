namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Habilidade de monstro (dado estático do SWARFARM), identificada pelo id do jogo.</summary>
/// <param name="Hits">Golpes por uso; a fórmula vale para cada golpe.</param>
/// <param name="FormulaText">Fórmula legível, ex.: <c>{ATK}*({SPD} + 140)/50</c>; nula sem dano.</param>
/// <param name="Formula">
/// Fórmula bruta do SWARFARM (JSON em texto): grupos avaliados em sequência, da esquerda para a direita,
/// ligados por grupos que só têm um operador. Avaliada no front, que recalcula a cada mudança do alvo.
/// </param>
/// <param name="DamageUpgrades">Bônus de dano (%) de cada skill-up, na ordem; 0 nos que não são de dano.</param>
/// <param name="Cooldown">Recarga em turnos; nula sem recarga (skill 1 ou passiva).</param>
/// <param name="LevelProgress">Texto de cada skill-up (ex.: "Damage +10%"), na ordem.</param>
/// <param name="DescriptionPt">Tradução da descrição para pt-BR; nula enquanto não traduzida.</param>
public sealed record SkillDefinition(
    int Id,
    string Name,
    int Slot,
    int Hits,
    bool Passive,
    bool Aoe,
    int MaxLevel,
    string? FormulaText,
    string? Formula,
    IReadOnlyList<int> DamageUpgrades,
    string? Description = null,
    int? Cooldown = null,
    string? IconFilename = null,
    IReadOnlyList<string>? LevelProgress = null,
    string? DescriptionPt = null)
{
    /// <summary>Bônus de dano (%) acumulado no nível: o nível 1 não tem upgrade; o nível N tem os N − 1 primeiros.</summary>
    public int DamageBonusAt(int level) => DamageUpgrades.Take(Math.Clamp(level - 1, 0, DamageUpgrades.Count)).Sum();
}
