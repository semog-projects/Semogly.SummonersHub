import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { formatNumber, formatPercent } from '@/lib/format'
import { ARTIFACT_MAIN_STAT_LABELS, formatRuneStat } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { artifactKindLabel } from '../artifacts/artifactFilters'
import { ELEMENT_TEXT_CLASSES, RARITY_TEXT_CLASSES } from '../overview/elementStyles'
import { efficiencyClass } from '../runes/efficiency'
import type { OptimizedBuild, OptimizeRunesResponse } from '../types'
import { mainStatLabel, OPTIMIZER_STATS } from './optimizerOptions'

interface Props {
  wizardId: number
  monsterId: number
  result: OptimizeRunesResponse
}

export function OptimizerResults({ wizardId, monsterId, result }: Props) {
  if (result.builds.length === 0) {
    return (
      <p role="status" className="text-sm text-muted-foreground">
        Nenhuma build atende aos critérios. Tente afrouxar os mínimos, liberar o principal de algum slot ou usar runas de
        outros monstros.
      </p>
    )
  }

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        {result.builds.length} builds, da melhor para a pior. Pontuação = soma dos atributos de foco em relação à base do
        monstro (atual: {formatNumber(result.current.score)}).
      </p>
      {result.builds.map((build, i) => (
        <BuildCard
          key={i}
          rank={i + 1}
          build={build}
          current={result.current}
          wizardId={wizardId}
          monsterId={monsterId}
          open={i === 0}
        />
      ))}
    </div>
  )
}

function BuildCard({
  rank,
  build,
  current,
  wizardId,
  monsterId,
  open,
}: {
  rank: number
  build: OptimizedBuild
  current: OptimizedBuild
  wizardId: number
  monsterId: number
  open: boolean
}) {
  const fromOthers = build.runes.filter((r) => r.equippedOn && r.equippedOn.monsterId !== monsterId).length
  const scoreDiff = build.score - current.score

  return (
    <details open={open} className="rounded-xl border bg-card" aria-label={`Build ${rank}`}>
      <summary className="flex cursor-pointer flex-wrap items-center gap-x-3 gap-y-1 px-4 py-3 text-sm">
        <span className="font-medium">Build {rank}</span>
        <span className="tabular-nums">
          {formatNumber(build.score)}
          <span className={cn('ml-1 text-xs', scoreDiff >= 0 ? 'text-emerald-400' : 'text-red-400')}>
            ({scoreDiff >= 0 ? '+' : ''}
            {formatNumber(Math.round(scoreDiff * 100) / 100)})
          </span>
        </span>
        <span className="flex flex-wrap gap-1">
          {build.activeSets.map((set, i) => (
            <Badge key={`${set}-${i}`} variant="secondary">
              {set}
            </Badge>
          ))}
        </span>
        <span className="text-xs text-muted-foreground sm:ml-auto">
          {fromOthers === 0 ? 'Sem tirar runas de outros monstros' : `${fromOthers} runa(s) de outros monstros`}
        </span>
      </summary>

      <div className="space-y-4 border-t px-4 py-3">
        <table className="w-full text-sm" aria-label="Atributos da build">
          <thead className="text-xs text-muted-foreground">
            <tr>
              <th scope="col" className="pb-1 text-left font-medium">
                Atributo
              </th>
              <th scope="col" className="pb-1 text-right font-medium">
                Atual
              </th>
              <th scope="col" className="pb-1 text-right font-medium">
                Build
              </th>
            </tr>
          </thead>
          <tbody>
            {OPTIMIZER_STATS.map(({ key, label, percent }) => {
              const diff = build.total[key] - current.total[key]
              const suffix = percent ? '%' : ''
              return (
                <tr key={key}>
                  <th scope="row" className="py-0.5 text-left font-normal text-muted-foreground">
                    {label}
                  </th>
                  <td className="py-0.5 text-right tabular-nums text-muted-foreground">
                    {formatNumber(current.total[key])}
                    {suffix}
                  </td>
                  <td className="py-0.5 text-right tabular-nums">
                    {formatNumber(build.total[key])}
                    {suffix}
                    {diff !== 0 && (
                      <span className={cn('ml-1 text-xs', diff > 0 ? 'text-emerald-400' : 'text-red-400')}>
                        ({diff > 0 ? '+' : ''}
                        {formatNumber(diff)})
                      </span>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>

        <ul className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3" aria-label="Runas da build">
          {build.runes.map(({ rune, equippedOn }) => {
            const gem = build.gems?.find((g) => g.runeId === rune.runeId)
            return (
              <li key={rune.runeId} className="rounded-lg border p-2 text-sm">
                <div className="flex items-baseline justify-between gap-2">
                  <span className={cn('font-medium', RARITY_TEXT_CLASSES[rune.originalRarity])}>
                    {rune.set} ({rune.slot}) +{rune.level}
                  </span>
                  <span className={cn('text-xs', efficiencyClass(rune.efficiency))}>{formatPercent(rune.efficiency)}</span>
                </div>
                <p>{formatRuneStat(rune.mainStat.type, rune.mainStat.value)}</p>
                <p className="text-xs text-muted-foreground">
                  {rune.substats.map((s) => formatRuneStat(s.type, s.value + s.grindValue)).join(' · ')}
                </p>
                {gem && (
                  <p className="text-xs text-violet-400">
                    Gem: {mainStatLabel(gem.from)} → {formatRuneStat(gem.to, gem.value)}
                  </p>
                )}
                <p className="mt-1 text-xs">
                  {!equippedOn ? (
                    <span className="text-muted-foreground">Do inventário</span>
                  ) : equippedOn.monsterId === monsterId ? (
                    <span className="text-muted-foreground">Já equipada</span>
                  ) : (
                    <>
                      <span className="text-amber-400">Tirar de </span>
                      <Link
                        to={`/contas/${wizardId}/monstros/${equippedOn.monsterId}`}
                        className={cn('hover:underline', ELEMENT_TEXT_CLASSES[equippedOn.element])}
                      >
                        {equippedOn.name ?? `Monstro ${equippedOn.masterId}`}
                      </Link>
                    </>
                  )}
                </p>
              </li>
            )
          })}
        </ul>

        <ul className="flex flex-wrap gap-x-6 gap-y-1 text-sm" aria-label="Artefatos da build">
          {build.artifacts.map(({ artifact, equippedOn }) => (
            <li key={artifact.artifactId}>
              <span className={cn('font-medium', RARITY_TEXT_CLASSES[artifact.originalRarity])}>{artifactKindLabel(artifact)}</span>{' '}
              {ARTIFACT_MAIN_STAT_LABELS[artifact.mainStat.type] ?? 'Atributo'} +{formatNumber(artifact.mainStat.value)}{' '}
              <span className="text-xs text-muted-foreground">
                {!equippedOn ? '· do inventário' : equippedOn.monsterId === monsterId ? '· já equipado' : `· tirar de ${equippedOn.name ?? 'outro monstro'}`}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </details>
  )
}
