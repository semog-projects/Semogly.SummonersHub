import { Check, CircleAlert, CircleDashed } from 'lucide-react'
import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ELEMENT_NAME_LABELS } from '@/lib/game/labels'
import { cn } from '@/lib/utils'
import { MonsterIcon } from '../monsters/MonsterIcon'
import { ELEMENT_TEXT_CLASSES } from '../overview/elementStyles'
import type { PveSlotCheck, PveTeamCheck } from '../types'
import { formatPveStat } from './pveLabels'

interface Props {
  wizardId: number
  team: PveTeamCheck
  /** Ícone do bestiário por masterId (só para os monstros que a conta tem). */
  iconOf: (masterId: number) => string | null | undefined
}

export function PveTeamCard({ wizardId, team, iconOf }: Props) {
  return (
    <Card aria-label={`Time ${team.name}`}>
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <CardTitle>{team.name}</CardTitle>
          {team.isReady ? (
            <Badge>Pronto</Badge>
          ) : (
            <Badge variant="outline">
              {team.readySlots} de {team.slots.length} prontos
            </Badge>
          )}
        </div>
        {team.notes && <p className="text-xs text-muted-foreground">{team.notes}</p>}
      </CardHeader>
      <CardContent>
        <ul className="space-y-3">
          {team.slots.map((slot, i) => (
            <SlotRow key={`${slot.monsterName}-${i}`} wizardId={wizardId} slot={slot} imageUrl={slot.monster && iconOf(slot.monster.masterId)} />
          ))}
        </ul>
      </CardContent>
    </Card>
  )
}

function SlotRow({ wizardId, slot, imageUrl }: { wizardId: number; slot: PveSlotCheck; imageUrl?: string | null }) {
  const label = `${slot.monsterName} (${ELEMENT_NAME_LABELS[slot.element]})`

  return (
    <li className="flex items-start gap-3 text-sm" aria-label={label}>
      <MonsterIcon imageUrl={imageUrl} className={cn(slot.status === 'Missing' && 'opacity-40')} />
      <div className="min-w-0 flex-1">
        <p>
          {slot.monster ? (
            <Link to={`/contas/${wizardId}/monstros/${slot.monster.monsterId}`} className="font-medium hover:underline">
              {slot.monsterName}
            </Link>
          ) : (
            <span className="font-medium text-muted-foreground">{slot.monsterName}</span>
          )}{' '}
          <span className={cn('text-xs', ELEMENT_TEXT_CLASSES[slot.element])}>{ELEMENT_NAME_LABELS[slot.element]}</span>
          {slot.monster && (
            <span className="text-xs text-muted-foreground">
              {' '}
              · {slot.monster.stars}★ · nv. {slot.monster.level}
            </span>
          )}
        </p>
        <p className="text-xs text-muted-foreground">
          {slot.role}
          {slot.requirements.length > 0 &&
            ` · mín. ${slot.requirements.map((r) => formatPveStat(r.stat, r.min)).join(', ')}`}
        </p>
        {slot.status === 'BelowRequirements' && (
          <p className="text-xs text-amber-400">
            Falta:{' '}
            {slot.unmet.map((u) => `${formatPveStat(u.stat, u.current)} de ${formatPveStat(u.stat, u.required)}`).join(', ')}
          </p>
        )}
      </div>
      <SlotStatus status={slot.status} />
    </li>
  )
}

function SlotStatus({ status }: { status: PveSlotCheck['status'] }) {
  switch (status) {
    case 'Ready':
      return (
        <span className="inline-flex items-center gap-1 text-xs text-emerald-400">
          <Check className="size-4" aria-hidden /> Pronto
        </span>
      )
    case 'BelowRequirements':
      return (
        <span className="inline-flex items-center gap-1 text-xs text-amber-400">
          <CircleAlert className="size-4" aria-hidden /> Abaixo
        </span>
      )
    case 'Missing':
      return (
        <span className="inline-flex items-center gap-1 text-xs whitespace-nowrap text-muted-foreground">
          <CircleDashed className="size-4" aria-hidden /> Não tem
        </span>
      )
  }
}
