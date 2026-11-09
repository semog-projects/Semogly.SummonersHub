import { applyImportFilters, DEFAULT_IMPORT_FILTERS } from './importFilters'
import type { AccountImport, Monster } from './types'

const monster = (id: number, stars: number) => ({ id, stars }) as Monster

const data = {
  monsters: [monster(1, 6), monster(2, 5), monster(3, 2), monster(4, 4)],
  runes: [{ equippedMonsterId: 1 }, { equippedMonsterId: null }],
  artifacts: [{ equippedMonsterId: 3 }],
} as unknown as AccountImport

const ids = (result: AccountImport) => result.monsters.map((m) => m.id)

describe('applyImportFilters', () => {
  it('sem filtro, nada muda', () => {
    expect(applyImportFilters(data, DEFAULT_IMPORT_FILTERS)).toBe(data)
  })

  it('ignora todos os monstros sem runas nem artefatos', () => {
    expect(ids(applyImportFilters(data, { skipUnequipped: true, belowStars: null }))).toEqual([1, 3])
  })

  it('ignora só os sem equipamento abaixo das estrelas escolhidas; equipados ficam sempre', () => {
    // 3 é 2★, mas tem artefato; 4 (4★) e 2 (5★) não têm equipamento
    expect(ids(applyImportFilters(data, { skipUnequipped: true, belowStars: 5 }))).toEqual([1, 2, 3])
  })

  it('não mexe em runas, artefatos nem itens de craft', () => {
    const result = applyImportFilters(data, { skipUnequipped: true, belowStars: null })
    expect(result.runes).toBe(data.runes)
    expect(result.artifacts).toBe(data.artifacts)
  })
})
