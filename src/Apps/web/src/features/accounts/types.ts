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
  /** HP × (1140 + 3,5 × DEF) ÷ 1000, calculado pela API. */
  effectiveHp: number
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
  /** Eficiência média das runas equipadas, ou nulo sem runas. */
  averageRuneEfficiency: number | null
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

/** GET /api/accounts/{wizardId}/artifacts */
export interface ArtifactListItem {
  artifact: ArtifactDetail
  /** Nulo quando o artefato está no inventário. */
  equippedOn: EquippedMonster | null
}

/** Valor na versão de referência e na comparada. */
export interface Change<T> {
  from: T
  to: T
}

export interface MonsterChangeItem {
  monsterId: number
  masterId: number
  name: string | null
  element: ElementName
  stars: number
  level: number
}

export interface MonsterProgress {
  monsterId: number
  masterId: number
  name: string | null
  element: ElementName
  stars: Change<number>
  level: Change<number>
  awakenLevel: Change<number | null>
  speed: Change<number>
}

export interface EquipmentChanges {
  total: Change<number>
  added: number
  removed: number
  /** Itens presentes nas duas versões que subiram de nível. */
  upgraded: number
}

/** GET /api/accounts/{wizardId}/snapshots/compare?from=&to= */
export interface SnapshotComparison {
  from: { version: number; importedAt: string; exportedAt: string | null }
  to: { version: number; importedAt: string; exportedAt: string | null }
  wizardLevel: Change<number>
  monsters: Change<number>
  addedMonsters: MonsterChangeItem[]
  removedMonsters: MonsterChangeItem[]
  progressedMonsters: MonsterProgress[]
  runes: EquipmentChanges
  artifacts: EquipmentChanges
  averageRuneEfficiency: Change<number>
  topNewRunes: RuneDetail[]
}

export type DungeonName = 'GiantsKeep' | 'DragonsLair' | 'Necropolis' | 'SpiritualRealm' | 'PunishersCrypt'

/** Atributo de requisito, como a API devolve (enum pelo nome). */
export type PveStatName = 'Hp' | 'Attack' | 'Defense' | 'Speed' | 'CriticalRate' | 'CriticalDamage' | 'Resistance' | 'Accuracy'

export interface PveSlotCheck {
  monsterName: string
  element: ElementName
  role: string
  /** A PRE mínima vem da RES do boss da dungeon. */
  debuffer: boolean
  requirements: Array<{ stat: PveStatName; min: number }>
  status: 'Missing' | 'BelowRequirements' | 'Ready'
  /** Melhor candidato da conta para a vaga. */
  monster: {
    monsterId: number
    masterId: number
    name: string | null
    element: ElementName
    stars: number
    level: number
    total: StatValues
  } | null
  unmet: Array<{ stat: PveStatName; required: number; current: number }>
}

/** GET /api/accounts/{wizardId}/pve-teams */
export interface PveTeamCheck {
  id: string
  dungeon: DungeonName
  /** Andar mais alto da dungeon, com dados do SWARFARM. */
  dungeonStats: {
    floor: number
    bossResistance: number
    maxResistance: number
    bossSpeed: number
    maxSpeed: number
  } | null
  name: string
  notes: string | null
  readySlots: number
  isReady: boolean
  slots: PveSlotCheck[]
}

export interface CraftSuggestionItem {
  rune: RuneDetail
  equippedOn: EquippedMonster | null
  /** Sub que recebe o grind ou é trocada pela gem (código do atributo). */
  targetStat: number
  currentValue: number
  minValue: number
  maxValue: number
  /** Pontos de eficiência no pior e no melhor sorteio (o pior pode ser negativo na gem). */
  minEfficiencyGain: number
  maxEfficiencyGain: number
}

/** GET /api/accounts/{wizardId}/craft-suggestions */
export interface CraftSuggestionGroup {
  type: CraftItemStats['type']
  set: string
  stat: number
  rarity: number
  amount: number
  /** Nulo nos itens ancient (faixa desconhecida). */
  range: { min: number; max: number } | null
  suggestions: CraftSuggestionItem[]
}

/** Atributo usado como foco ou mínimo no otimizador (mesmo enum dos times PvE). */
export type OptimizerStat = 'Hp' | 'Attack' | 'Defense' | 'Speed' | 'CriticalRate' | 'CriticalDamage' | 'Resistance' | 'Accuracy'

/** POST /api/accounts/{wizardId}/monsters/{monsterId}/optimize */
export interface OptimizeRunesRequest {
  /** Sets obrigatórios; repetir pede mais de um. */
  sets: string[]
  /** Principais aceitos por slot (códigos do jogo). */
  mainStats: Record<string, number[]>
  focus: OptimizerStat[]
  minimums: Array<{ stat: OptimizerStat; min: number }>
  includeEquippedOnOthers: boolean
  /** Runas em +15: principal no máximo e subs pelo ganho médio esperado. */
  assumeMaxLevel: boolean
  /** Troca os artefatos pelos compatíveis cujo principal mais ajuda o foco. */
  optimizeArtifacts: boolean
}

export interface OptimizedBuild {
  score: number
  total: StatValues
  activeSets: string[]
  runes: RuneListItem[]
  artifacts: ArtifactListItem[]
}

export interface OptimizeRunesResponse {
  current: OptimizedBuild
  builds: OptimizedBuild[]
}

export interface MonsterSkillDetail {
  skillId: number
  /** Nulo quando a habilidade não está no catálogo. */
  name: string | null
  slot: number
  level: number
  maxLevel: number
  hits: number
  passive: boolean
  aoe: boolean
  formulaText: string | null
  /** Fórmula bruta do SWARFARM (JSON em texto); nula sem dano. */
  formula: string | null
  /** Bônus de dano (%) dos skill-ups até o nível atual. */
  damageBonus: number
}

/** GET /api/accounts/{wizardId}/monsters/{monsterId}/skills */
export interface MonsterSkills {
  monsterId: number
  masterId: number
  name: string | null
  element: ElementName
  level: number
  total: StatValues
  skills: MonsterSkillDetail[]
}
