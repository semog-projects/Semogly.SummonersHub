/**
 * Payload enviado à API. Contém apenas o necessário para o painel da conta: nenhum dado de
 * sessão, autenticação ou dispositivo do export original sai do navegador.
 */
export interface AccountImport {
  schemaVersion: 1
  /** Momento do export (servidor do jogo), em ISO 8601. */
  exportedAt: string | null
  wizard: Wizard
  monsters: Monster[]
  runes: Rune[]
  artifacts: Artifact[]
  craftItems: CraftItem[]
}

export interface Wizard {
  id: number
  name: string
  level: number
  serverId: number | null
}

export interface Monster {
  id: number
  masterId: number
  level: number
  stars: number
  /** Elemento (1 água, 2 fogo, 3 vento, 4 luz, 5 trevas). */
  attribute: number
  baseStats: BaseStats
  skills: MonsterSkill[]
  isHomunculus: boolean
  /** Quando o monstro foi obtido (ISO sem fuso, horário do servidor do jogo). */
  createdAt: string | null
}

export interface BaseStats {
  hp: number
  attack: number
  defense: number
  speed: number
  resistance: number
  accuracy: number
  criticalRate: number
  criticalDamage: number
}

export interface MonsterSkill {
  id: number
  level: number
}

export interface Stat {
  type: number
  value: number
}

export interface RuneSubstat extends Stat {
  /** Sub substituída por gem (enchant). */
  isGemmed: boolean
  /** Valor adicionado por grind. */
  grindValue: number
}

export interface Rune {
  id: number
  slot: number
  /** Estrelas (1 a 6), já sem o marcador de ancient. */
  stars: number
  isAncient: boolean
  setId: number
  level: number
  /** Raridade atual (1 normal a 5 lendária). */
  rarity: number
  /** Raridade com que a runa caiu (1 a 5). */
  originalRarity: number
  mainStat: Stat
  innateStat: Stat | null
  substats: RuneSubstat[]
  equippedMonsterId: number | null
}

export interface ArtifactSubstat extends Stat {
  /** Quantas vezes a sub foi melhorada no upgrade. */
  upgrades: number
}

export interface Artifact {
  id: number
  /** 1 = elemento, 2 = arquétipo. */
  type: number
  attribute: number
  archetype: number
  rarity: number
  originalRarity: number
  level: number
  mainStat: Stat
  substats: ArtifactSubstat[]
  equippedMonsterId: number | null
}

export interface CraftItem {
  id: number
  /** Tipo de item de craft (grind, gem...). */
  craftType: number
  /** Código que combina set, efeito e raridade do item. */
  craftTypeId: number
  amount: number
}
