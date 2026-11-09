import { useMemo } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { ArtifactFiltersBar } from '@/features/accounts/artifacts/ArtifactFiltersBar'
import {
  ARTIFACT_PAGE_SIZE,
  artifactFiltersFromParams,
  artifactFiltersToParams,
  filterAndSortArtifacts,
  type ArtifactFilters,
  type ArtifactSortKey,
} from '@/features/accounts/artifacts/artifactFilters'
import { ArtifactTable } from '@/features/accounts/artifacts/ArtifactTable'
import { availableTags } from '@/features/accounts/marks/markFilters'
import { MarksBulkBar } from '@/features/accounts/marks/MarksBulkBar'
import { ShowMoreButton } from '@/features/accounts/shared/ShowMoreButton'
import { nextSort } from '@/features/accounts/shared/sorting'
import { usePaging } from '@/features/accounts/shared/usePaging'
import type { ArtifactListItem } from '@/features/accounts/types'
import { useArtifacts } from '@/features/accounts/useAccounts'
import { artifactsCsv } from '@/features/accounts/export/listCsv'
import { ExportCsvButton } from '@/features/accounts/shared/ExportCsvButton'
import { QueryStatus } from './QueryStatus'

export function ArtifactListPage() {
  const wizardId = Number(useParams().wizardId)
  const query = useArtifacts(wizardId)
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = artifactFiltersFromParams(searchParams)

  const setFilters = (next: ArtifactFilters) => setSearchParams(artifactFiltersToParams(next), { replace: true })

  return (
    <QueryStatus query={query} notFound="Conta não encontrada." loading="Carregando artefatos…">
      {(artifacts) => (
        <ArtifactList
          wizardId={wizardId}
          artifacts={artifacts}
          filters={filters}
          onChange={setFilters}
          onSort={(sort) => setFilters({ ...filters, ...nextSort(filters, sort, 'desc') })}
        />
      )}
    </QueryStatus>
  )
}

function ArtifactList({
  wizardId,
  artifacts,
  filters,
  onChange,
  onSort,
}: {
  wizardId: number
  artifacts: ArtifactListItem[]
  filters: ArtifactFilters
  onChange: (filters: ArtifactFilters) => void
  onSort: (sort: ArtifactSortKey) => void
}) {
  const visible = useMemo(() => filterAndSortArtifacts([...artifacts], filters), [artifacts, filters])
  const { limit, showMore } = usePaging(artifactFiltersToParams(filters).toString(), ARTIFACT_PAGE_SIZE)
  const tags = useMemo(() => availableTags(artifacts), [artifacts])

  return (
    <div className="space-y-4">
      <ArtifactFiltersBar filters={filters} tags={tags} onChange={onChange} />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-muted-foreground" aria-live="polite">
          {visible.length} de {artifacts.length} artefatos · escolha uma sub para ordenar pelo valor dela
        </p>
        <ExportCsvButton fileName="artefatos.csv" build={() => artifactsCsv(visible)} />
      </div>
      <MarksBulkBar wizardId={wizardId} kind="Artifact" itemIds={visible.map((item) => item.artifact.artifactId)} />
      <ArtifactTable wizardId={wizardId} artifacts={visible.slice(0, limit)} filters={filters} onSort={onSort} />
      <ShowMoreButton total={visible.length} limit={limit} onClick={showMore} />
    </div>
  )
}
