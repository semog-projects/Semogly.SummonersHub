/**
 * Rótulos em pt-BR dos códigos do jogo. Os códigos são os mesmos do export do SW Exporter e dos
 * enums do domínio (Semogly.SummonersHub.Domain.GameData). Nomes de sets seguem o original em inglês,
 * como a comunidade costuma usar.
 */

export const ELEMENT_LABELS: Record<number, string> = {
  1: 'Água',
  2: 'Fogo',
  3: 'Vento',
  4: 'Luz',
  5: 'Trevas',
  6: 'Puro',
}

export const ARCHETYPE_LABELS: Record<number, string> = {
  0: 'Nenhum',
  1: 'Ataque',
  2: 'Defesa',
  3: 'HP',
  4: 'Suporte',
  5: 'Material',
}

export const RUNE_SET_LABELS: Record<number, string> = {
  1: 'Energy',
  2: 'Guard',
  3: 'Swift',
  4: 'Blade',
  5: 'Rage',
  6: 'Focus',
  7: 'Endure',
  8: 'Fatal',
  10: 'Despair',
  11: 'Vampire',
  13: 'Violent',
  14: 'Nemesis',
  15: 'Will',
  16: 'Shield',
  17: 'Revenge',
  18: 'Destroy',
  19: 'Fight',
  20: 'Determination',
  21: 'Enhance',
  22: 'Accuracy',
  23: 'Tolerance',
  24: 'Seal',
  25: 'Intangible',
  99: 'Immemorial',
}

/** Atributos de runa. `percent` indica se o valor é exibido com %. */
export const RUNE_STATS: Record<number, { label: string; percent: boolean }> = {
  1: { label: 'HP', percent: false },
  2: { label: 'HP', percent: true },
  3: { label: 'ATK', percent: false },
  4: { label: 'ATK', percent: true },
  5: { label: 'DEF', percent: false },
  6: { label: 'DEF', percent: true },
  8: { label: 'VEL', percent: false },
  9: { label: 'Taxa Crít.', percent: true },
  10: { label: 'Dano Crít.', percent: true },
  11: { label: 'RES', percent: true },
  12: { label: 'PRE', percent: true },
}

export const RARITY_LABELS: Record<number, string> = {
  1: 'Comum',
  2: 'Mágica',
  3: 'Rara',
  4: 'Heroica',
  5: 'Lendária',
}

export const CRAFT_TYPE_LABELS: Record<number, string> = {
  1: 'Gem',
  2: 'Grind',
  3: 'Gem Immemorial',
  4: 'Grind Immemorial',
  5: 'Gem Ancient',
  6: 'Grind Ancient',
}

export const ARTIFACT_MAIN_STAT_LABELS: Record<number, string> = {
  100: 'HP',
  101: 'ATK',
  102: 'DEF',
}

/** Subs de artefato. `{v}` é substituído pelo valor. */
export const ARTIFACT_EFFECT_LABELS: Record<number, string> = {
  200: 'ATK aumenta conforme o HP perdido, até {v}%',
  201: 'DEF aumenta conforme o HP perdido, até {v}%',
  202: 'VEL aumenta conforme o HP perdido, até {v}%',
  203: 'VEL sob efeitos de incapacidade +{v}%',
  204: 'Efeito de aumento de ATK +{v}%',
  205: 'Efeito de aumento de DEF +{v}%',
  206: 'Efeito de aumento de VEL +{v}%',
  207: 'Efeito de aumento de Taxa Crít. +{v}%',
  208: 'Dano de contra-ataque +{v}%',
  209: 'Dano de ataque conjunto +{v}%',
  210: 'Dano de bomba +{v}%',
  211: 'Dano refletido +{v}%',
  212: 'Dano de golpe esmagador +{v}%',
  213: 'Dano recebido sob incapacidade -{v}%',
  214: 'Dano crítico recebido -{v}%',
  215: 'Roubo de vida +{v}%',
  216: 'HP ao reviver +{v}%',
  217: 'Barra de ataque ao reviver +{v}%',
  218: 'Dano adicional de {v}% do HP',
  219: 'Dano adicional de {v}% do ATK',
  220: 'Dano adicional de {v}% da DEF',
  221: 'Dano adicional de {v}% da VEL',
  222: 'Dano Crít. +{v}% conforme o HP do inimigo está alto',
  223: 'Dano Crít. +{v}% conforme o HP do inimigo está baixo',
  224: 'Dano Crít. de habilidade de alvo único +{v}% no seu turno',
  225: 'Dano de contra-ataque/ataque conjunto +{v}%',
  226: 'Efeito de aumento de ATK/DEF +{v}%',
  300: 'Dano causado a Fogo +{v}%',
  301: 'Dano causado a Água +{v}%',
  302: 'Dano causado a Vento +{v}%',
  303: 'Dano causado a Luz +{v}%',
  304: 'Dano causado a Trevas +{v}%',
  305: 'Dano recebido de Fogo -{v}%',
  306: 'Dano recebido de Água -{v}%',
  307: 'Dano recebido de Vento -{v}%',
  308: 'Dano recebido de Luz -{v}%',
  309: 'Dano recebido de Trevas -{v}%',
  400: 'Dano Crít. da habilidade 1 +{v}%',
  401: 'Dano Crít. da habilidade 2 +{v}%',
  402: 'Dano Crít. da habilidade 3 +{v}%',
  403: 'Dano Crít. da habilidade 4 +{v}%',
  404: 'Recuperação da habilidade 1 +{v}%',
  405: 'Recuperação da habilidade 2 +{v}%',
  406: 'Recuperação da habilidade 3 +{v}%',
  407: 'Precisão da habilidade 1 +{v}%',
  408: 'Precisão da habilidade 2 +{v}%',
  409: 'Precisão da habilidade 3 +{v}%',
  410: 'Dano Crít. das habilidades 3/4 +{v}%',
  411: 'Dano Crít. do primeiro ataque +{v}%',
}

/** Formata um atributo de runa, ex.: `VEL +24`, `ATK +63%`. */
export function formatRuneStat(type: number, value: number): string {
  const stat = RUNE_STATS[type]
  if (!stat) return `Atributo ${type} +${value}`
  return `${stat.label} +${value}${stat.percent ? '%' : ''}`
}

/** Formata uma sub de artefato com o valor, ex.: `Dano adicional de 31% da VEL`. */
export function formatArtifactEffect(type: number, value: number): string {
  const template = ARTIFACT_EFFECT_LABELS[type]
  return template ? template.replace('{v}', String(value)) : `Efeito ${type} (${value})`
}

/** Decodifica o `craft_type_id` de grind/gem: set × 10000 + atributo × 100 + raridade (ancient soma 10). */
export function decodeCraftTypeId(code: number): { setId: number; statType: number; rarity: number } {
  const rarity = code % 100
  return {
    setId: Math.floor(code / 10000),
    statType: Math.floor(code / 100) % 100,
    rarity: rarity > 10 ? rarity - 10 : rarity,
  }
}
