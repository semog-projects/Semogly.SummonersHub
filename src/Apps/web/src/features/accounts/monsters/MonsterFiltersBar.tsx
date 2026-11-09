import { Button } from '@/components/ui/button'
import { ELEMENT_NAME_LABELS, EQUIPPABLE_RUNE_SETS, type ElementName } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { BUILD_KIND_LABELS } from '../builds/buildLabels'
import { Chip } from '../shared/filterControls'
import { controlClass, optionClass, selectClass } from '../shared/filterStyles'
import type { BuildKindName } from '../types'
import { DEFAULT_FILTERS, type MonsterFilters } from './monsterFilters'

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
        className={selectClass}
      >
        <option value="" className={optionClass}>Todos os elementos</option>
        {Object.entries(ELEMENT_NAME_LABELS).map(([value, label]) => (
          <option key={value} value={value} className={optionClass}>
            {label}
          </option>
        ))}
      </select>

      <select
        aria-label="Set"
        value={filters.set ?? ''}
        onChange={(event) => update({ set: event.target.value || null })}
        className={selectClass}
      >
        <option value="" className={optionClass}>Todos os sets</option>
        {EQUIPPABLE_RUNE_SETS.map((set) => (
          <option key={set} value={set} className={optionClass}>
            {set}
          </option>
        ))}
      </select>

      <select
        aria-label="Build própria"
        value={filters.buildKind ?? ''}
        onChange={(event) => update({ buildKind: (event.target.value || null) as BuildKindName | null })}
        className={selectClass}
      >
        <option value="" className={optionClass}>Qualquer build</option>
        {Object.entries(BUILD_KIND_LABELS).map(([value, label]) => (
          <option key={value} value={value} className={optionClass}>
            Com build de {label}
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
