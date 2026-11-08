import type { ArchetypeName, ElementName } from '@/lib/game/labels'

/** GET /api/accounts e /api/accounts/{wizardId}: resumo do último import. */
export interface AccountSummary {
  wizardId: number
  wizardName: string
  wizardLevel: number
  snapshotId: string
  version: number
  importedAt: string
  exportedAt: string | null
  monsters: number
  runes: number
  artifacts: number
  craftItems: number
}

export interface CountBy<TKey> {
  key: TKey
  count: number
}

export interface SpeedRankingEntry {
  monsterId: number
  masterId: number
  name: string | null
  element: ElementName
  stars: number
  level: number
  baseSpeed: number
  totalSpeed: number
  /** Nomes dos sets (Swift, Violent...), um por set completo. */
  activeSets: string[]
}

/** GET /api/accounts/{wizardId}/overview */
export interface AccountOverview {
  wizard: {
    wizardId: number
    name: string
    level: number
    serverId: number | null
    version: number
    importedAt: string
    exportedAt: string | null
  }
  monsters: number
  runes: number
  artifacts: number
  monstersByStars: CountBy<number>[]
  monstersByElement: CountBy<ElementName>[]
  monstersByAwakening: CountBy<number>[]
  speedRanking: SpeedRankingEntry[]
}

/** Atributos de um monstro (base ou total). */
export interface StatValues {
  hp: number
  attack: number
  defense: number
  speed: number
  criticalRate: number
  criticalDamage: number
  resistance: number
  accuracy: number
}

/** GET /api/accounts/{wizardId}/monsters */
export interface MonsterListItem {
  monsterId: number
  masterId: number
  name: string | null
  element: ElementName
  archetype: ArchetypeName | null
  stars: number
  level: number
  awakenLevel: number | null
  total: StatValues
  activeSets: string[]
  runes: number
  artifacts: number
}

/** Atributo com o código numérico do jogo (mesma tabela de rótulos do import). */
export interface StatCode {
  type: number
  value: number
}

export interface RuneDetail {
  runeId: number
  slot: number
  stars: number
  isAncient: boolean
  set: string
  level: number
  rarity: number
  originalRarity: number
  mainStat: StatCode
  innateStat: StatCode | null
  substats: Array<StatCode & { isGemmed: boolean; grindValue: number }>
  /** Eficiência atual em % (modelo do SW Exporter). */
  efficiency: number
  /** Eficiência com roll máximo nos upgrades que faltam até o +12. */
  maxEfficiency: number
}

export interface ArtifactDetail {
  artifactId: number
  type: 'Element' | 'Archetype'
  element: ElementName | null
  archetype: ArchetypeName | null
  level: number
  rarity: number
  originalRarity: number
  mainStat: StatCode
  substats: Array<StatCode & { upgrades: number }>
}

/** GET /api/accounts/{wizardId}/monsters/{monsterId} */
export interface MonsterDetail extends Omit<MonsterListItem, 'total' | 'runes' | 'artifacts'> {
  base: StatValues
  total: StatValues
  skills: Array<{ skillId: number; level: number }>
  runes: RuneDetail[]
  artifacts: ArtifactDetail[]
}

export interface RuneSetStats {
  set: string
  total: number
  equipped: number
  maxLevel: number
}

export interface SpeedRuneEntry {
  runeId: number
  slot: number
  set: string
  stars: number
  level: number
  isAncient: boolean
  originalRarity: number
  /** Velocidade da sub com o grind já somado (ou a inata). */
  speed: number
  grind: number
  isGemmed: boolean
  isInnate: boolean
  equippedMonsterId: number | null
  equippedMonsterName: string | null
}

export interface CraftItemStats {
  type: 'EnchantedGem' | 'Grindstone' | 'ImmemorialGem' | 'ImmemorialGrindstone' | 'AncientGem' | 'AncientGrindstone'
  set: string
  /** Código numérico do atributo (mesma tabela das runas). */
  stat: number
  rarity: number
  amount: number
}

/** GET /api/accounts/{wizardId}/inventory */
export interface InventoryStats {
  runes: {
    total: number
    equipped: number
    maxLevel: number
    ancient: number
    bySet: RuneSetStats[]
    bySlot: CountBy<number>[]
    byRarity: CountBy<number>[]
    byStars: CountBy<number>[]
    topSpeed: SpeedRuneEntry[]
  }
  artifacts: {
    total: number
    equipped: number
    maxLevel: number
    byType: CountBy<'Element' | 'Archetype'>[]
    byRarity: CountBy<number>[]
  }
  craft: {
    gems: number
    grinds: number
    items: CraftItemStats[]
  }
}

/** Monstro que equipa uma runa ou artefato. */
export interface EquippedMonster {
  monsterId: number
  masterId: number
  name: string | null
  element: ElementName
}

/** GET /api/accounts/{wizardId}/runes */
export interface RuneListItem {
  rune: RuneDetail
  /** Nulo quando a runa está no inventário. */
  equippedOn: EquippedMonster | null
}
