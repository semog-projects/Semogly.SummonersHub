import { useMutation, useQueryClient } from '@tanstack/react-query'
import { postJson } from '@/lib/api'
import type { AccountImport } from './types'

export interface ImportAccountResult {
  snapshotId: string
  wizardId: number
  version: number
  /** Falso quando o mesmo conteúdo já tinha sido importado. */
  created: boolean
  monsters: number
  runes: number
  artifacts: number
  craftItems: number
}

/** Envia o payload sanitizado para a API (POST /api/imports). */
export function useImportAccount() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: AccountImport) => postJson<ImportAccountResult>('/imports', payload),
    onSuccess: (result) => queryClient.invalidateQueries({ queryKey: ['accounts', result.wizardId] }),
  })
}
