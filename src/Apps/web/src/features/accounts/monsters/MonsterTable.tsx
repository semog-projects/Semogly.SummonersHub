import { ArrowDown, ArrowUp } from 'lucide-react'
import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { useMonsterDefinitions } from '@/features/game-data/useMonsterDefinitions'
import { formatNumber } from '@/lib/format'
import { ELEMENT_NAME_LABELS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { ELEMENT_TEXT_CLASSES } from '../overview/elementStyles'
import type { MonsterListItem } from '../types'
import { MonsterIcon } from './MonsterIcon'
import type { MonsterFilters, SortKey } from './monsterFilters'
import { STAT_COLUMNS } from './statColumns'

interface Props {
  monsters: MonsterListItem[]
  filters: MonsterFilters
  onSort: (sort: SortKey) => void
}

/** Tabela de monstros com atributos totais; o cabeçalho de cada coluna ordena por ela. */
export function MonsterTable({ monsters, filters, onSort }: Props) {
  const { data: definitions } = useMonsterDefinitions(monsters.map((monster) => monster.masterId))

  if (monsters.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">Nenhum monstro com esses filtros.</p>
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="text-left text-xs text-muted-foreground">
          <tr className="border-b">
            <SortHeader sortKey="name" label="Monstro" align="left" filters={filters} onSort={onSort} />
            <th scope="col" className="py-2 pr-3 font-medium">
              Sets
            </th>
            {STAT_COLUMNS.map((column) => (
              <SortHeader key={column.key} sortKey={column.key} label={column.label} filters={filters} onSort={onSort} />
            ))}
          </tr>
        </thead>
        <tbody>
          {monsters.map((monster) => (
            <tr key={monster.monsterId} className="border-b last:border-0 hover:bg-muted/40">
              <td className="py-2 pr-3">
                <div className="flex items-center gap-2">
                  <MonsterIcon imageUrl={definitions?.get(monster.masterId)?.imageUrl} />
                  <div className="min-w-0">
                    <Link to={String(monster.monsterId)} className="font-medium hover:underline">
                      {monster.name ?? `Monstro ${monster.masterId}`}
                    </Link>
                    <p className="text-xs whitespace-nowrap text-muted-foreground">
                      <span className={ELEMENT_TEXT_CLASSES[monster.element]}>{ELEMENT_NAME_LABELS[monster.element]}</span>{' '}
                      · {monster.stars}★ · nv. {monster.level}
                    </p>
                  </div>
                </div>
              </td>
              <td className="py-2 pr-3">
                <div className="flex flex-wrap gap-1">
                  {monster.activeSets.length === 0 ? (
                    <span className="text-xs text-muted-foreground">{monster.runes === 0 ? 'Sem runas' : '—'}</span>
                  ) : (
                    monster.activeSets.map((set, i) => (
                      <Badge key={`${set}-${i}`} variant="secondary">
                        {set}
                      </Badge>
                    ))
                  )}
                </div>
              </td>
              {STAT_COLUMNS.map((column) => (
                <td
                  key={column.key}
                  className={cn(
                    'py-2 pr-3 text-right tabular-nums',
                    filters.sort === column.key && 'font-semibold',
                  )}
                >
                  {formatNumber(monster.total[column.key])}
                  {column.percent && '%'}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

interface SortHeaderProps {
  sortKey: SortKey
  label: string
  align?: 'left' | 'right'
  filters: MonsterFilters
  onSort: (sort: SortKey) => void
}

function SortHeader({ sortKey, label, align = 'right', filters, onSort }: SortHeaderProps) {
  const active = filters.sort === sortKey
  const Icon = filters.direction === 'asc' ? ArrowUp : ArrowDown
  return (
    <th
      scope="col"
      aria-sort={active ? (filters.direction === 'asc' ? 'ascending' : 'descending') : 'none'}
      className={cn('py-2 pr-3 font-medium whitespace-nowrap', align === 'right' && 'text-right')}
    >
      <button
        type="button"
        onClick={() => onSort(sortKey)}
        className={cn('inline-flex items-center gap-1 hover:text-foreground', active && 'text-foreground')}
      >
        {label}
        {active && <Icon className="size-3" aria-hidden />}
      </button>
    </th>
  )
}
