import { Button } from '@/components/ui/button'
import { ELEMENT_NAME_LABELS, EQUIPPABLE_RUNE_SETS, type ElementName } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { DEFAULT_FILTERS, type MonsterFilters } from './monsterFilters'

const controlClass =
  'h-8 rounded-md border bg-transparent px-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/50'

interface Props {
  filters: MonsterFilters
  onChange: (filters: MonsterFilters) => void
}

export function MonsterFiltersBar({ filters, onChange }: Props) {
  const update = (patch: Partial<MonsterFilters>) => onChange({ ...filters, ...patch })

  return (
    <div role="search" className="flex flex-wrap items-center gap-3">
      <input
        type="search"
        aria-label="Buscar monstro"
        placeholder="Buscar monstro"
        value={filters.search}
        onChange={(event) => update({ search: event.target.value })}
        className={cn(controlClass, 'w-44')}
      />

      <div role="group" aria-label="Estrelas" className="flex gap-1">
        {[6, 5, 4, 3, 2, 1].map((stars) => (
          <Chip
            key={stars}
            active={filters.stars === stars}
            onClick={() => update({ stars: filters.stars === stars ? null : stars })}
          >
            {stars}★
          </Chip>
        ))}
      </div>

      <select
        aria-label="Elemento"
        value={filters.element ?? ''}
        onChange={(event) => update({ element: (event.target.value || null) as ElementName | null })}
        className={controlClass}
      >
        <option value="">Todos os elementos</option>
        {Object.entries(ELEMENT_NAME_LABELS).map(([value, label]) => (
          <option key={value} value={value}>
            {label}
          </option>
        ))}
      </select>

      <select
        aria-label="Set"
        value={filters.set ?? ''}
        onChange={(event) => update({ set: event.target.value || null })}
        className={controlClass}
      >
        <option value="">Todos os sets</option>
        {EQUIPPABLE_RUNE_SETS.map((set) => (
          <option key={set} value={set}>
            {set}
          </option>
        ))}
      </select>

      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={filters.includeMaterials}
          onChange={(event) => update({ includeMaterials: event.target.checked })}
        />
        Mostrar materiais
      </label>

      <Button
        variant="ghost"
        size="sm"
        onClick={() => onChange({ ...DEFAULT_FILTERS, sort: filters.sort, direction: filters.direction })}
      >
        Limpar filtros
      </Button>
    </div>
  )
}

function Chip({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={cn(
        'h-8 rounded-md border px-2 text-sm tabular-nums transition-colors hover:bg-muted',
        active && 'border-primary bg-muted font-medium',
      )}
    >
      {children}
    </button>
  )
}
