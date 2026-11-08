import { Card, CardContent } from '@/components/ui/card'
import { formatDateTime, formatNumber } from '@/lib/format'
import { AWAKEN_LABELS, ELEMENT_NAME_LABELS } from '@/lib/game/labels'
import type { AccountOverview as Overview } from '../types'
import { DistributionCard } from './DistributionCard'
import { ELEMENT_BAR_CLASSES } from './elementStyles'
import { SpeedRankingCard } from './SpeedRankingCard'

/** Visão geral da conta: invocador, totais, distribuição dos monstros e ranking de velocidade. */
export function AccountOverview({ overview }: { overview: Overview }) {
  const { wizard } = overview

  const totals = [
    { label: 'Monstros', value: overview.monsters },
    { label: 'Runas', value: overview.runes },
    { label: 'Artefatos', value: overview.artifacts },
  ]

  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="flex flex-wrap items-center justify-between gap-6">
          <div>
            <h1 className="font-heading text-2xl font-semibold">{wizard.name}</h1>
            <p className="text-sm text-muted-foreground">
              Nível {wizard.level}
              {wizard.serverId !== null && ` · servidor ${wizard.serverId}`}
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              Último import em {formatDateTime(wizard.importedAt)} (versão {wizard.version})
              {wizard.exportedAt && ` · exportado em ${formatDateTime(wizard.exportedAt)}`}
            </p>
          </div>

          <dl className="flex gap-6">
            {totals.map((item) => (
              <div key={item.label} className="text-right">
                <dt className="text-xs text-muted-foreground">{item.label}</dt>
                <dd className="text-2xl font-semibold tabular-nums">{formatNumber(item.value)}</dd>
              </div>
            ))}
          </dl>
        </CardContent>
      </Card>

      <div className="grid gap-6 md:grid-cols-3">
        <DistributionCard
          title="Por estrelas"
          items={[...overview.monstersByStars]
            .sort((a, b) => b.key - a.key)
            .map((item) => ({ label: `${item.key}★`, count: item.count }))}
        />
        <DistributionCard
          title="Por elemento"
          items={overview.monstersByElement.map((item) => ({
            label: ELEMENT_NAME_LABELS[item.key],
            count: item.count,
            barClassName: ELEMENT_BAR_CLASSES[item.key],
          }))}
        />
        <DistributionCard
          title="Por despertar"
          items={overview.monstersByAwakening.map((item) => ({ label: AWAKEN_LABELS[item.key], count: item.count }))}
        />
      </div>

      <SpeedRankingCard entries={overview.speedRanking} />
    </div>
  )
}
