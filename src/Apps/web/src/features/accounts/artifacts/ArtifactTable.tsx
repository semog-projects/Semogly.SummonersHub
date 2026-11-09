import { Link } from 'react-router'
import { formatNumber } from '@/lib/format'
import { ARTIFACT_MAIN_STAT_LABELS, formatArtifactEffect, RARITY_LABELS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { ELEMENT_TEXT_CLASSES, RARITY_TEXT_CLASSES } from '../overview/elementStyles'
import { MarkCell } from '../marks/MarkCell'
import { SortHeader } from '../shared/SortHeader'
import type { ArtifactListItem } from '../types'
import { artifactKindLabel, subValue, type ArtifactFilters, type ArtifactSortKey } from './artifactFilters'

interface Props {
  wizardId: number
  artifacts: ArtifactListItem[]
  filters: ArtifactFilters
  onSort: (sort: ArtifactSortKey) => void
}

export function ArtifactTable({ wizardId, artifacts, filters, onSort }: Props) {
  if (artifacts.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">Nenhum artefato com esses filtros.</p>
  }

  const header = (sortKey: ArtifactSortKey, label: string) => (
    <SortHeader sortKey={sortKey} label={label} sort={filters.sort} direction={filters.direction} onSort={onSort} />
  )

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="text-left text-xs text-muted-foreground">
          <tr className="border-b">
            <th scope="col" className="py-2 pr-3 font-medium">
              Artefato
            </th>
            <th scope="col" className="py-2 pr-3 font-medium">
              Principal
            </th>
            <th scope="col" className="py-2 pr-3 font-medium">
              Subs
            </th>
            {filters.subEffect !== null && header('subValue', 'Valor da sub')}
            {header('rarity', 'Raridade')}
            {header('level', 'Nível')}
            <th scope="col" className="py-2 pr-3 font-medium">
              Equipado em
            </th>
            <th scope="col" className="py-2 pr-3 font-medium">
              Bloqueio e tags
            </th>
          </tr>
        </thead>
        <tbody>
          {artifacts.map((item) => {
            const { artifact, equippedOn, locked, tags } = item
            return (
              <tr key={artifact.artifactId} className="border-b align-top last:border-0 hover:bg-muted/40">
                <td className={cn('py-2 pr-3 font-medium whitespace-nowrap', RARITY_TEXT_CLASSES[artifact.originalRarity])}>
                  {artifactKindLabel(artifact)}
                </td>
                <td className="py-2 pr-3 whitespace-nowrap">
                  {ARTIFACT_MAIN_STAT_LABELS[artifact.mainStat.type] ?? 'Atributo'} +{formatNumber(artifact.mainStat.value)}
                </td>
                <td className="py-2 pr-3">
                  <ul className="space-y-0.5">
                    {artifact.substats.map((sub) => (
                      <li key={sub.type} className={cn(sub.type === filters.subEffect && 'font-semibold text-foreground')}>
                        {formatArtifactEffect(sub.type, sub.value)}
                      </li>
                    ))}
                  </ul>
                </td>
                {filters.subEffect !== null && (
                  <td className="py-2 pr-3 text-right font-semibold tabular-nums">{formatNumber(subValue(item, filters.subEffect))}</td>
                )}
                <td className={cn('py-2 pr-3 text-right whitespace-nowrap', RARITY_TEXT_CLASSES[artifact.originalRarity])}>
                  {RARITY_LABELS[artifact.originalRarity]}
                </td>
                <td className="py-2 pr-3 text-right tabular-nums">+{artifact.level}</td>
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
                  <MarkCell wizardId={wizardId} kind="Artifact" itemId={artifact.artifactId} locked={locked} tags={tags} />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
