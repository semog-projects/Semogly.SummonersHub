import type { ArtifactDetail, ArtifactListItem } from '../types'
import {
  artifactFiltersFromParams,
  artifactFiltersToParams,
  artifactKindLabel,
  DEFAULT_ARTIFACT_FILTERS,
  filterAndSortArtifacts,
  type ArtifactFilters,
} from './artifactFilters'

function item(overrides: Partial<ArtifactDetail> & { artifactId: number }, equipped = true): ArtifactListItem {
  return {
    artifact: {
      type: 'Element',
      element: 'Water',
      archetype: null,
      level: 15,
      rarity: 5,
      originalRarity: 5,
      mainStat: { type: 101, value: 100 },
      substats: [],
      ...overrides,
    },
    equippedOn: equipped ? { monsterId: 1, masterId: 19811, name: 'Lapis', element: 'Water' } : null,
  }
}

const SPEED_DAMAGE = 221

const artifacts = [
  item({ artifactId: 1, originalRarity: 4, substats: [{ type: SPEED_DAMAGE, value: 25, upgrades: 1 }] }),
  item({ artifactId: 2, type: 'Archetype', element: null, archetype: 'Support', mainStat: { type: 100, value: 1500 } }, false),
  item({ artifactId: 3, element: 'Fire', substats: [{ type: SPEED_DAMAGE, value: 31, upgrades: 2 }] }, false),
  item({ artifactId: 4, level: 12, originalRarity: 3 }),
]

const ids = (filters: Partial<ArtifactFilters>) =>
  filterAndSortArtifacts([...artifacts], { ...DEFAULT_ARTIFACT_FILTERS, ...filters }).map((a) => a.artifact.artifactId)

describe('filterAndSortArtifacts', () => {
  it('ordena pela raridade original e desempata pelo nível', () => {
    expect(ids({})).toEqual([2, 3, 1, 4])
  })

  it('filtra por tipo, elemento, arquétipo e principal', () => {
    expect(ids({ type: 'Archetype' })).toEqual([2])
    expect(ids({ element: 'Fire' })).toEqual([3])
    expect(ids({ archetype: 'Support' })).toEqual([2])
    expect(ids({ mainStat: 101 })).toEqual([3, 1, 4])
  })

  it('filtra pela sub e ordena pelo valor dela', () => {
    expect(ids({ subEffect: SPEED_DAMAGE, sort: 'subValue' })).toEqual([3, 1])
    expect(ids({ subEffect: SPEED_DAMAGE, sort: 'subValue', direction: 'asc' })).toEqual([1, 3])
  })

  it('filtra por equipado ou inventário', () => {
    expect(ids({ location: 'equipped' })).toEqual([1, 4])
    expect(ids({ location: 'inventory' })).toEqual([2, 3])
  })
})

describe('artifactKindLabel', () => {
  it('descreve elemento e arquétipo em pt-BR', () => {
    expect(artifactKindLabel(artifacts[0].artifact)).toBe('Elemento Água')
    expect(artifactKindLabel(artifacts[1].artifact)).toBe('Arquétipo Suporte')
  })
})

describe('parâmetros da URL', () => {
  it('ida e volta preservam os filtros', () => {
    const filters: ArtifactFilters = {
      type: 'Element',
      element: 'Dark',
      archetype: null,
      mainStat: 102,
      subEffect: SPEED_DAMAGE,
      location: 'equipped',
      sort: 'subValue',
      direction: 'asc',
    }
    expect(artifactFiltersFromParams(artifactFiltersToParams(filters))).toEqual(filters)
  })

  it('ignora valores inválidos', () => {
    const params = new URLSearchParams('tipo=x&elemento=Pure&arquetipo=Material&principal=4&sub=999&ordem=foo')
    expect(artifactFiltersFromParams(params)).toEqual(DEFAULT_ARTIFACT_FILTERS)
  })
})
