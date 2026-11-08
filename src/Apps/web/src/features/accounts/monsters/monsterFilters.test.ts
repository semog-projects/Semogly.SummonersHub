import type { MonsterListItem } from '../types'
import { DEFAULT_FILTERS, filterAndSortMonsters, filtersFromParams, filtersToParams } from './monsterFilters'

function monster(overrides: Partial<MonsterListItem> & { monsterId: number }): MonsterListItem {
  return {
    masterId: 1,
    name: 'Monstro',
    element: 'Water',
    archetype: 'Attack',
    stars: 6,
    level: 40,
    awakenLevel: 1,
    total: { hp: 10000, attack: 1000, defense: 500, speed: 100, criticalRate: 15, criticalDamage: 50, resistance: 15, accuracy: 0, effectiveHp: 30000 },
    activeSets: [],
    runes: 6,
    artifacts: 2,
    averageRuneEfficiency: null,
    ...overrides,
  }
}

const withSpeed = (speed: number) => ({ ...monster({ monsterId: 0 }).total, speed })

const monsters = [
  monster({ monsterId: 1, name: 'Lapis', element: 'Water', total: withSpeed(151), activeSets: ['Vampire', 'Revenge'] }),
  monster({ monsterId: 2, name: 'Konamiya', element: 'Dark', total: withSpeed(214), activeSets: ['Swift', 'Will'] }),
  monster({ monsterId: 3, name: 'Tyron', element: 'Water', total: withSpeed(195), activeSets: ['Violent', 'Focus'] }),
  monster({ monsterId: 4, name: 'Rainbowmon', archetype: 'Material', stars: 5, total: withSpeed(300) }),
  monster({ monsterId: 5, name: 'Gânimedes', element: 'Wind', stars: 5, total: withSpeed(195), activeSets: ['Swift'] }),
]

const names = (list: MonsterListItem[]) => list.map((m) => m.name)

describe('filterAndSortMonsters', () => {
  it('ordena por velocidade e esconde materiais por padrão', () => {
    expect(names(filterAndSortMonsters([...monsters], DEFAULT_FILTERS))).toEqual([
      'Konamiya',
      'Gânimedes', // empate em 195: desempata pelo nome
      'Tyron',
      'Lapis',
    ])
  })

  it('mostra materiais quando pedido', () => {
    const result = filterAndSortMonsters([...monsters], { ...DEFAULT_FILTERS, includeMaterials: true })
    expect(result[0].name).toBe('Rainbowmon')
  })

  it('filtra por estrelas, elemento e set', () => {
    expect(names(filterAndSortMonsters([...monsters], { ...DEFAULT_FILTERS, element: 'Water' }))).toEqual(['Tyron', 'Lapis'])
    expect(names(filterAndSortMonsters([...monsters], { ...DEFAULT_FILTERS, stars: 5 }))).toEqual(['Gânimedes'])
    expect(names(filterAndSortMonsters([...monsters], { ...DEFAULT_FILTERS, set: 'Swift' }))).toEqual(['Konamiya', 'Gânimedes'])
  })

  it('busca pelo nome sem diferenciar maiúsculas nem acentos', () => {
    expect(names(filterAndSortMonsters([...monsters], { ...DEFAULT_FILTERS, search: 'GANI' }))).toEqual(['Gânimedes'])
  })

  it('ordena pela eficiência média das runas, com quem não tem runas por último', () => {
    const withEfficiency = [
      monster({ monsterId: 1, averageRuneEfficiency: 70 }),
      monster({ monsterId: 2, averageRuneEfficiency: null }),
      monster({ monsterId: 3, averageRuneEfficiency: 95 }),
    ]
    const sorted = filterAndSortMonsters(withEfficiency, { ...DEFAULT_FILTERS, sort: 'averageRuneEfficiency' })
    expect(sorted.map((m) => m.monsterId)).toEqual([3, 1, 2])
  })

  it('ordena por nome em ordem crescente', () => {
    const result = filterAndSortMonsters([...monsters], { ...DEFAULT_FILTERS, sort: 'name', direction: 'asc' })
    expect(names(result)).toEqual(['Gânimedes', 'Konamiya', 'Lapis', 'Tyron'])
  })
})

describe('filtros na URL', () => {
  it('ida e volta preservam os filtros', () => {
    const filters = { ...DEFAULT_FILTERS, search: 'lap', stars: 6, element: 'Water' as const, set: 'Violent', includeMaterials: true, sort: 'hp' as const, direction: 'asc' as const }

    expect(filtersFromParams(filtersToParams(filters))).toEqual(filters)
  })

  it('valores padrão não aparecem na URL', () => {
    expect(filtersToParams(DEFAULT_FILTERS).toString()).toBe('')
  })

  it('ignora valores inválidos', () => {
    const filters = filtersFromParams(new URLSearchParams('estrelas=9&elemento=Agua&ordem=foo&dir=x'))

    expect(filters).toEqual(DEFAULT_FILTERS)
  })
})
