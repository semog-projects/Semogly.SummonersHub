import { Link } from 'react-router'
import { formatPercent } from '@/lib/format'
import { formatRuneStat } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { ELEMENT_TEXT_CLASSES, RARITY_TEXT_CLASSES } from '../overview/elementStyles'
import { MarkCell } from '../marks/MarkCell'
import { SortHeader } from '../shared/SortHeader'
import type { RuneListItem } from '../types'
import { efficiencyClass, runeSpeed } from './efficiency'
import type { RuneFilters, RuneSortKey } from './runeFilters'

interface Props {
  wizardId: number
  runes: RuneListItem[]
  filters: RuneFilters
  onSort: (sort: RuneSortKey) => void
}

/** Tabela compacta de runas; as colunas numéricas ordenam pelo cabeçalho. */
export function RuneTable({ wizardId, runes, filters, onSort }: Props) {
  if (runes.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">Nenhuma runa com esses filtros.</p>
  }

  const header = (sortKey: RuneSortKey, label: string) => (
    <SortHeader sortKey={sortKey} label={label} sort={filters.sort} direction={filters.direction} onSort={onSort} />
  )

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="text-left text-xs text-muted-foreground">
          <tr className="border-b">
            <th scope="col" className="py-2 pr-3 font-medium">
              Runa
            </th>
            <th scope="col" className="py-2 pr-3 font-medium">
              Principal
            </th>
            <th scope="col" className="py-2 pr-3 font-medium">
              Subs
            </th>
            {header('speed', 'VEL')}
            {header('efficiency', 'Eficiência')}
            {header('maxEfficiency', 'Máx.')}
            <th scope="col" className="py-2 pr-3 font-medium">
              Equipada em
            </th>
            <th scope="col" className="py-2 pr-3 font-medium">
              Bloqueio e tags
            </th>
          </tr>
        </thead>
        <tbody>
          {runes.map(({ rune, equippedOn, locked, tags, lockedByMonster }) => (
            <tr key={rune.runeId} className="border-b align-top last:border-0 hover:bg-muted/40">
              <td className="py-2 pr-3 whitespace-nowrap">
                <p className={cn('font-medium', RARITY_TEXT_CLASSES[rune.originalRarity])}>
                  {rune.set} ({rune.slot}) +{rune.level}
                </p>
                <p className="text-xs text-muted-foreground">
                  {rune.stars}★{rune.isAncient && ' · Ancient'}
                </p>
              </td>
              <td className="py-2 pr-3 whitespace-nowrap">
                <p className="font-medium">{formatRuneStat(rune.mainStat.type, rune.mainStat.value)}</p>
                {rune.innateStat && (
                  <p className="text-xs text-muted-foreground">
                    Inata: {formatRuneStat(rune.innateStat.type, rune.innateStat.value)}
                  </p>
                )}
              </td>
              <td className="py-2 pr-3">
                <ul className="flex flex-wrap gap-x-3 gap-y-0.5">
                  {rune.substats.map((sub) => (
                    <li key={sub.type} className="whitespace-nowrap">
                      {formatRuneStat(sub.type, sub.value + sub.grindValue)}
                      {sub.grindValue > 0 && <span className="ml-1 text-xs text-amber-400">({sub.grindValue})</span>}
                      {sub.isGemmed && <span className="ml-1 text-xs text-muted-foreground">gem</span>}
                    </li>
                  ))}
                </ul>
              </td>
              <td className={cn('py-2 pr-3 text-right tabular-nums', filters.sort === 'speed' && 'font-semibold')}>
                {runeSpeed(rune) || '—'}
              </td>
              <td className={cn('py-2 pr-3 text-right font-medium tabular-nums', efficiencyClass(rune.efficiency))}>
                {formatPercent(rune.efficiency)}
              </td>
              <td className="py-2 pr-3 text-right tabular-nums text-muted-foreground">{formatPercent(rune.maxEfficiency)}</td>
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
              <td className="py-2 pr-3">
                <MarkCell wizardId={wizardId} kind="Rune" itemId={rune.runeId} locked={locked} tags={tags} lockedByMonster={lockedByMonster} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
