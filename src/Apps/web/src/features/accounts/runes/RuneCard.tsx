import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { formatPercent } from '@/lib/format'
import { formatRuneStat, RARITY_LABELS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { RARITY_TEXT_CLASSES } from '../overview/elementStyles'
import type { RuneDetail } from '../types'
import { efficiencyClass } from './efficiency'

/** Runa como no jogo: set, slot, nível, principal, inata, subs (com grind e gem) e eficiência. */
export function RuneCard({ slot, rune }: { slot: number; rune?: RuneDetail }) {
  if (!rune) {
    return (
      <Card size="sm" className="border-dashed">
        <CardContent className="text-sm text-muted-foreground">Slot {slot} vazio</CardContent>
      </Card>
    )
  }

  return (
    <Card size="sm" aria-label={`Runa do slot ${slot}`}>
      <CardContent className="space-y-2 text-sm">
        <div className="flex items-baseline justify-between gap-2">
          <p className={cn('font-medium', RARITY_TEXT_CLASSES[rune.originalRarity])}>
            {rune.set} ({slot}) +{rune.level}
          </p>
          <span className="text-xs whitespace-nowrap text-muted-foreground">
            {rune.stars}★{rune.isAncient && ' · Ancient'}
          </span>
        </div>
        <p className="font-semibold">{formatRuneStat(rune.mainStat.type, rune.mainStat.value)}</p>
        {rune.innateStat && (
          <p className="text-xs text-muted-foreground">Inata: {formatRuneStat(rune.innateStat.type, rune.innateStat.value)}</p>
        )}
        <ul className="space-y-0.5">
          {rune.substats.map((sub) => (
            <li key={sub.type} className="flex items-center justify-between gap-2">
              <span>
                {formatRuneStat(sub.type, sub.value + sub.grindValue)}
                {sub.grindValue > 0 && <span className="ml-1 text-xs text-amber-400">({sub.grindValue} grind)</span>}
              </span>
              {sub.isGemmed && (
                <Badge variant="outline" className="text-[10px]">
                  gem
                </Badge>
              )}
            </li>
          ))}
        </ul>
        <div className="flex items-baseline justify-between gap-2 text-xs">
          <span className="text-muted-foreground">{RARITY_LABELS[rune.originalRarity] ?? 'Raridade desconhecida'}</span>
          <span>
            <span className="text-muted-foreground">Eficiência </span>
            <span className={cn('font-medium', efficiencyClass(rune.efficiency))}>{formatPercent(rune.efficiency)}</span>
            {rune.maxEfficiency > rune.efficiency && (
              <span className="text-muted-foreground"> · máx. {formatPercent(rune.maxEfficiency)}</span>
            )}
          </span>
        </div>
      </CardContent>
    </Card>
  )
}
