import { Button } from '@/components/ui/button'
import { ARTIFACT_MAIN_STAT_LABELS } from '@/lib/game/labels'
import { SelectFilter } from '../shared/filterControls'
import {
  ARTIFACT_ARCHETYPE_OPTIONS,
  ARTIFACT_EFFECT_OPTIONS,
  ARTIFACT_ELEMENT_OPTIONS,
  DEFAULT_ARTIFACT_FILTERS,
  type ArtifactFilters,
  type ArtifactKind,
  type ArtifactLocation,
} from './artifactFilters'

const TYPE_OPTIONS: Array<{ value: ArtifactKind; label: string }> = [
  { value: 'Element', label: 'Elemento' },
  { value: 'Archetype', label: 'Arquétipo' },
]

const MAIN_STAT_OPTIONS = Object.entries(ARTIFACT_MAIN_STAT_LABELS).map(([type, label]) => ({ value: Number(type), label }))

const LOCATION_OPTIONS: Array<{ value: ArtifactLocation; label: string }> = [
  { value: 'equipped', label: 'Equipados' },
  { value: 'inventory', label: 'No inventário' },
]

interface Props {
  filters: ArtifactFilters
  onChange: (filters: ArtifactFilters) => void
}

export function ArtifactFiltersBar({ filters, onChange }: Props) {
  const update = (patch: Partial<ArtifactFilters>) => onChange({ ...filters, ...patch })

  function changeSub(subEffect: number | null) {
    // Escolher uma sub já ordena pelo valor dela; sem sub, essa ordenação não faz sentido
    if (subEffect !== null) {
      update({ subEffect, sort: 'subValue', direction: 'desc' })
    } else {
      update({ subEffect, ...(filters.sort === 'subValue' && { sort: DEFAULT_ARTIFACT_FILTERS.sort }) })
    }
  }

  return (
    <div role="search" className="flex flex-wrap items-center gap-3">
      <SelectFilter
        label="Tipo"
        allLabel="Todos os tipos"
        value={filters.type}
        options={TYPE_OPTIONS}
        // Trocar o tipo limpa o filtro do outro lado (elemento x arquétipo)
        onChange={(type) =>
          update({
            type,
            element: type === 'Archetype' ? null : filters.element,
            archetype: type === 'Element' ? null : filters.archetype,
          })
        }
      />

      {filters.type !== 'Archetype' && (
        <SelectFilter
          label="Elemento"
          allLabel="Todos os elementos"
          value={filters.element}
          options={ARTIFACT_ELEMENT_OPTIONS}
          onChange={(element) => update({ element })}
        />
      )}

      {filters.type !== 'Element' && (
        <SelectFilter
          label="Arquétipo"
          allLabel="Todos os arquétipos"
          value={filters.archetype}
          options={ARTIFACT_ARCHETYPE_OPTIONS}
          onChange={(archetype) => update({ archetype })}
        />
      )}

      <SelectFilter
        label="Principal"
        allLabel="Qualquer principal"
        value={filters.mainStat}
        options={MAIN_STAT_OPTIONS}
        onChange={(mainStat) => update({ mainStat })}
      />

      <SelectFilter label="Sub" allLabel="Qualquer sub" value={filters.subEffect} options={ARTIFACT_EFFECT_OPTIONS} onChange={changeSub} />

      <SelectFilter
        label="Local"
        allLabel="Equipados e inventário"
        value={filters.location}
        options={LOCATION_OPTIONS}
        onChange={(location) => update({ location })}
      />

      <Button variant="ghost" size="sm" onClick={() => onChange(DEFAULT_ARTIFACT_FILTERS)}>
        Limpar filtros
      </Button>
    </div>
  )
}
