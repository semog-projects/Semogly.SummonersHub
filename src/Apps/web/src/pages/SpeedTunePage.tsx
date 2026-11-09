import { useQueries, useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useParams } from 'react-router'
import { Card, CardContent } from '@/components/ui/card'
import { DUNGEON_LABELS } from '@/features/accounts/pve/pveLabels'
import { SelectFilter } from '@/features/accounts/shared/filterControls'
import { controlClass } from '@/features/accounts/shared/filterStyles'
import type { DungeonName, MonsterDetail, MonsterListItem } from '@/features/accounts/types'
import { useMonsters } from '@/features/accounts/useAccounts'
import { getJson } from '@/lib/api'
import { formatNumber } from '@/lib/format'
import { combatSpeed, simulateTurns, ticksToAct, type SpeedUnit } from '@/lib/game/speedTune'
import { cn } from '@/lib/utils'
import { QueryStatus } from './QueryStatus'

const TEAM_SIZE = 5

/** Quantas ações a simulação mostra. */
const TURNS = 10

interface DungeonStats {
  dungeon: DungeonName
  floor: number
  bossSpeed: number
  maxSpeed: number
}

const ENEMY_ID = 'inimigo'

export function SpeedTunePage() {
  const wizardId = Number(useParams().wizardId)
  const monsters = useMonsters(wizardId)

  return (
    <QueryStatus query={monsters} notFound="Conta não encontrada." loading="Carregando monstros…">
      {(list) => <SpeedTune wizardId={wizardId} monsters={list} />}
    </QueryStatus>
  )
}

function SpeedTune({ wizardId, monsters }: { wizardId: number; monsters: MonsterListItem[] }) {
  const [team, setTeam] = useState<Array<number | null>>(Array(TEAM_SIZE).fill(null))
  const [startAtb, setStartAtb] = useState<Record<number, string>>({})
  const [leader, setLeader] = useState('0')
  const [tower, setTower] = useState('15')
  const [dungeon, setDungeon] = useState<DungeonName | null>(null)

  const dungeons = useQuery({
    queryKey: ['game-data', 'dungeons'],
    queryFn: ({ signal }) => getJson<DungeonStats[]>('/game-data/dungeons', signal),
    staleTime: Number.POSITIVE_INFINITY,
  })

  // VEL base vem do detalhe (mesma chave de cache da página do monstro)
  const selectedIds = team.filter((id): id is number => id !== null)
  const details = useQueries({
    queries: selectedIds.map((monsterId) => ({
      queryKey: ['accounts', wizardId, 'monsters', monsterId],
      queryFn: ({ signal }: { signal: AbortSignal }) => getJson<MonsterDetail>(`/accounts/${wizardId}/monsters/${monsterId}`, signal),
    })),
  })
  const detailById = new Map(details.flatMap((q) => (q.data ? [[q.data.monsterId, q.data] as const] : [])))

  const options = useMemo(
    () =>
      monsters
        .filter((m) => m.archetype !== 'Material')
        .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '', 'pt-BR') || b.total.speed - a.total.speed)
        .map((m) => ({ value: m.monsterId, label: `${m.name ?? `Monstro ${m.masterId}`} · ${m.stars}★ · VEL ${m.total.speed}` })),
    [monsters],
  )

  const leaderPercent = Math.max(0, Number(leader) || 0)
  const towerPercent = Math.max(0, Number(tower) || 0)
  const enemy = dungeons.data?.find((d) => d.dungeon === dungeon)

  const rows = selectedIds.flatMap((monsterId) => {
    const detail = detailById.get(monsterId)
    if (!detail) return []
    const speed = combatSpeed(detail.base.speed, detail.total.speed, leaderPercent, towerPercent)
    const atb = Math.min(100, Math.max(0, Number(startAtb[monsterId]) || 0))
    return [{ detail, speed, atb, ticks: ticksToAct(speed, atb) }]
  })

  const units: SpeedUnit[] = [
    ...rows.map((r) => ({ id: String(r.detail.monsterId), name: r.detail.name ?? `Monstro ${r.detail.masterId}`, speed: r.speed, startAtb: r.atb })),
    ...(enemy ? [{ id: ENEMY_ID, name: `Inimigo mais rápido (VEL ${enemy.maxSpeed})`, speed: enemy.maxSpeed, startAtb: 0 }] : []),
  ]
  const turns = simulateTurns(units, TURNS)
  const nameOf = new Map(units.map((u) => [u.id, u.name]))
  const enemyTicks = enemy ? ticksToAct(enemy.maxSpeed) : null

  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="space-y-4">
          <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
            {team.map((monsterId, i) => (
              <SelectFilter
                key={i}
                label={`Monstro ${i + 1}`}
                allLabel={`Monstro ${i + 1}`}
                value={monsterId}
                options={options}
                onChange={(value) => setTeam(team.map((id, j) => (j === i ? value : id)))}
              />
            ))}
          </div>
          <div className="flex flex-wrap items-end gap-4 text-sm">
            <NumberField label="Líder de VEL (%)" value={leader} onChange={setLeader} />
            <NumberField label="Torre/flag de VEL (%)" value={tower} onChange={setTower} />
            <SelectFilter
              label="Dungeon"
              allLabel="Comparar com uma dungeon"
              value={dungeon}
              options={(dungeons.data ?? []).map((d) => ({ value: d.dungeon, label: `${DUNGEON_LABELS[d.dungeon]} B${d.floor}` }))}
              onChange={setDungeon}
            />
          </div>
        </CardContent>
      </Card>

      {rows.length === 0 ? (
        <p className="text-sm text-muted-foreground">Escolha os monstros do time para ver a ordem de turnos.</p>
      ) : (
        <>
          <div className="overflow-x-auto">
            <table className="w-full text-sm" aria-label="Velocidade do time">
              <thead className="text-left text-xs text-muted-foreground">
                <tr className="border-b">
                  <th scope="col" className="py-2 pr-3 font-medium">Monstro</th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">VEL base</th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">VEL total</th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">VEL de combate</th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">Barra inicial (%)</th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">Ticks até agir</th>
                  {enemy && <th scope="col" className="py-2 pr-3 font-medium">Contra o inimigo</th>}
                </tr>
              </thead>
              <tbody>
                {[...rows]
                  .sort((a, b) => a.ticks - b.ticks || b.speed - a.speed)
                  .map(({ detail, speed, ticks }) => (
                    <tr key={detail.monsterId} className="border-b last:border-0">
                      <th scope="row" className="py-2 pr-3 text-left font-medium">
                        {detail.name ?? `Monstro ${detail.masterId}`}
                      </th>
                      <td className="py-2 pr-3 text-right tabular-nums text-muted-foreground">{detail.base.speed}</td>
                      <td className="py-2 pr-3 text-right tabular-nums">{detail.total.speed}</td>
                      <td className="py-2 pr-3 text-right font-semibold tabular-nums">{formatNumber(speed)}</td>
                      <td className="py-2 pr-3 text-right">
                        <input
                          type="number"
                          min={0}
                          max={100}
                          inputMode="numeric"
                          aria-label={`Barra inicial de ${detail.name ?? detail.monsterId}`}
                          value={startAtb[detail.monsterId] ?? '0'}
                          onChange={(event) => setStartAtb({ ...startAtb, [detail.monsterId]: event.target.value })}
                          className={cn(controlClass, 'w-16 text-right')}
                        />
                      </td>
                      <td className="py-2 pr-3 text-right tabular-nums">{ticks}</td>
                      {enemyTicks !== null && (
                        <td className={cn('py-2 pr-3 text-xs', ticks < enemyTicks ? 'text-emerald-400' : 'text-amber-400')}>
                          {ticks < enemyTicks ? 'Age antes' : ticks === enemyTicks ? 'Empata no tick' : 'Age depois'}
                        </td>
                      )}
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>

          <section aria-label="Primeiros turnos" className="space-y-2">
            <h2 className="font-heading text-lg font-semibold">Primeiros {TURNS} turnos</h2>
            <ol className="space-y-1 text-sm">
              {turns.map((turn, i) => (
                <li key={i} className={cn(turn.unitId === ENEMY_ID && 'text-amber-400')}>
                  <span className="text-muted-foreground tabular-nums">{i + 1}. tick {turn.tick} · </span>
                  {nameOf.get(turn.unitId)}
                </li>
              ))}
            </ol>
          </section>
        </>
      )}

      <p className="text-xs text-muted-foreground">
        VEL de combate = VEL total + (líder + torre/flag)% da VEL base. A cada tick a barra de ataque sobe 7% da VEL de
        combate; quem chega a 100% age. Fora do cálculo: ganhos e reduções de barra durante a luta, buffs e debuffs de VEL.
      </p>
    </div>
  )
}

function NumberField({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <label className="space-y-1">
      <span className="block text-xs text-muted-foreground">{label}</span>
      <input
        type="number"
        min={0}
        inputMode="numeric"
        aria-label={label}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className={cn(controlClass, 'w-24')}
      />
    </label>
  )
}
