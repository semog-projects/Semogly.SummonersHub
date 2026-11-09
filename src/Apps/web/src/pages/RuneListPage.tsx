import { useMemo } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { availableTags } from '@/features/accounts/marks/markFilters'
import { MarksBulkBar } from '@/features/accounts/marks/MarksBulkBar'
import { RuneFiltersBar } from '@/features/accounts/runes/RuneFiltersBar'
import {
  filterAndSortRunes,
  RUNE_PAGE_SIZE,
  runeFiltersFromParams,
  runeFiltersToParams,
  type RuneFilters,
  type RuneSortKey,
} from '@/features/accounts/runes/runeFilters'
import { RuneTable } from '@/features/accounts/runes/RuneTable'
import { ShowMoreButton } from '@/features/accounts/shared/ShowMoreButton'
import { nextSort } from '@/features/accounts/shared/sorting'
import { usePaging } from '@/features/accounts/shared/usePaging'
import type { RuneListItem } from '@/features/accounts/types'
import { useRunes } from '@/features/accounts/useAccounts'
import { runesCsv } from '@/features/accounts/export/listCsv'
import { ExportCsvButton } from '@/features/accounts/shared/ExportCsvButton'
import { QueryStatus } from './QueryStatus'

export function RuneListPage() {
  const wizardId = Number(useParams().wizardId)
  const query = useRunes(wizardId)
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = runeFiltersFromParams(searchParams)

  const setFilters = (next: RuneFilters) => setSearchParams(runeFiltersToParams(next), { replace: true })

  return (
    <QueryStatus query={query} notFound="Conta não encontrada." loading="Carregando runas…">
      {(runes) => (
        <RuneList
          wizardId={wizardId}
          runes={runes}
          filters={filters}
          onChange={setFilters}
          onSort={(sort) => setFilters({ ...filters, ...nextSort(filters, sort, 'desc') })}
        />
      )}
    </QueryStatus>
  )
}

function RuneList({
  wizardId,
  runes,
  filters,
  onChange,
  onSort,
}: {
  wizardId: number
  runes: RuneListItem[]
  filters: RuneFilters
  onChange: (filters: RuneFilters) => void
  onSort: (sort: RuneSortKey) => void
}) {
  const visible = useMemo(() => filterAndSortRunes([...runes], filters), [runes, filters])
  const { limit, showMore } = usePaging(runeFiltersToParams(filters).toString(), RUNE_PAGE_SIZE)
  const tags = useMemo(() => availableTags(runes), [runes])

  return (
    <div className="space-y-4">
      <RuneFiltersBar filters={filters} tags={tags} onChange={onChange} />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-muted-foreground" aria-live="polite">
          {visible.length} de {runes.length} runas · eficiência no modelo do SW Exporter; a máxima supõe roll máximo nos
          upgrades que faltam até o +12
        </p>
        <ExportCsvButton fileName="runas.csv" build={() => runesCsv(visible)} />
      </div>
      <MarksBulkBar wizardId={wizardId} kind="Rune" itemIds={visible.map((item) => item.rune.runeId)} />
      <RuneTable wizardId={wizardId} runes={visible.slice(0, limit)} filters={filters} onSort={onSort} />
      <ShowMoreButton total={visible.length} limit={limit} onClick={showMore} />
    </div>
  )
}
