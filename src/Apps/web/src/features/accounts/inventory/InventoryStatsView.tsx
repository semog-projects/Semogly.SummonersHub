import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { formatNumber } from '@/lib/format'
import { RARITY_LABELS, RUNE_STATS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { DistributionCard } from '../overview/DistributionCard'
import { RARITY_BAR_CLASSES, RARITY_TEXT_CLASSES } from '../overview/elementStyles'
import type { CraftItemStats, InventoryStats, RuneSetStats, SpeedRuneEntry } from '../types'

const CRAFT_TYPE_LABELS: Record<CraftItemStats['type'], string> = {
  EnchantedGem: 'Gem',
  Grindstone: 'Grind',
  ImmemorialGem: 'Gem Immemorial',
  ImmemorialGrindstone: 'Grind Immemorial',
  AncientGem: 'Gem Ancient',
  AncientGrindstone: 'Grind Ancient',
}

const isGem = (item: CraftItemStats) => item.type.endsWith('Gem')

/** Indicadores do inventário: runas, artefatos, gems e grinds. */
export function InventoryStatsView({ inventory }: { inventory: InventoryStats }) {
  const { runes, artifacts, craft } = inventory

  const totals = [
    { label: 'Runas', value: runes.total, detail: `${runes.equipped} equipadas · ${runes.total - runes.equipped} no inventário` },
    { label: 'Runas +15', value: runes.maxLevel, detail: `${runes.ancient} ancient` },
    { label: 'Artefatos', value: artifacts.total, detail: `${artifacts.equipped} equipados · ${artifacts.maxLevel} em +15` },
    { label: 'Gems e grinds', value: craft.gems + craft.grinds, detail: `${craft.gems} gems · ${craft.grinds} grinds` },
  ]

  return (
    <div className="space-y-6">
      <dl className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {totals.map((item) => (
          <Card key={item.label} size="sm">
            <CardContent>
              <dt className="text-xs text-muted-foreground">{item.label}</dt>
              <dd className="text-2xl font-semibold tabular-nums">{formatNumber(item.value)}</dd>
              <dd className="text-xs text-muted-foreground">{item.detail}</dd>
            </CardContent>
          </Card>
        ))}
      </dl>

      <div className="grid gap-6 lg:grid-cols-[1fr_20rem]">
        <RuneSetsCard sets={runes.bySet} />
        <div className="space-y-6">
          <DistributionCard
            title="Runas por slot"
            items={runes.bySlot.map((item) => ({ label: `Slot ${item.key}`, count: item.count }))}
          />
          <DistributionCard
            title="Runas por raridade"
            items={[...runes.byRarity].reverse().map((item) => ({
              label: RARITY_LABELS[item.key],
              count: item.count,
              barClassName: RARITY_BAR_CLASSES[item.key],
            }))}
          />
          <DistributionCard
            title="Runas por estrelas"
            items={[...runes.byStars].reverse().map((item) => ({ label: `${item.key}★`, count: item.count }))}
          />
        </div>
      </div>

      <SpeedRunesCard entries={runes.topSpeed} />

      <div className="grid gap-6 md:grid-cols-2">
        <DistributionCard
          title="Artefatos por tipo"
          items={artifacts.byType.map((item) => ({
            label: item.key === 'Element' ? 'Elemento' : 'Tipo',
            count: item.count,
          }))}
        />
        <DistributionCard
          title="Artefatos por raridade"
          items={[...artifacts.byRarity].reverse().map((item) => ({
            label: RARITY_LABELS[item.key],
            count: item.count,
            barClassName: RARITY_BAR_CLASSES[item.key],
          }))}
        />
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <CraftCard title="Gems disponíveis" items={craft.items.filter(isGem)} />
        <CraftCard title="Grinds disponíveis" items={craft.items.filter((item) => !isGem(item))} />
      </div>
    </div>
  )
}

function RuneSetsCard({ sets }: { sets: RuneSetStats[] }) {
  const max = Math.max(1, ...sets.map((set) => set.total))

  return (
    <Card>
      <CardHeader>
        <CardTitle>Runas por set</CardTitle>
        <CardDescription>Barra escura: equipadas. Clara: no inventário.</CardDescription>
      </CardHeader>
      <CardContent className="overflow-x-auto">
        <table className="w-full text-sm" aria-label="Runas por set">
          <thead className="text-left text-xs text-muted-foreground">
            <tr className="border-b">
              <th scope="col" className="py-2 pr-3 font-medium">Set</th>
              <th scope="col" className="w-1/2 py-2 pr-3 font-medium" aria-label="Distribuição" />
              <th scope="col" className="py-2 pr-3 text-right font-medium">Total</th>
              <th scope="col" className="py-2 pr-3 text-right font-medium">Equipadas</th>
              <th scope="col" className="py-2 text-right font-medium">+15</th>
            </tr>
          </thead>
          <tbody>
            {sets.map((set) => (
              <tr key={set.set} className="border-b last:border-0">
                <th scope="row" className="py-1.5 pr-3 text-left font-medium">{set.set}</th>
                <td className="py-1.5 pr-3">
                  <span className="flex h-2 overflow-hidden rounded-full bg-muted" style={{ width: `${(set.total / max) * 100}%` }}>
                    <span className="h-full bg-primary" style={{ width: `${(set.equipped / set.total) * 100}%` }} />
                    <span className="h-full flex-1 bg-primary/30" />
                  </span>
                </td>
                <td className="py-1.5 pr-3 text-right tabular-nums">{set.total}</td>
                <td className="py-1.5 pr-3 text-right tabular-nums text-muted-foreground">{set.equipped}</td>
                <td className="py-1.5 text-right tabular-nums text-muted-foreground">{set.maxLevel}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </CardContent>
    </Card>
  )
}

function SpeedRunesCard({ entries }: { entries: SpeedRuneEntry[] }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Top runas por velocidade</CardTitle>
        <CardDescription>Velocidade nas subs (ou inata), já com o grind. O atributo principal não conta.</CardDescription>
      </CardHeader>
      <CardContent className="overflow-x-auto">
        {entries.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhuma runa com velocidade nas subs.</p>
        ) : (
          <table className="w-full text-sm" aria-label="Top runas por velocidade">
            <thead className="text-left text-xs text-muted-foreground">
              <tr className="border-b">
                <th scope="col" className="py-2 pr-3 font-medium">#</th>
                <th scope="col" className="py-2 pr-3 font-medium">Runa</th>
                <th scope="col" className="py-2 pr-3 text-right font-medium">VEL</th>
                <th scope="col" className="py-2 font-medium">Equipada em</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((entry, index) => (
                <tr key={entry.runeId} className="border-b last:border-0">
                  <td className="py-2 pr-3 tabular-nums text-muted-foreground">{index + 1}</td>
                  <td className="py-2 pr-3">
                    <span className={cn('font-medium', RARITY_TEXT_CLASSES[entry.originalRarity])}>
                      {entry.set} ({entry.slot}) +{entry.level}
                    </span>
                    <span className="ml-2 text-xs text-muted-foreground">
                      {entry.stars}★{entry.isAncient && ' · Ancient'}
                    </span>
                  </td>
                  <td className="py-2 pr-3 text-right whitespace-nowrap">
                    <span className="font-semibold tabular-nums">{entry.speed}</span>
                    {entry.grind > 0 && <span className="ml-1 text-xs text-amber-400">({entry.grind} grind)</span>}
                    {entry.isGemmed && <Badge variant="outline" className="ml-1 text-[10px]">gem</Badge>}
                    {entry.isInnate && <Badge variant="outline" className="ml-1 text-[10px]">inata</Badge>}
                  </td>
                  <td className="py-2">
                    {entry.equippedMonsterId === null ? (
                      <span className="text-muted-foreground">Inventário</span>
                    ) : (
                      <Link to={`../monstros/${entry.equippedMonsterId}`} relative="path" className="hover:underline">
                        {entry.equippedMonsterName ?? 'Monstro'}
                      </Link>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </CardContent>
    </Card>
  )
}

function CraftCard({ title, items }: { title: string; items: CraftItemStats[] }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent>
        {items.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhum item.</p>
        ) : (
          <ul className="divide-y text-sm" aria-label={title}>
            {items.map((item) => (
              <li
                key={`${item.type}-${item.set}-${item.stat}-${item.rarity}`}
                className="flex items-center justify-between gap-3 py-1.5"
              >
                <span>
                  <span className={cn('font-medium', RARITY_TEXT_CLASSES[item.rarity])}>
                    {item.set === 'Immemorial' ? 'Qualquer set' : item.set} · {statLabel(item.stat)}
                  </span>
                  {item.type !== 'EnchantedGem' && item.type !== 'Grindstone' && (
                    <Badge variant="secondary" className="ml-2">
                      {CRAFT_TYPE_LABELS[item.type]}
                    </Badge>
                  )}
                </span>
                <span className="tabular-nums text-muted-foreground">×{item.amount}</span>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

/** Rótulo do atributo de gem/grind: os percentuais de HP/ATK/DEF levam "%" para não confundir com os fixos. */
function statLabel(type: number): string {
  const stat = RUNE_STATS[type]
  if (!stat) return `Atributo ${type}`
  return [2, 4, 6].includes(type) ? `${stat.label} %` : stat.label
}
