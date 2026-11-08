import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useMonsterDefinitions } from '@/features/game-data/useMonsterDefinitions'
import { formatNumber } from '@/lib/format'
import { AWAKEN_LABELS, ELEMENT_NAME_LABELS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { MonsterIcon } from '../monsters/MonsterIcon'
import { ELEMENT_TEXT_CLASSES } from '../overview/elementStyles'
import { RuneCard } from '../runes/RuneCard'
import type { EquipmentChanges, MonsterChangeItem, SnapshotComparison } from '../types'
import { Delta } from './Delta'

interface Props {
  wizardId: number
  comparison: SnapshotComparison
}

export function SnapshotComparisonView({ wizardId, comparison }: Props) {
  const masterIds = [
    ...comparison.addedMonsters,
    ...comparison.removedMonsters,
    ...comparison.progressedMonsters,
  ].map((m) => m.masterId)
  const { data: definitions } = useMonsterDefinitions(masterIds)
  const iconOf = (masterId: number) => definitions?.get(masterId)?.imageUrl

  return (
    <div className="space-y-6">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <SummaryCard label="Nível do invocador">
          <Delta change={comparison.wizardLevel} />
        </SummaryCard>
        <SummaryCard label="Monstros">
          <Delta change={comparison.monsters} />
        </SummaryCard>
        <SummaryCard label="Runas">
          <EquipmentSummary changes={comparison.runes} />
        </SummaryCard>
        <SummaryCard label="Artefatos">
          <EquipmentSummary changes={comparison.artifacts} />
        </SummaryCard>
        <SummaryCard label="Eficiência média das runas">
          <Delta change={comparison.averageRuneEfficiency} percent />
        </SummaryCard>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <MonsterList title="Monstros novos" monsters={comparison.addedMonsters} wizardId={wizardId} iconOf={iconOf} linked />
        <MonsterList title="Monstros que saíram" monsters={comparison.removedMonsters} wizardId={wizardId} iconOf={iconOf} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Evolução</CardTitle>
        </CardHeader>
        <CardContent>
          {comparison.progressedMonsters.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhum monstro mudou de estrelas, nível, despertar ou velocidade.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm" aria-label="Evolução dos monstros">
                <thead className="text-left text-xs text-muted-foreground">
                  <tr className="border-b">
                    <th scope="col" className="py-2 pr-3 font-medium">
                      Monstro
                    </th>
                    <th scope="col" className="py-2 pr-3 text-right font-medium">
                      Estrelas
                    </th>
                    <th scope="col" className="py-2 pr-3 text-right font-medium">
                      Nível
                    </th>
                    <th scope="col" className="py-2 pr-3 font-medium">
                      Despertar
                    </th>
                    <th scope="col" className="py-2 pr-3 text-right font-medium">
                      VEL
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {comparison.progressedMonsters.map((monster) => (
                    <tr key={monster.monsterId} className="border-b last:border-0">
                      <td className="py-2 pr-3">
                        <MonsterName monster={monster} wizardId={wizardId} imageUrl={iconOf(monster.masterId)} linked />
                      </td>
                      <td className="py-2 pr-3 text-right">
                        <Delta change={monster.stars} />
                      </td>
                      <td className="py-2 pr-3 text-right">
                        <Delta change={monster.level} />
                      </td>
                      <td className="py-2 pr-3 whitespace-nowrap">
                        {awakenLabel(monster.awakenLevel.from) === awakenLabel(monster.awakenLevel.to) ? (
                          awakenLabel(monster.awakenLevel.to)
                        ) : (
                          <>
                            <span className="text-muted-foreground">{awakenLabel(monster.awakenLevel.from)} → </span>
                            <span className="text-emerald-400">{awakenLabel(monster.awakenLevel.to)}</span>
                          </>
                        )}
                      </td>
                      <td className="py-2 pr-3 text-right">
                        <Delta change={monster.speed} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <section aria-label="Melhores runas novas" className="space-y-3">
        <h2 className="font-heading text-lg font-semibold">Melhores runas novas</h2>
        {comparison.topNewRunes.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhuma runa nova entre as duas versões.</p>
        ) : (
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {comparison.topNewRunes.map((rune) => (
              <RuneCard key={rune.runeId} slot={rune.slot} rune={rune} />
            ))}
          </div>
        )}
      </section>
    </div>
  )
}

function SummaryCard({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Card size="sm">
      <CardContent className="space-y-1">
        <p className="text-xs text-muted-foreground">{label}</p>
        <div className="text-lg font-semibold">{children}</div>
      </CardContent>
    </Card>
  )
}

function EquipmentSummary({ changes }: { changes: EquipmentChanges }) {
  return (
    <div className="space-y-1">
      <Delta change={changes.total} />
      <p className="text-xs font-normal text-muted-foreground">
        +{formatNumber(changes.added)} novas · −{formatNumber(changes.removed)} saíram · {formatNumber(changes.upgraded)}{' '}
        evoluíram
      </p>
    </div>
  )
}

function MonsterList({
  title,
  monsters,
  wizardId,
  iconOf,
  linked = false,
}: {
  title: string
  monsters: MonsterChangeItem[]
  wizardId: number
  iconOf: (masterId: number) => string | null | undefined
  /** Só monstros que ainda existem no último import têm página de detalhe. */
  linked?: boolean
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>
          {title} <span className="text-sm font-normal text-muted-foreground">({monsters.length})</span>
        </CardTitle>
      </CardHeader>
      <CardContent>
        {monsters.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhum.</p>
        ) : (
          <ul className="space-y-2" aria-label={title}>
            {monsters.map((monster) => (
              <li key={monster.monsterId}>
                <MonsterName
                  monster={monster}
                  wizardId={wizardId}
                  imageUrl={iconOf(monster.masterId)}
                  details={`${monster.stars}★ · nv. ${monster.level}`}
                  linked={linked}
                />
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

function MonsterName({
  monster,
  wizardId,
  imageUrl,
  details,
  linked,
}: {
  monster: Pick<MonsterChangeItem, 'monsterId' | 'masterId' | 'name' | 'element'>
  wizardId: number
  imageUrl?: string | null
  /** Texto depois do elemento, ex.: "6★ · nv. 40". */
  details?: string
  linked: boolean
}) {
  const name = monster.name ?? `Monstro ${monster.masterId}`
  return (
    <div className="flex items-center gap-2">
      <MonsterIcon imageUrl={imageUrl} />
      <div className="min-w-0">
        {linked ? (
          <Link to={`/contas/${wizardId}/monstros/${monster.monsterId}`} className="font-medium hover:underline">
            {name}
          </Link>
        ) : (
          <span className="font-medium">{name}</span>
        )}
        <p className="text-xs whitespace-nowrap text-muted-foreground">
          <span className={cn(ELEMENT_TEXT_CLASSES[monster.element])}>{ELEMENT_NAME_LABELS[monster.element]}</span>
          {details && ` · ${details}`}
        </p>
      </div>
    </div>
  )
}

function awakenLabel(level: number | null): string {
  return level === null ? '—' : (AWAKEN_LABELS[level] ?? '—')
}
