import type { MonsterDefinition } from '@/features/game-data/useMonsterDefinitions'
import { normalizeText } from '@/features/accounts/shared/sorting'

type Element = MonsterDefinition['element']
type Archetype = MonsterDefinition['archetype']

export interface BestiaryFilters {
  search: string
  element: Element | null
  stars: number | null
  archetype: Archetype | null
  /** 0 = não desperto, 1 = desperto, 2 = 2º despertar. */
  awaken: number | null
}

export const DEFAULT_BESTIARY_FILTERS: BestiaryFilters = { search: '', element: null, stars: null, archetype: null, awaken: null }

const ELEMENTS: Element[] = ['Water', 'Fire', 'Wind', 'Light', 'Dark', 'Pure']
const ARCHETYPES: Archetype[] = ['Attack', 'Defense', 'Hp', 'Support', 'Material', 'None']

/** Filtros na URL (?q=&elemento=&estrelas=&tipo=&despertar=), para voltar do detalhe com a mesma lista. */
export function bestiaryFiltersFromParams(params: URLSearchParams): BestiaryFilters {
  const element = params.get('elemento') as Element | null
  const archetype = params.get('tipo') as Archetype | null
  const stars = Number(params.get('estrelas'))
  const awaken = params.get('despertar')

  return {
    search: params.get('q') ?? '',
    element: element && ELEMENTS.includes(element) ? element : null,
    stars: stars >= 1 && stars <= 6 ? stars : null,
    archetype: archetype && ARCHETYPES.includes(archetype) ? archetype : null,
    awaken: awaken !== null && ['0', '1', '2'].includes(awaken) ? Number(awaken) : null,
  }
}

export function bestiaryFiltersToParams(filters: BestiaryFilters): URLSearchParams {
  const params = new URLSearchParams()
  if (filters.search) params.set('q', filters.search)
  if (filters.element) params.set('elemento', filters.element)
  if (filters.stars) params.set('estrelas', String(filters.stars))
  if (filters.archetype) params.set('tipo', filters.archetype)
  if (filters.awaken !== null) params.set('despertar', String(filters.awaken))
  return params
}

/** Filtra mantendo a ordem da API (nome, despertar, elemento). Busca ignora acentos e maiúsculas. */
export function filterBestiary(monsters: MonsterDefinition[], filters: BestiaryFilters): MonsterDefinition[] {
  const search = normalizeText(filters.search.trim())
  return monsters.filter(
    (monster) =>
      (filters.element === null || monster.element === filters.element) &&
      (filters.stars === null || monster.naturalStars === filters.stars) &&
      (filters.archetype === null || monster.archetype === filters.archetype) &&
      (filters.awaken === null || monster.awakenLevel === filters.awaken) &&
      (search === '' || normalizeText(monster.name).includes(search)),
  )
}
