import { Link } from 'react-router'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { formatPercent } from '@/lib/format'
import { formatRuneStat, RARITY_LABELS, RUNE_STATS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { ELEMENT_TEXT_CLASSES, RARITY_TEXT_CLASSES } from '../overview/elementStyles'
import { efficiencyClass } from '../runes/efficiency'
import type { CraftSuggestionGroup, CraftSuggestionItem } from '../types'
import { craftGroupTitle, isGemType } from './craftLabels'

export function CraftSuggestionCard({ wizardId, group }: { wizardId: number; group: CraftSuggestionGroup }) {
  const gem = isGemType(group.type)
  const percent = RUNE_STATS[group.stat]?.percent ? '%' : ''

  return (
    <Card aria-label={craftGroupTitle(group)}>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-baseline gap-x-2">
          <span>{craftGroupTitle(group)}</span>
          <span className={cn('text-sm font-normal', RARITY_TEXT_CLASSES[group.rarity])}>{RARITY_LABELS[group.rarity]}</span>
          <span className="text-sm font-normal text-muted-foreground">×{group.amount}</span>
        </CardTitle>
        <CardDescription>
          {group.range
            ? `${gem ? 'Sub nova' : 'Grind'} de ${group.range.min}${percent} a ${group.range.max}${percent}`
            : 'Itens ancient ainda não têm sugestão: a faixa de valores deles não está disponível.'}
        </CardDescription>
      </CardHeader>
      {group.range && (
        <CardContent>
          {group.suggestions.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhuma runa compatível ganharia eficiência com este item.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="text-left text-xs text-muted-foreground">
                  <tr className="border-b">
                    <th scope="col" className="py-2 pr-3 font-medium">
                      Runa
                    </th>
                    <th scope="col" className="py-2 pr-3 font-medium">
                      {gem ? 'Troca' : 'Sub'}
                    </th>
                    <th scope="col" className="py-2 pr-3 text-right font-medium">
                      Ganho de eficiência
                    </th>
                    <th scope="col" className="py-2 pr-3 text-right font-medium">
                      Eficiência
                    </th>
                    <th scope="col" className="py-2 pr-3 font-medium">
                      Equipada em
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {group.suggestions.map((suggestion) => (
                    <SuggestionRow key={suggestion.rune.runeId} wizardId={wizardId} suggestion={suggestion} gem={gem} gemStat={group.stat} />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      )}
    </Card>
  )
}

function SuggestionRow({
  wizardId,
  suggestion,
  gem,
  gemStat,
}: {
  wizardId: number
  suggestion: CraftSuggestionItem
  gem: boolean
  gemStat: number
}) {
  const { rune, equippedOn } = suggestion
  const range = (stat: number) => {
    const min = formatRuneStat(stat, suggestion.minValue)
    const percent = RUNE_STATS[stat]?.percent ? '%' : ''
    return suggestion.minValue === suggestion.maxValue ? min : `${min} a ${suggestion.maxValue}${percent}`
  }

  return (
    <tr className="border-b last:border-0">
      <td className={cn('py-2 pr-3 font-medium whitespace-nowrap', RARITY_TEXT_CLASSES[rune.originalRarity])}>
        {rune.set} ({rune.slot}) +{rune.level}
      </td>
      <td className="py-2 pr-3 whitespace-nowrap">
        <span className="text-muted-foreground">{formatRuneStat(suggestion.targetStat, suggestion.currentValue)} → </span>
        {range(gem ? gemStat : suggestion.targetStat)}
      </td>
      <td className="py-2 pr-3 text-right whitespace-nowrap tabular-nums">
        <span className={cn(suggestion.minEfficiencyGain < 0 ? 'text-red-400' : 'text-muted-foreground')}>
          {formatGain(suggestion.minEfficiencyGain)}
        </span>
        <span className="text-muted-foreground"> a </span>
        <span className="font-semibold text-emerald-400">{formatGain(suggestion.maxEfficiencyGain)}</span>
      </td>
      <td className="py-2 pr-3 text-right whitespace-nowrap tabular-nums">
        <span className="text-muted-foreground">{formatPercent(rune.efficiency)} → </span>
        <span className={efficiencyClass(rune.efficiency + suggestion.maxEfficiencyGain)}>
          {formatPercent(rune.efficiency + suggestion.maxEfficiencyGain)}
        </span>
      </td>
      <td className="py-2 pr-3 whitespace-nowrap">
        {equippedOn ? (
          <Link
            to={`/contas/${wizardId}/monstros/${equippedOn.monsterId}`}
            className={cn('hover:underline', ELEMENT_TEXT_CLASSES[equippedOn.element])}
          >
            {equippedOn.name ?? `Monstro ${equippedOn.masterId}`}
          </Link>
        ) : (
          <span className="text-xs text-muted-foreground">Inventário</span>
        )}
      </td>
    </tr>
  )
}

/** Pontos percentuais com sinal, ex.: +5,95 pp. */
function formatGain(value: number): string {
  return `${value > 0 ? '+' : ''}${formatPercent(value).replace('%', '')} pp`
}
