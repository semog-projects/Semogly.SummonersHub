import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { MonsterIcon } from '@/features/accounts/monsters/MonsterIcon'
import { ELEMENT_TEXT_CLASSES } from '@/features/accounts/overview/elementStyles'
import { Chip } from '@/features/accounts/shared/filterControls'
import { controlClass, optionClass, selectClass } from '@/features/accounts/shared/filterStyles'
import {
  bestiaryFiltersFromParams,
  bestiaryFiltersToParams,
  DEFAULT_BESTIARY_FILTERS,
  filterBestiary,
  type BestiaryFilters,
} from '@/features/bestiary/bestiaryFilters'
import { useBestiary } from '@/features/bestiary/useBestiary'
import { ARCHETYPE_NAME_LABELS, AWAKEN_LABELS, ELEMENT_NAME_LABELS, type ArchetypeName, type ElementName } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { QueryStatus } from './QueryStatus'

/** Monstros por página: a lista completa tem cerca de 2.000 ícones. */
const PAGE_SIZE = 120

export function BestiaryPage() {
  const query = useBestiary()
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = bestiaryFiltersFromParams(searchParams)
  const [visible, setVisible] = useState(PAGE_SIZE)

  const apply = (next: BestiaryFilters) => {
    setSearchParams(bestiaryFiltersToParams(next), { replace: true })
    setVisible(PAGE_SIZE)
  }
  const update = (patch: Partial<BestiaryFilters>) => apply({ ...filters, ...patch })

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-heading text-2xl font-semibold">Bestiário</h1>
        <p className="text-sm text-muted-foreground">Todos os monstros do jogo, com atributos, skills e skill de líder.</p>
      </div>

      <div role="search" className="flex flex-wrap items-center gap-3">
        <input
          type="search"
          aria-label="Buscar monstro"
          placeholder="Buscar monstro"
          value={filters.search}
          onChange={(event) => update({ search: event.target.value })}
          className={cn(controlClass, 'w-44')}
        />
        <div role="group" aria-label="Estrelas naturais" className="flex gap-1">
          {[5, 4, 3, 2, 1].map((stars) => (
            <Chip key={stars} active={filters.stars === stars} onClick={() => update({ stars: filters.stars === stars ? null : stars })}>
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
          aria-label="Tipo"
          value={filters.archetype ?? ''}
          onChange={(event) => update({ archetype: (event.target.value || null) as ArchetypeName | null })}
          className={selectClass}
        >
          <option value="" className={optionClass}>Todos os tipos</option>
          {Object.entries(ARCHETYPE_NAME_LABELS).map(([value, label]) => (
            <option key={value} value={value} className={optionClass}>
              {label}
            </option>
          ))}
        </select>
        <select
          aria-label="Despertar"
          value={filters.awaken ?? ''}
          onChange={(event) => update({ awaken: event.target.value === '' ? null : Number(event.target.value) })}
          className={selectClass}
        >
          <option value="" className={optionClass}>Qualquer despertar</option>
          {Object.entries(AWAKEN_LABELS).map(([value, label]) => (
            <option key={value} value={value} className={optionClass}>
              {label}
            </option>
          ))}
        </select>
        <Button variant="ghost" size="sm" onClick={() => apply(DEFAULT_BESTIARY_FILTERS)}>
          Limpar filtros
        </Button>
      </div>

      <QueryStatus query={query} notFound="Bestiário indisponível." loading="Carregando bestiário…">
        {(monsters) => {
          const filtered = filterBestiary(monsters, filters)
          return (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">{formatCount(filtered.length)}</p>
              <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4" aria-label="Monstros">
                {filtered.slice(0, visible).map((monster) => (
                  <li key={monster.com2usId}>
                    <Link
                      to={String(monster.com2usId)}
                      className="flex items-center gap-3 rounded-lg border p-2 transition-colors hover:bg-muted/50"
                    >
                      <MonsterIcon imageUrl={monster.imageUrl} className="size-12" />
                      <div className="min-w-0">
                        <p className="truncate font-medium">{monster.name}</p>
                        <p className="text-xs text-muted-foreground">
                          <span className={ELEMENT_TEXT_CLASSES[monster.element]}>{ELEMENT_NAME_LABELS[monster.element]}</span> ·{' '}
                          {monster.naturalStars}★ · {AWAKEN_LABELS[monster.awakenLevel]}
                        </p>
                      </div>
                    </Link>
                  </li>
                ))}
              </ul>
              {filtered.length > visible && (
                <Button variant="outline" onClick={() => setVisible((count) => count + PAGE_SIZE)}>
                  Mostrar mais ({filtered.length - visible} restantes)
                </Button>
              )}
            </div>
          )
        }}
      </QueryStatus>
    </div>
  )
}

function formatCount(count: number): string {
  return count === 1 ? '1 monstro' : `${count.toLocaleString('pt-BR')} monstros`
}
