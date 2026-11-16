import { useQuery } from '@tanstack/react-query'
import type { MonsterDefinition } from '@/features/game-data/useMonsterDefinitions'
import { getJson } from '@/lib/api'

export interface BestiaryStats {
  hp: number
  attack: number
  defense: number
  speed: number
  resistance: number
  accuracy: number
  criticalRate: number
  criticalDamage: number
}

export interface LeaderSkill {
  /** Atributo como no SWARFARM (ex.: "Attack Power"). */
  attribute: string
  amount: number
  /** General, Element, Arena, Guild, Dungeon... */
  area: string
  element: string | null
}

export interface BestiarySkill {
  id: number
  name: string
  slot: number
  /** Descrição em pt-BR, ou em inglês enquanto não traduzida. */
  description: string | null
  /** Texto original em inglês quando a descrição é tradução. */
  originalDescription: string | null
  /** Recarga em turnos; nula sem recarga. */
  cooldown: number | null
  hits: number
  passive: boolean
  aoe: boolean
  maxLevel: number
  formulaText: string | null
  levelProgress: string[]
  iconUrl: string | null
}

/** GET /api/game-data/bestiary/{com2usId} */
export interface BestiaryMonster {
  monster: MonsterDefinition
  /** Atributos base em 6★ nível 40, sem runas. */
  maxLevelStats: BestiaryStats | null
  leaderSkill: LeaderSkill | null
  awakenBonus: string | null
  sources: string[]
  skills: BestiarySkill[]
  /** Outros elementos e versões da mesma família. */
  family: MonsterDefinition[]
}

/** Todos os monstros obteníveis. Dado estático: fica em cache durante a sessão. */
export function useBestiary() {
  return useQuery({
    queryKey: ['game-data', 'bestiary'],
    queryFn: ({ signal }) => getJson<MonsterDefinition[]>('/game-data/bestiary', signal),
    staleTime: Infinity,
  })
}

export function useBestiaryMonster(com2usId: number) {
  return useQuery({
    queryKey: ['game-data', 'bestiary', com2usId],
    queryFn: ({ signal }) => getJson<BestiaryMonster>(`/game-data/bestiary/${com2usId}`, signal),
    enabled: Number.isSafeInteger(com2usId) && com2usId > 0,
    staleTime: Infinity,
  })
}
