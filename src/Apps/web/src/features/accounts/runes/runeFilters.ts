import { RUNE_STATS } from '@/lib/game/labels'
import type { SortDirection } from '../shared/sorting'
import type { RuneListItem } from '../types'
import { runeSpeed } from './efficiency'

/** Quantas linhas a tabela mostra por vez; contas grandes têm milhares de runas. */
export const RUNE_PAGE_SIZE = 100

/** Atributos de runa como opções de filtro (HP e HP% são atributos diferentes). */
export const RUNE_STAT_OPTIONS = Object.entries(RUNE_STATS).map(([type, stat]) => ({
  value: Number(type),
  label: `${stat.label}${stat.percent ? '%' : ''}`,
}))

export type RuneSortKey = 'efficiency' | 'maxEfficiency' | 'level' | 'speed'

export type RuneLocation = 'equipped' | 'inventory'

export interface RuneFilters {
  set: string | null
  slot: number | null
  /** Código do atributo principal. */
  mainStat: number | null
  /** Código de um atributo que a runa tenha nas subs ou na inata. */
  subStat: number | null
  location: RuneLocation | null
  sort: RuneSortKey
  direction: SortDirection
}

export const DEFAULT_RUNE_FILTERS: RuneFilters = {
  set: null,
  slot: null,
  mainStat: null,
  subStat: null,
  location: null,
  sort: 'efficiency',
  direction: 'desc',
}

const SORT_KEYS: RuneSortKey[] = ['efficiency', 'maxEfficiency', 'level', 'speed']

const LOCATION_PARAMS: Record<RuneLocation, string> = { equipped: 'equipada', inventory: 'inventario' }

/** Filtros na URL (?set=&slot=&principal=&sub=&local=equipada|inventario&ordem=&dir=), como na lista de monstros. */
export function runeFiltersFromParams(params: URLSearchParams): RuneFilters {
  const slot = Number(params.get('slot'))
  const sort = params.get('ordem') as RuneSortKey | null
  const location = (Object.keys(LOCATION_PARAMS) as RuneLocation[]).find((key) => LOCATION_PARAMS[key] === params.get('local'))

  return {
    set: params.get('set') || null,
    slot: Number.isInteger(slot) && slot >= 1 && slot <= 6 ? slot : null,
    mainStat: statParam(params.get('principal')),
    subStat: statParam(params.get('sub')),
    location: location ?? null,
    sort: sort && SORT_KEYS.includes(sort) ? sort : DEFAULT_RUNE_FILTERS.sort,
    direction: params.get('dir') === 'asc' ? 'asc' : 'desc',
  }
}

export function runeFiltersToParams(filters: RuneFilters): URLSearchParams {
  const params = new URLSearchParams()
  if (filters.set) params.set('set', filters.set)
  if (filters.slot) params.set('slot', String(filters.slot))
  if (filters.mainStat) params.set('principal', String(filters.mainStat))
  if (filters.subStat) params.set('sub', String(filters.subStat))
  if (filters.location) params.set('local', LOCATION_PARAMS[filters.location])
  if (filters.sort !== DEFAULT_RUNE_FILTERS.sort) params.set('ordem', filters.sort)
  if (filters.direction !== DEFAULT_RUNE_FILTERS.direction) params.set('dir', filters.direction)
  return params
}

export function filterAndSortRunes(runes: RuneListItem[], filters: RuneFilters): RuneListItem[] {
  const filtered = runes.filter(({ rune, equippedOn }) => {
    if (filters.set !== null && rune.set !== filters.set) return false
    if (filters.slot !== null && rune.slot !== filters.slot) return false
    if (filters.mainStat !== null && rune.mainStat.type !== filters.mainStat) return false
    if (
      filters.subStat !== null &&
      rune.innateStat?.type !== filters.subStat &&
      !rune.substats.some((sub) => sub.type === filters.subStat)
    ) {
      return false
    }
    if (filters.location === 'equipped' && equippedOn === null) return false
    if (filters.location === 'inventory' && equippedOn !== null) return false
    return true
  })

  const factor = filters.direction === 'asc' ? 1 : -1
  return filtered.sort((a, b) => {
    const diff = sortValue(a, filters.sort) - sortValue(b, filters.sort)
    // Desempate: eficiência e depois id, para a ordem não pular entre renderizações
    return diff !== 0 ? diff * factor : b.rune.efficiency - a.rune.efficiency || a.rune.runeId - b.rune.runeId
  })
}

function sortValue({ rune }: RuneListItem, key: RuneSortKey): number {
  switch (key) {
    case 'efficiency':
      return rune.efficiency
    case 'maxEfficiency':
      return rune.maxEfficiency
    case 'level':
      return rune.level
    case 'speed':
      return runeSpeed(rune)
  }
}

function statParam(value: string | null): number | null {
  const stat = Number(value)
  return value && RUNE_STATS[stat] ? stat : null
}
