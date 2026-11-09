import { Sparkles, Swords } from 'lucide-react'
import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { buttonVariants } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useMonsterDefinitions } from '@/features/game-data/useMonsterDefinitions'
import { formatNumber, formatPercent } from '@/lib/format'
import {
  ARCHETYPE_NAME_LABELS,
  ARTIFACT_MAIN_STAT_LABELS,
  AWAKEN_LABELS,
  ELEMENT_NAME_LABELS,
  formatArtifactEffect,
  RARITY_LABELS,
} from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { ELEMENT_TEXT_CLASSES, RARITY_TEXT_CLASSES } from '../overview/elementStyles'
import type { ArtifactDetail, MonsterDetail } from '../types'
import { efficiencyClass } from '../runes/efficiency'
import { RuneCard } from '../runes/RuneCard'
import { MonsterIcon } from './MonsterIcon'
import { STAT_COLUMNS } from './statColumns'

/** @param readOnly Esconde as ações da conta (otimizar, calcular dano), como na build compartilhada. */
export function MonsterDetailView({ monster, readOnly = false }: { monster: MonsterDetail; readOnly?: boolean }) {
  const { data: definitions } = useMonsterDefinitions([monster.masterId])
  const runesBySlot = new Map(monster.runes.map((rune) => [rune.slot, rune]))

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-4">
        <MonsterIcon imageUrl={definitions?.get(monster.masterId)?.imageUrl} className="size-16 rounded-lg" />
        <div>
          <h1 className="font-heading text-2xl font-semibold">{monster.name ?? `Monstro ${monster.masterId}`}</h1>
          <p className="text-sm text-muted-foreground">
            <span className={ELEMENT_TEXT_CLASSES[monster.element]}>{ELEMENT_NAME_LABELS[monster.element]}</span>
            {monster.archetype && ` · ${ARCHETYPE_NAME_LABELS[monster.archetype]}`} · {monster.stars}★ · nível{' '}
            {monster.level}
            {monster.awakenLevel !== null && ` · ${AWAKEN_LABELS[monster.awakenLevel]}`}
          </p>
          {monster.averageRuneEfficiency !== null && (
            <p className="text-xs text-muted-foreground">
              Eficiência média das runas:{' '}
              <span className={cn('font-medium', efficiencyClass(monster.averageRuneEfficiency))}>
                {formatPercent(monster.averageRuneEfficiency)}
              </span>
            </p>
          )}
          {monster.skills.length > 0 && (
            <p className="text-xs text-muted-foreground">
              Níveis das skills: {monster.skills.map((skill) => skill.level).join(' · ')}
            </p>
          )}
        </div>
        <div className="flex flex-wrap items-center gap-2 sm:ml-auto">
          {monster.activeSets.map((set, i) => (
            <Badge key={`${set}-${i}`}>{set}</Badge>
          ))}
          {!readOnly && (
            <>
              <Link to="otimizar" className={buttonVariants({ variant: 'outline', size: 'sm' })}>
                <Sparkles aria-hidden /> Otimizar runas
              </Link>
              <Link to="dano" className={buttonVariants({ variant: 'outline', size: 'sm' })}>
                <Swords aria-hidden /> Calcular dano
              </Link>
            </>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[18rem_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Atributos</CardTitle>
          </CardHeader>
          <CardContent>
            <table className="w-full text-sm" aria-label="Atributos">
              <thead className="text-xs text-muted-foreground">
                <tr>
                  <th scope="col" className="pb-2 text-left font-medium">
                    Atributo
                  </th>
                  <th scope="col" className="pb-2 text-right font-medium">
                    Base
                  </th>
                  <th scope="col" className="pb-2 text-right font-medium">
                    Total
                  </th>
                </tr>
              </thead>
              <tbody>
                {STAT_COLUMNS.map((column) => {
                  const suffix = column.percent ? '%' : ''
                  const bonus = monster.total[column.key] - monster.base[column.key]
                  return (
                    <tr key={column.key}>
                      <th scope="row" className="py-1 text-left font-normal text-muted-foreground">
                        {column.label}
                      </th>
                      <td className="py-1 text-right tabular-nums text-muted-foreground">
                        {formatNumber(monster.base[column.key])}
                        {suffix}
                      </td>
                      <td className="py-1 text-right tabular-nums">
                        <span className="font-semibold">
                          {formatNumber(monster.total[column.key])}
                          {suffix}
                        </span>
                        {bonus > 0 && (
                          <span className="ml-1 text-xs text-emerald-400">
                            +{formatNumber(bonus)}
                            {suffix}
                          </span>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
            <p className="mt-3 text-xs text-muted-foreground">Sem bônus de líder, torres ou buffs.</p>
          </CardContent>
        </Card>

        <section aria-label="Runas" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {[1, 2, 3, 4, 5, 6].map((slot) => (
            <RuneCard key={slot} slot={slot} rune={runesBySlot.get(slot)} />
          ))}
        </section>
      </div>

      <section aria-label="Artefatos" className="grid gap-3 sm:grid-cols-2">
        {monster.artifacts.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhum artefato equipado.</p>
        ) : (
          monster.artifacts.map((artifact) => <ArtifactCard key={artifact.artifactId} artifact={artifact} />)
        )}
      </section>
    </div>
  )
}

function ArtifactCard({ artifact }: { artifact: ArtifactDetail }) {
  const kind =
    artifact.type === 'Element'
      ? `Elemento ${artifact.element ? ELEMENT_NAME_LABELS[artifact.element] : ''}`
      : `Tipo ${artifact.archetype ? ARCHETYPE_NAME_LABELS[artifact.archetype] : ''}`

  return (
    <Card size="sm" aria-label={`Artefato de ${kind.toLowerCase()}`}>
      <CardContent className="space-y-2 text-sm">
        <div className="flex items-baseline justify-between gap-2">
          <p className={cn('font-medium', RARITY_TEXT_CLASSES[artifact.originalRarity])}>
            Artefato de {kind.toLowerCase()} +{artifact.level}
          </p>
          <span className="text-xs text-muted-foreground">{RARITY_LABELS[artifact.originalRarity]}</span>
        </div>
        <p className="font-semibold">
          {ARTIFACT_MAIN_STAT_LABELS[artifact.mainStat.type] ?? 'Atributo'} +{formatNumber(artifact.mainStat.value)}
        </p>
        <ul className="space-y-0.5">
          {artifact.substats.map((sub) => (
            <li key={sub.type}>{formatArtifactEffect(sub.type, sub.value)}</li>
          ))}
        </ul>
      </CardContent>
    </Card>
  )
}
