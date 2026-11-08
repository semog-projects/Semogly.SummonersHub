import type { ElementName } from '@/lib/game/labels'

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
