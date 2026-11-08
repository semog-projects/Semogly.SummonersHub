import { formatNumber, formatPercent } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { Change } from '../types'

/** "antes → depois (+diferença)", com a diferença em verde quando sobe e vermelho quando desce. */
export function Delta({ change, percent = false }: { change: Change<number>; percent?: boolean }) {
  const format = percent ? formatPercent : formatNumber
  const diff = Math.round((change.to - change.from) * 100) / 100

  if (diff === 0) {
    return <span className="tabular-nums">{format(change.to)}</span>
  }

  return (
    <span className="tabular-nums whitespace-nowrap">
      <span className="text-muted-foreground">{format(change.from)} → </span>
      {format(change.to)}
      <span className={cn('ml-1 text-xs', diff > 0 ? 'text-emerald-400' : 'text-red-400')}>
        ({diff > 0 ? '+' : ''}
        {format(diff)})
      </span>
    </span>
  )
}
