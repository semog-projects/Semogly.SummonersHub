import { useMemo } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { MonsterFiltersBar } from '@/features/accounts/monsters/MonsterFiltersBar'
import { MonsterTable } from '@/features/accounts/monsters/MonsterTable'
import {
  filterAndSortMonsters,
  filtersFromParams,
  filtersToParams,
  type MonsterFilters,
  type SortKey,
} from '@/features/accounts/monsters/monsterFilters'
import { useMonsters } from '@/features/accounts/useAccounts'
import { QueryStatus } from './QueryStatus'

export function MonsterListPage() {
  const wizardId = Number(useParams().wizardId)
  const query = useMonsters(wizardId)
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = filtersFromParams(searchParams)

  // replace: digitar na busca não deve encher o histórico do navegador
  const setFilters = (next: MonsterFilters) => setSearchParams(filtersToParams(next), { replace: true })

  function handleSort(sort: SortKey) {
    // Mesma coluna inverte a direção; coluna nova começa pelo maior (nome começa pelo A)
    const direction =
      filters.sort === sort ? (filters.direction === 'asc' ? 'desc' : 'asc') : sort === 'name' ? 'asc' : 'desc'
    setFilters({ ...filters, sort, direction })
  }

  return (
    <QueryStatus query={query} notFound="Conta não encontrada." loading="Carregando monstros…">
      {(monsters) => <MonsterList monsters={monsters} filters={filters} onChange={setFilters} onSort={handleSort} />}
    </QueryStatus>
  )
}

function MonsterList({
  monsters,
  filters,
  onChange,
  onSort,
}: {
  monsters: Parameters<typeof filterAndSortMonsters>[0]
  filters: MonsterFilters
  onChange: (filters: MonsterFilters) => void
  onSort: (sort: SortKey) => void
}) {
  const visible = useMemo(() => filterAndSortMonsters([...monsters], filters), [monsters, filters])

  return (
    <div className="space-y-4">
      <MonsterFiltersBar filters={filters} onChange={onChange} />
      <p className="text-xs text-muted-foreground" aria-live="polite">
        {visible.length} de {monsters.length} monstros · atributos totais, sem bônus de líder, torres ou buffs
      </p>
      <MonsterTable monsters={visible} filters={filters} onSort={onSort} />
    </div>
  )
}
