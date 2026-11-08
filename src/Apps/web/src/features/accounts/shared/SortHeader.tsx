import { ArrowDown, ArrowUp } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { SortDirection } from './sorting'

interface Props<K extends string> {
  sortKey: K
  label: string
  align?: 'left' | 'right'
  sort: K
  direction: SortDirection
  onSort: (sort: K) => void
}

/** Cabeçalho de coluna que ordena a tabela; a coluna ativa mostra a direção. */
export function SortHeader<K extends string>({ sortKey, label, align = 'right', sort, direction, onSort }: Props<K>) {
  const active = sort === sortKey
  const Icon = direction === 'asc' ? ArrowUp : ArrowDown
  return (
    <th
      scope="col"
      aria-sort={active ? (direction === 'asc' ? 'ascending' : 'descending') : 'none'}
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
