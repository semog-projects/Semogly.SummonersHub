/** Limite de caracteres da tag (o mesmo da API). */
export const MAX_TAG_LENGTH = 24

/** Filtros de bloqueio e tag, comuns às listas de runas e artefatos. */
export type LockFilter = 'locked' | 'unlocked'

export interface MarkFilters {
  lock: LockFilter | null
  tag: string | null
}

const LOCK_PARAMS: Record<LockFilter, string> = { locked: 'sim', unlocked: 'nao' }

/** ?bloqueio=sim|nao&tag=... */
export function markFiltersFromParams(params: URLSearchParams): MarkFilters {
  const lock = (Object.keys(LOCK_PARAMS) as LockFilter[]).find((key) => LOCK_PARAMS[key] === params.get('bloqueio'))
  return { lock: lock ?? null, tag: params.get('tag') || null }
}

export function markFiltersToParams(filters: MarkFilters, params: URLSearchParams) {
  if (filters.lock) params.set('bloqueio', LOCK_PARAMS[filters.lock])
  if (filters.tag) params.set('tag', filters.tag)
}

export function matchesMarks(item: { locked: boolean; tags: string[] }, filters: MarkFilters): boolean {
  if (filters.lock === 'locked' && !item.locked) return false
  if (filters.lock === 'unlocked' && item.locked) return false
  if (filters.tag !== null && !item.tags.some((tag) => tag.toLowerCase() === filters.tag!.toLowerCase())) return false
  return true
}

/** Tags usadas nos itens, sem repetição e em ordem alfabética, para o filtro. */
export function availableTags(items: Array<{ tags: string[] }>): string[] {
  const byKey = new Map<string, string>()
  for (const item of items) {
    for (const tag of item.tags) {
      if (!byKey.has(tag.toLowerCase())) byKey.set(tag.toLowerCase(), tag)
    }
  }
  return [...byKey.values()].sort((a, b) => a.localeCompare(b, 'pt-BR'))
}
