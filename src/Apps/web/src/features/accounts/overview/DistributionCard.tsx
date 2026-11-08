import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { cn } from '@/lib/utils'

export interface DistributionItem {
  label: string
  count: number
  /** Classe de cor da barra (ex.: cor do elemento). */
  barClassName?: string
}

/** Barras horizontais com a quantidade de monstros por categoria, na escala do maior valor. */
export function DistributionCard({ title, items }: { title: string; items: DistributionItem[] }) {
  const max = Math.max(1, ...items.map((item) => item.count))

  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <ul className="space-y-2" aria-label={title}>
          {items.map((item) => (
            <li key={item.label} className="grid grid-cols-[6.5rem_1fr_2.5rem] items-center gap-3 text-sm">
              <span className="truncate text-muted-foreground">{item.label}</span>
              <span className="h-2 overflow-hidden rounded-full bg-muted">
                <span
                  className={cn('block h-full rounded-full bg-primary', item.barClassName)}
                  style={{ width: `${(item.count / max) * 100}%` }}
                />
              </span>
              <span className="text-right font-medium tabular-nums">{item.count}</span>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  )
}
