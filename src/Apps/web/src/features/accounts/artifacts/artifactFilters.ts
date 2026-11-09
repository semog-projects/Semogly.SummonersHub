import {
  ARCHETYPE_NAME_LABELS,
  ARTIFACT_EFFECT_LABELS,
  ARTIFACT_MAIN_STAT_LABELS,
  ELEMENT_NAME_LABELS,
  type ArchetypeName,
  type ElementName,
} from '@/lib/game/labels'
import { markFiltersFromParams, markFiltersToParams, matchesMarks, type MarkFilters } from '../marks/markFilters'
import type { SortDirection } from '../shared/sorting'
import type { ArtifactDetail, ArtifactListItem } from '../types'

export type ArtifactKind = 'Element' | 'Archetype'

export type ArtifactSortKey = 'rarity' | 'level' | 'subValue'

export type ArtifactLocation = 'equipped' | 'inventory'

export interface ArtifactFilters extends MarkFilters {
  type: ArtifactKind | null
  element: ElementName | null
  archetype: ArchetypeName | null
  /** Código do atributo principal (100 HP, 101 ATK, 102 DEF). */
  mainStat: number | null
  /** Código de uma sub; com ela, a ordenação por "valor da sub" usa o valor dessa sub. */
  subEffect: number | null
  location: ArtifactLocation | null
  sort: ArtifactSortKey
  direction: SortDirection
}

export const DEFAULT_ARTIFACT_FILTERS: ArtifactFilters = {
  type: null,
  element: null,
  archetype: null,
  mainStat: null,
  subEffect: null,
  location: null,
  lock: null,
  tag: null,
  sort: 'rarity',
  direction: 'desc',
}

export const ARTIFACT_PAGE_SIZE = 100

/** Elementos de artefato (não há artefato de elemento puro). */
export const ARTIFACT_ELEMENTS: ElementName[] = (Object.keys(ELEMENT_NAME_LABELS) as ElementName[]).filter((e) => e !== 'Pure')

/** Arquétipos de artefato (não há de material). */
export const ARTIFACT_ARCHETYPES: ArchetypeName[] = ['Attack', 'Defense', 'Hp', 'Support']

/** Subs como opção de filtro: o texto do efeito com "X" no lugar do valor. */
export const ARTIFACT_EFFECT_OPTIONS = Object.entries(ARTIFACT_EFFECT_LABELS).map(([type, template]) => ({
  value: Number(type),
  label: template.replace('{v}', 'X'),
}))

const SORT_KEYS: ArtifactSortKey[] = ['rarity', 'level', 'subValue']
const TYPE_PARAMS: Record<ArtifactKind, string> = { Element: 'elemento', Archetype: 'arquetipo' }
const LOCATION_PARAMS: Record<ArtifactLocation, string> = { equipped: 'equipado', inventory: 'inventario' }

/** Filtros na URL (?tipo=&elemento=&arquetipo=&principal=&sub=&local=&ordem=&dir=). */
export function artifactFiltersFromParams(params: URLSearchParams): ArtifactFilters {
  const element = params.get('elemento') as ElementName | null
  const archetype = params.get('arquetipo') as ArchetypeName | null
  const mainStat = Number(params.get('principal'))
  const subEffect = Number(params.get('sub'))
  const sort = params.get('ordem') as ArtifactSortKey | null

  return {
    type: keyOf(TYPE_PARAMS, params.get('tipo')),
    element: element && ARTIFACT_ELEMENTS.includes(element) ? element : null,
    archetype: archetype && ARTIFACT_ARCHETYPES.includes(archetype) ? archetype : null,
    mainStat: ARTIFACT_MAIN_STAT_LABELS[mainStat] ? mainStat : null,
    subEffect: ARTIFACT_EFFECT_LABELS[subEffect] ? subEffect : null,
    location: keyOf(LOCATION_PARAMS, params.get('local')),
    ...markFiltersFromParams(params),
    sort: sort && SORT_KEYS.includes(sort) ? sort : DEFAULT_ARTIFACT_FILTERS.sort,
    direction: params.get('dir') === 'asc' ? 'asc' : 'desc',
  }
}

export function artifactFiltersToParams(filters: ArtifactFilters): URLSearchParams {
  const params = new URLSearchParams()
  if (filters.type) params.set('tipo', TYPE_PARAMS[filters.type])
  if (filters.element) params.set('elemento', filters.element)
  if (filters.archetype) params.set('arquetipo', filters.archetype)
  if (filters.mainStat) params.set('principal', String(filters.mainStat))
  if (filters.subEffect) params.set('sub', String(filters.subEffect))
  if (filters.location) params.set('local', LOCATION_PARAMS[filters.location])
  markFiltersToParams(filters, params)
  if (filters.sort !== DEFAULT_ARTIFACT_FILTERS.sort) params.set('ordem', filters.sort)
  if (filters.direction !== DEFAULT_ARTIFACT_FILTERS.direction) params.set('dir', filters.direction)
  return params
}

/** "Elemento Água", "Arquétipo Ataque". */
export function artifactKindLabel(artifact: ArtifactDetail): string {
  return artifact.type === 'Element'
    ? `Elemento ${artifact.element ? ELEMENT_NAME_LABELS[artifact.element] : ''}`.trim()
    : `Arquétipo ${artifact.archetype ? ARCHETYPE_NAME_LABELS[artifact.archetype] : ''}`.trim()
}

/** Valor da sub no artefato, ou 0 se ele não a tem. */
export function subValue({ artifact }: ArtifactListItem, effect: number | null): number {
  return artifact.substats.find((sub) => sub.type === effect)?.value ?? 0
}

export function filterAndSortArtifacts(items: ArtifactListItem[], filters: ArtifactFilters): ArtifactListItem[] {
  const filtered = items.filter((item) => {
    const { artifact, equippedOn } = item
    if (!matchesMarks(item, filters)) return false
    if (filters.type !== null && artifact.type !== filters.type) return false
    if (filters.element !== null && artifact.element !== filters.element) return false
    if (filters.archetype !== null && artifact.archetype !== filters.archetype) return false
    if (filters.mainStat !== null && artifact.mainStat.type !== filters.mainStat) return false
    if (filters.subEffect !== null && !artifact.substats.some((sub) => sub.type === filters.subEffect)) return false
    if (filters.location === 'equipped' && equippedOn === null) return false
    if (filters.location === 'inventory' && equippedOn !== null) return false
    return true
  })

  const factor = filters.direction === 'asc' ? 1 : -1
  return filtered.sort((a, b) => {
    const diff = sortValue(a, filters) - sortValue(b, filters)
    // Desempate: raridade, nível e id, para a ordem não pular entre renderizações
    return diff !== 0
      ? diff * factor
      : b.artifact.originalRarity - a.artifact.originalRarity ||
          b.artifact.level - a.artifact.level ||
          a.artifact.artifactId - b.artifact.artifactId
  })
}

function sortValue(item: ArtifactListItem, filters: ArtifactFilters): number {
  switch (filters.sort) {
    case 'rarity':
      return item.artifact.originalRarity
    case 'level':
      return item.artifact.level
    case 'subValue':
      return subValue(item, filters.subEffect)
  }
}

function keyOf<K extends string>(map: Record<K, string>, value: string | null): K | null {
  return (Object.keys(map) as K[]).find((key) => map[key] === value) ?? null
}

/** Rótulos dos filtros de elemento e arquétipo. */
export const ARTIFACT_ELEMENT_OPTIONS = ARTIFACT_ELEMENTS.map((e) => ({ value: e, label: ELEMENT_NAME_LABELS[e] }))
export const ARTIFACT_ARCHETYPE_OPTIONS = ARTIFACT_ARCHETYPES.map((a) => ({ value: a, label: ARCHETYPE_NAME_LABELS[a] }))
