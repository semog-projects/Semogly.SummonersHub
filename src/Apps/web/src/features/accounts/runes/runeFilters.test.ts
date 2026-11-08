import type { RuneDetail, RuneListItem } from '../types'
import {
  DEFAULT_RUNE_FILTERS,
  filterAndSortRunes,
  runeFiltersFromParams,
  runeFiltersToParams,
  type RuneFilters,
} from './runeFilters'

function item(overrides: Partial<RuneDetail> & { runeId: number }, equipped = true): RuneListItem {
  return {
    rune: {
      slot: 1,
      stars: 6,
      isAncient: false,
      set: 'Swift',
      level: 15,
      rarity: 5,
      originalRarity: 5,
      mainStat: { type: 3, value: 160 },
      innateStat: null,
      substats: [],
      efficiency: 80,
      maxEfficiency: 80,
      ...overrides,
    },
    equippedOn: equipped ? { monsterId: 1, masterId: 19811, name: 'Lapis', element: 'Water' } : null,
  }
}

const runes = [
  item({ runeId: 1, efficiency: 70, substats: [{ type: 8, value: 18, isGemmed: false, grindValue: 0 }] }),
  item({ runeId: 2, set: 'Violent', slot: 2, mainStat: { type: 8, value: 42 }, efficiency: 95, maxEfficiency: 95 }, false),
  item({ runeId: 3, level: 9, efficiency: 60, maxEfficiency: 88, innateStat: { type: 8, value: 5 } }, false),
  item({ runeId: 4, efficiency: 85, substats: [{ type: 8, value: 12, isGemmed: false, grindValue: 4 }] }),
]

const ids = (filters: Partial<RuneFilters>) =>
  filterAndSortRunes([...runes], { ...DEFAULT_RUNE_FILTERS, ...filters }).map((r) => r.rune.runeId)

describe('filterAndSortRunes', () => {
  it('ordena pela eficiência atual, da maior para a menor', () => {
    expect(ids({})).toEqual([2, 4, 1, 3])
  })

  it('ordena pela eficiência máxima', () => {
    expect(ids({ sort: 'maxEfficiency' })).toEqual([2, 3, 4, 1])
  })

  it('ordena pela velocidade de sub com grind ou inata', () => {
    expect(ids({ sort: 'speed' })).toEqual([1, 4, 3, 2])
  })

  it('filtra por set, slot e principal', () => {
    expect(ids({ set: 'Violent' })).toEqual([2])
    expect(ids({ slot: 1 })).toEqual([4, 1, 3])
    expect(ids({ mainStat: 8 })).toEqual([2])
  })

  it('filtra pela sub, considerando a inata', () => {
    expect(ids({ subStat: 8 })).toEqual([4, 1, 3])
  })

  it('filtra por equipada ou inventário', () => {
    expect(ids({ location: 'equipped' })).toEqual([4, 1])
    expect(ids({ location: 'inventory' })).toEqual([2, 3])
  })
})

describe('parâmetros da URL', () => {
  it('ida e volta preservam os filtros', () => {
    const filters: RuneFilters = {
      set: 'Swift',
      slot: 2,
      mainStat: 8,
      subStat: 9,
      location: 'inventory',
      sort: 'speed',
      direction: 'asc',
    }
    expect(runeFiltersFromParams(runeFiltersToParams(filters))).toEqual(filters)
  })

  it('ignora valores inválidos', () => {
    const filters = runeFiltersFromParams(new URLSearchParams('slot=9&principal=7&local=x&ordem=foo'))
    expect(filters).toEqual(DEFAULT_RUNE_FILTERS)
  })

  it('filtros padrão geram URL vazia', () => {
    expect(runeFiltersToParams(DEFAULT_RUNE_FILTERS).toString()).toBe('')
  })
})
