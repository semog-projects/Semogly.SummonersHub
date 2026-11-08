import type { ElementName } from '@/lib/game/labels'
import type { MonsterListItem, StatValues } from '../types'

export type SortKey = 'name' | 'stars' | 'level' | keyof StatValues

export interface MonsterFilters {
  search: string
  stars: number | null
  element: ElementName | null
  set: string | null
  /** Rainbowmons, devilmons e afins ficam ocultos por padrão. */
  includeMaterials: boolean
  sort: SortKey
  direction: 'asc' | 'desc'
}

export const DEFAULT_FILTERS: MonsterFilters = {
  search: '',
  stars: null,
  element: null,
  set: null,
  includeMaterials: false,
  sort: 'speed',
  direction: 'desc',
}

const SORT_KEYS: SortKey[] = [
  'name',
  'stars',
  'level',
  'hp',
  'attack',
  'defense',
  'speed',
  'criticalRate',
  'criticalDamage',
  'resistance',
  'accuracy',
]

const ELEMENTS: ElementName[] = ['Water', 'Fire', 'Wind', 'Light', 'Dark', 'Pure']

/**
 * Filtros guardados na URL (?q=&estrelas=&elemento=&set=&materiais=1&ordem=speed&dir=desc), para que voltar
 * do detalhe do monstro ou compartilhar o link mantenha a mesma lista.
 */
export function filtersFromParams(params: URLSearchParams): MonsterFilters {
  const stars = Number(params.get('estrelas'))
  const element = params.get('elemento') as ElementName | null
  const sort = params.get('ordem') as SortKey | null

  return {
    search: params.get('q') ?? '',
    stars: stars >= 1 && stars <= 6 ? stars : null,
    element: element && ELEMENTS.includes(element) ? element : null,
    set: params.get('set') || null,
    includeMaterials: params.get('materiais') === '1',
    sort: sort && SORT_KEYS.includes(sort) ? sort : DEFAULT_FILTERS.sort,
    direction: params.get('dir') === 'asc' ? 'asc' : params.get('dir') === 'desc' ? 'desc' : DEFAULT_FILTERS.direction,
  }
}

export function filtersToParams(filters: MonsterFilters): URLSearchParams {
  const params = new URLSearchParams()
  if (filters.search) params.set('q', filters.search)
  if (filters.stars) params.set('estrelas', String(filters.stars))
  if (filters.element) params.set('elemento', filters.element)
  if (filters.set) params.set('set', filters.set)
  if (filters.includeMaterials) params.set('materiais', '1')
  if (filters.sort !== DEFAULT_FILTERS.sort) params.set('ordem', filters.sort)
  if (filters.direction !== DEFAULT_FILTERS.direction) params.set('dir', filters.direction)
  return params
}

export function filterAndSortMonsters(monsters: MonsterListItem[], filters: MonsterFilters): MonsterListItem[] {
  const search = normalize(filters.search.trim())

  const filtered = monsters.filter(
    (monster) =>
      (filters.includeMaterials || monster.archetype !== 'Material') &&
      (filters.stars === null || monster.stars === filters.stars) &&
      (filters.element === null || monster.element === filters.element) &&
      (filters.set === null || monster.activeSets.includes(filters.set)) &&
      (search === '' || normalize(monster.name ?? '').includes(search)),
  )

  const factor = filters.direction === 'asc' ? 1 : -1
  return filtered.sort((a, b) => {
    const diff = compare(a, b, filters.sort)
    // Desempate estável: nome e depois id
    return diff !== 0 ? diff * factor : compare(a, b, 'name') || a.monsterId - b.monsterId
  })
}

function compare(a: MonsterListItem, b: MonsterListItem, key: SortKey): number {
  switch (key) {
    case 'name':
      return (a.name ?? '').localeCompare(b.name ?? '', 'pt-BR')
    case 'stars':
      return a.stars - b.stars
    case 'level':
      return a.level - b.level
    default:
      return a.total[key] - b.total[key]
  }
}

/** Busca sem diferenciar maiúsculas nem acentos. */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
}
