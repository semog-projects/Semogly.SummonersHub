import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useMonsterDefinitions } from '@/features/game-data/useMonsterDefinitions'
import { ELEMENT_NAME_LABELS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import type { SpeedRankingEntry } from '../types'
import { ELEMENT_TEXT_CLASSES } from './elementStyles'

/** Monstros mais rápidos da conta pela velocidade total (base + runas + sets + artefatos). */
export function SpeedRankingCard({ entries }: { entries: SpeedRankingEntry[] }) {
  // Ícones vêm do bestiário (cache da sessão)
  const { data: definitions } = useMonsterDefinitions(entries.map((entry) => entry.masterId))

  return (
    <Card>
      <CardHeader>
        <CardTitle>Ranking de velocidade</CardTitle>
        <CardDescription>
          Os {entries.length} monstros mais rápidos, sem bônus de líder, torres ou buffs. Materiais ficam de fora.
        </CardDescription>
      </CardHeader>
      <CardContent className="overflow-x-auto">
        {entries.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhum monstro no último import.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="text-left text-xs text-muted-foreground">
              <tr className="border-b">
                <th className="py-2 pr-3 font-medium">#</th>
                <th className="py-2 pr-3 font-medium">Monstro</th>
                <th className="hidden py-2 pr-3 font-medium sm:table-cell">Sets</th>
                <th className="py-2 pr-3 text-right font-medium">Base</th>
                <th className="py-2 text-right font-medium">Velocidade</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((entry, index) => {
                const imageUrl = definitions?.get(entry.masterId)?.imageUrl
                return (
                  <tr key={entry.monsterId} className="border-b last:border-0">
                    <td className="py-2 pr-3 tabular-nums text-muted-foreground">{index + 1}</td>
                    <td className="py-2 pr-3">
                      <div className="flex items-center gap-2">
                        {imageUrl ? (
                          <img src={imageUrl} alt="" loading="lazy" className="size-8 rounded-md" />
                        ) : (
                          <span className="size-8 rounded-md bg-muted" />
                        )}
                        <div className="min-w-0">
                          <p className="truncate font-medium">{entry.name ?? `Monstro ${entry.masterId}`}</p>
                          <p className="text-xs text-muted-foreground">
                            <span className={cn(ELEMENT_TEXT_CLASSES[entry.element])}>
                              {ELEMENT_NAME_LABELS[entry.element]}
                            </span>{' '}
                            · {entry.stars}★ · nível {entry.level}
                          </p>
                        </div>
                      </div>
                    </td>
                    <td className="hidden py-2 pr-3 sm:table-cell">
                      <div className="flex flex-wrap gap-1">
                        {entry.activeSets.map((set, i) => (
                          <Badge key={`${set}-${i}`} variant="secondary">
                            {set}
                          </Badge>
                        ))}
                      </div>
                    </td>
                    <td className="py-2 pr-3 text-right tabular-nums text-muted-foreground">{entry.baseSpeed}</td>
                    <td className="py-2 text-right">
                      <span className="font-semibold tabular-nums">{entry.totalSpeed}</span>
                      <span className="ml-1 text-xs tabular-nums text-muted-foreground">
                        +{entry.totalSpeed - entry.baseSpeed}
                      </span>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </CardContent>
    </Card>
  )
}
