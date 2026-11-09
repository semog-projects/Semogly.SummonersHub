import { SelectFilter } from '../shared/filterControls'
import type { LockFilter, MarkFilters } from './markFilters'

const LOCK_OPTIONS: Array<{ value: LockFilter; label: string }> = [
  { value: 'locked', label: 'Bloqueados' },
  { value: 'unlocked', label: 'Desbloqueados' },
]

/** Selects de bloqueio e tag para as barras de filtro de runas e artefatos. */
export function MarkFilterControls({
  filters,
  tags,
  onChange,
}: {
  filters: MarkFilters
  /** Tags existentes nos itens (para o select). */
  tags: string[]
  onChange: (patch: Partial<MarkFilters>) => void
}) {
  return (
    <>
      <SelectFilter label="Bloqueio" allLabel="Com e sem bloqueio" value={filters.lock} options={LOCK_OPTIONS} onChange={(lock) => onChange({ lock })} />
      {tags.length > 0 && (
        <SelectFilter
          label="Tag"
          allLabel="Qualquer tag"
          value={filters.tag}
          options={tags.map((tag) => ({ value: tag, label: tag }))}
          onChange={(tag) => onChange({ tag })}
        />
      )}
    </>
  )
}
