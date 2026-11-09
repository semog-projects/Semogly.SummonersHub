import type { ElementName } from '@/lib/game/labels'
import { normalizeText } from '../shared/sorting'
import type { BuildKindName, MonsterListItem, StatValues } from '../types'

export type SortKey = 'name' | 'stars' | 'level' | 'averageRuneEfficiency' | keyof StatValues

export interface MonsterFilters {
  search: string
  stars: number | null
  element: ElementName | null
  set: string | null
  /** Rainbowmons, devilmons e afins ficam ocultos por padrão. */
  includeMaterials: boolean
  /** Só monstros com build própria nesse modo (RTA, Siege, Arena). */
  buildKind: BuildKindName | null
  sort: SortKey
  direction: 'asc' | 'desc'
}

export const DEFAULT_FILTERS: MonsterFilters = {
  search: '',
  stars: null,
  element: null,
  set: null,
  includeMaterials: false,
  buildKind: null,
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
  'effectiveHp',
  'averageRuneEfficiency',
]

const ELEMENTS: ElementName[] = ['Water', 'Fire', 'Wind', 'Light', 'Dark', 'Pure']

const BUILD_KINDS: BuildKindName[] = ['Rta', 'Siege', 'Arena']

/**
 * Filtros guardados na URL (?q=&estrelas=&elemento=&set=&materiais=1&ordem=speed&dir=desc), para que voltar
 * do detalhe do monstro ou compartilhar o link mantenha a mesma lista.
 */
export function filtersFromParams(params: URLSearchParams): MonsterFilters {
  const stars = Number(params.get('estrelas'))
  const element = params.get('elemento') as ElementName | null
  const sort = params.get('ordem') as SortKey | null
  const buildKind = params.get('modo') as BuildKindName | null

  return {
    search: params.get('q') ?? '',
    stars: stars >= 1 && stars <= 6 ? stars : null,
    element: element && ELEMENTS.includes(element) ? element : null,
    set: params.get('set') || null,
    includeMaterials: params.get('materiais') === '1',
    buildKind: buildKind && BUILD_KINDS.includes(buildKind) ? buildKind : null,
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
  if (filters.buildKind) params.set('modo', filters.buildKind)
  if (filters.sort !== DEFAULT_FILTERS.sort) params.set('ordem', filters.sort)
  if (filters.direction !== DEFAULT_FILTERS.direction) params.set('dir', filters.direction)
  return params
}

export function filterAndSortMonsters(monsters: MonsterListItem[], filters: MonsterFilters): MonsterListItem[] {
  const search = normalizeText(filters.search.trim())

  const filtered = monsters.filter(
    (monster) =>
      (filters.includeMaterials || monster.archetype !== 'Material') &&
      (filters.stars === null || monster.stars === filters.stars) &&
      (filters.element === null || monster.element === filters.element) &&
      (filters.set === null || monster.activeSets.includes(filters.set)) &&
      (filters.buildKind === null || (monster.buildKinds ?? []).includes(filters.buildKind)) &&
      (search === '' || normalizeText(monster.name ?? '').includes(search)),
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
    case 'averageRuneEfficiency':
      // Sem runas fica abaixo de qualquer eficiência
      return (a.averageRuneEfficiency ?? -1) - (b.averageRuneEfficiency ?? -1)
    default:
      return a.total[key] - b.total[key]
  }
}
