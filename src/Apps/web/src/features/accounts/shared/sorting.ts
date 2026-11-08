export type SortDirection = 'asc' | 'desc'

/** Mesma coluna inverte a direção; coluna nova começa pela direção indicada. */
export function nextSort<K extends string>(
  current: { sort: K; direction: SortDirection },
  sort: K,
  initialDirection: SortDirection,
): { sort: K; direction: SortDirection } {
  if (current.sort === sort) {
    return { sort, direction: current.direction === 'asc' ? 'desc' : 'asc' }
  }
  return { sort, direction: initialDirection }
}

/** Busca sem diferenciar maiúsculas nem acentos. */
export function normalizeText(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
}
