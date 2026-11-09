import type { AccountImport } from './types'

/**
 * Filtros aplicados no navegador antes do envio. Só monstros sem runas nem artefatos podem sair: remover um
 * monstro equipado deixaria o equipamento apontando para um monstro que não existe, e o import seria recusado.
 */
export interface ImportFilters {
  /** Ignora monstros sem equipamento (materiais, fodder...). */
  skipUnequipped: boolean
  /** Com o filtro acima, ignora só os sem equipamento abaixo destas estrelas; nulo = todos os sem equipamento. */
  belowStars: number | null
}

export const DEFAULT_IMPORT_FILTERS: ImportFilters = { skipUnequipped: false, belowStars: null }

export function applyImportFilters(data: AccountImport, filters: ImportFilters): AccountImport {
  if (!filters.skipUnequipped) return data

  const equipped = new Set<number>()
  for (const item of [...data.runes, ...data.artifacts]) {
    if (item.equippedMonsterId !== null) equipped.add(item.equippedMonsterId)
  }

  const monsters = data.monsters.filter(
    (monster) => equipped.has(monster.id) || (filters.belowStars !== null && monster.stars >= filters.belowStars),
  )
  return monsters.length === data.monsters.length ? data : { ...data, monsters }
}
