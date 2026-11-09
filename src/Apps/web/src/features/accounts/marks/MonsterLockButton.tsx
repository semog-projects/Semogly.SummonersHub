import { Lock, LockOpen } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useUpdateMarks } from './useUpdateMarks'

/** Bloqueia o monstro: as runas e artefatos equipados nele contam como bloqueados, em todos os imports. */
export function MonsterLockButton({ wizardId, monsterId, locked, withLabel = false }: {
  wizardId: number
  monsterId: number
  locked: boolean
  withLabel?: boolean
}) {
  const update = useUpdateMarks(wizardId)
  const Icon = locked ? Lock : LockOpen

  return (
    <button
      type="button"
      aria-pressed={locked}
      aria-label={locked ? 'Desbloquear monstro' : 'Bloquear monstro'}
      title={locked ? 'Monstro bloqueado: o otimizador não tira o equipamento dele' : 'Bloquear o equipamento deste monstro'}
      disabled={update.isPending}
      onClick={() => update.mutate({ kind: 'Monster', itemIds: [monsterId], locked: !locked })}
      className={cn(
        'inline-flex items-center gap-1 rounded p-1 text-xs transition-colors hover:bg-muted disabled:opacity-50',
        locked ? 'text-amber-400' : 'text-muted-foreground/60',
      )}
    >
      <Icon className="size-4" aria-hidden />
      {withLabel && (locked ? 'Bloqueado' : 'Bloquear')}
    </button>
  )
}
