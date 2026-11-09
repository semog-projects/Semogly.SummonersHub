import { Lock, LockOpen } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { InventoryItemKind } from '../types'
import { useUpdateMarks } from './useUpdateMarks'

interface Props {
  wizardId: number
  kind: InventoryItemKind
  itemId: number
  locked: boolean
  tags: string[]
}

/** Cadeado (alterna o bloqueio) e as tags do item, na linha da tabela. */
export function MarkCell({ wizardId, kind, itemId, locked, tags }: Props) {
  const update = useUpdateMarks(wizardId)
  const item = kind === 'Rune' ? 'runa' : 'artefato'
  const Icon = locked ? Lock : LockOpen

  return (
    <div className="flex flex-wrap items-center gap-1">
      <button
        type="button"
        aria-pressed={locked}
        aria-label={locked ? `Desbloquear ${item}` : `Bloquear ${item}`}
        title={locked ? 'Bloqueado: o otimizador não tira de quem usa' : 'Bloquear'}
        disabled={update.isPending}
        onClick={() => update.mutate({ kind, itemIds: [itemId], locked: !locked })}
        className={cn(
          'rounded p-1 transition-colors hover:bg-muted disabled:opacity-50',
          locked ? 'text-amber-400' : 'text-muted-foreground/60',
        )}
      >
        <Icon className="size-4" aria-hidden />
      </button>
      {tags.map((tag) => (
        <Badge key={tag} variant="outline" className="text-[10px]">
          {tag}
        </Badge>
      ))}
    </div>
  )
}
