import { Button } from '@/components/ui/button'
import { EQUIPPABLE_RUNE_SETS } from '@/lib/game/labels'
import { Chip, SelectFilter } from '../shared/filterControls'
import { DEFAULT_RUNE_FILTERS, RUNE_STAT_OPTIONS, type RuneFilters, type RuneLocation } from './runeFilters'

const SET_OPTIONS = EQUIPPABLE_RUNE_SETS.map((set) => ({ value: set, label: set }))

const LOCATION_OPTIONS: Array<{ value: RuneLocation; label: string }> = [
  { value: 'equipped', label: 'Equipadas' },
  { value: 'inventory', label: 'No inventário' },
]

interface Props {
  filters: RuneFilters
  onChange: (filters: RuneFilters) => void
}

export function RuneFiltersBar({ filters, onChange }: Props) {
  const update = (patch: Partial<RuneFilters>) => onChange({ ...filters, ...patch })

  return (
    <div role="search" className="flex flex-wrap items-center gap-3">
      <div role="group" aria-label="Slot" className="flex gap-1">
        {[1, 2, 3, 4, 5, 6].map((slot) => (
          <Chip key={slot} active={filters.slot === slot} onClick={() => update({ slot: filters.slot === slot ? null : slot })}>
            {slot}
          </Chip>
        ))}
      </div>

      <SelectFilter label="Set" allLabel="Todos os sets" value={filters.set} options={SET_OPTIONS} onChange={(set) => update({ set })} />

      <SelectFilter
        label="Principal"
        allLabel="Qualquer principal"
        value={filters.mainStat}
        options={RUNE_STAT_OPTIONS}
        onChange={(mainStat) => update({ mainStat })}
      />

      <SelectFilter
        label="Sub"
        allLabel="Qualquer sub"
        value={filters.subStat}
        options={RUNE_STAT_OPTIONS}
        onChange={(subStat) => update({ subStat })}
      />

      <SelectFilter
        label="Local"
        allLabel="Equipadas e inventário"
        value={filters.location}
        options={LOCATION_OPTIONS}
        onChange={(location) => update({ location })}
      />

      <Button
        variant="ghost"
        size="sm"
        onClick={() => onChange({ ...DEFAULT_RUNE_FILTERS, sort: filters.sort, direction: filters.direction })}
      >
        Limpar filtros
      </Button>
    </div>
  )
}
