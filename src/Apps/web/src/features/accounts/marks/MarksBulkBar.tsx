import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { formatNumber } from '@/lib/format'
import { cn } from '@/lib/utils'
import { controlClass } from '../shared/filterStyles'
import type { InventoryItemKind } from '../types'
import { MAX_TAG_LENGTH } from './markFilters'
import { useUpdateMarks } from './useUpdateMarks'

interface Props {
  wizardId: number
  kind: InventoryItemKind
  /** Itens filtrados que recebem a ação (todos, não só a página visível). */
  itemIds: number[]
}

/** Ações em lote sobre os itens filtrados: bloquear, desbloquear e adicionar/remover uma tag. */
export function MarksBulkBar({ wizardId, kind, itemIds }: Props) {
  const update = useUpdateMarks(wizardId)
  const [tag, setTag] = useState('')
  const count = formatNumber(itemIds.length)
  const disabled = itemIds.length === 0 || update.isPending
  const trimmed = tag.trim()

  return (
    <div role="group" aria-label="Ações nos itens filtrados" className="flex flex-wrap items-center gap-2 text-sm">
      <span className="text-xs text-muted-foreground">Nos {count} filtrados:</span>
      <Button variant="outline" size="sm" disabled={disabled} onClick={() => update.mutate({ kind, itemIds, locked: true })}>
        Bloquear
      </Button>
      <Button variant="outline" size="sm" disabled={disabled} onClick={() => update.mutate({ kind, itemIds, locked: false })}>
        Desbloquear
      </Button>
      <input
        type="text"
        aria-label="Tag"
        placeholder="Tag"
        maxLength={MAX_TAG_LENGTH}
        value={tag}
        onChange={(event) => setTag(event.target.value)}
        className={cn(controlClass, 'w-32')}
      />
      <Button
        variant="outline"
        size="sm"
        disabled={disabled || trimmed === ''}
        onClick={() => update.mutate({ kind, itemIds, addTags: [trimmed] }, { onSuccess: () => setTag('') })}
      >
        Adicionar tag
      </Button>
      <Button
        variant="ghost"
        size="sm"
        disabled={disabled || trimmed === ''}
        onClick={() => update.mutate({ kind, itemIds, removeTags: [trimmed] }, { onSuccess: () => setTag('') })}
      >
        Remover tag
      </Button>
      {update.isError && (
        <span role="alert" className="text-xs text-destructive">
          {update.error.message}
        </span>
      )}
    </div>
  )
}
