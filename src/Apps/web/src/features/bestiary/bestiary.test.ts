import type { MonsterDefinition } from '@/features/game-data/useMonsterDefinitions'
import {
  bestiaryFiltersFromParams,
  bestiaryFiltersToParams,
  DEFAULT_BESTIARY_FILTERS,
  filterBestiary,
} from './bestiaryFilters'
import { leaderSkillText } from './leaderSkill'

function monster(overrides: Partial<MonsterDefinition> & { com2usId: number; name: string }): MonsterDefinition {
  return {
    familyId: 1,
    element: 'Water',
    archetype: 'Attack',
    naturalStars: 3,
    awakenLevel: 1,
    isHomunculus: false,
    imageUrl: null,
    ...overrides,
  }
}

const monsters = [
  monster({ com2usId: 1, name: 'Lapis', element: 'Water', archetype: 'Attack', naturalStars: 3 }),
  monster({ com2usId: 2, name: 'Fairy', element: 'Water', archetype: 'Support', naturalStars: 3, awakenLevel: 0 }),
  monster({ com2usId: 3, name: 'Gânimedes', element: 'Wind', archetype: 'Defense', naturalStars: 4 }),
  monster({ com2usId: 4, name: 'Veromos', element: 'Dark', archetype: 'Hp', naturalStars: 4, awakenLevel: 2 }),
]

const names = (list: MonsterDefinition[]) => list.map((m) => m.name)

describe('filterBestiary', () => {
  it('sem filtros mantém a ordem da API', () => {
    expect(names(filterBestiary(monsters, DEFAULT_BESTIARY_FILTERS))).toEqual(['Lapis', 'Fairy', 'Gânimedes', 'Veromos'])
  })

  it('combina elemento, estrelas, tipo e despertar', () => {
    expect(names(filterBestiary(monsters, { ...DEFAULT_BESTIARY_FILTERS, element: 'Water' }))).toEqual(['Lapis', 'Fairy'])
    expect(names(filterBestiary(monsters, { ...DEFAULT_BESTIARY_FILTERS, stars: 4, archetype: 'Hp' }))).toEqual(['Veromos'])
    expect(names(filterBestiary(monsters, { ...DEFAULT_BESTIARY_FILTERS, awaken: 0 }))).toEqual(['Fairy'])
  })

  it('busca sem diferenciar acentos nem maiúsculas', () => {
    expect(names(filterBestiary(monsters, { ...DEFAULT_BESTIARY_FILTERS, search: 'GANI' }))).toEqual(['Gânimedes'])
  })

  it('filtros vão e voltam pela URL e ignoram valores inválidos', () => {
    const filters = { search: 'lap', element: 'Water' as const, stars: 3, archetype: 'Attack' as const, awaken: 0 }

    expect(bestiaryFiltersFromParams(bestiaryFiltersToParams(filters))).toEqual(filters)
    expect(bestiaryFiltersToParams(DEFAULT_BESTIARY_FILTERS).toString()).toBe('')
    expect(bestiaryFiltersFromParams(new URLSearchParams('elemento=Agua&estrelas=9&tipo=x&despertar=5'))).toEqual(DEFAULT_BESTIARY_FILTERS)
  })
})

describe('leaderSkillText', () => {
  it('descreve o líder em português', () => {
    expect(leaderSkillText({ attribute: 'Attack Power', amount: 25, area: 'General', element: null })).toBe(
      'Aumenta o ATK dos aliados em 25%',
    )
    expect(leaderSkillText({ attribute: 'Attack Speed', amount: 24, area: 'Element', element: 'Wind' })).toBe(
      'Aumenta a VEL dos aliados de vento em 24%',
    )
    expect(leaderSkillText({ attribute: 'HP', amount: 33, area: 'Arena', element: null })).toBe('Aumenta o HP dos aliados em 33% na Arena')
  })

  it('mostra atributos e áreas desconhecidos como vêm do bestiário', () => {
    expect(leaderSkillText({ attribute: 'Mana', amount: 10, area: 'Siege', element: null })).toBe('Aumenta Mana dos aliados em 10% (Siege)')
  })
})
