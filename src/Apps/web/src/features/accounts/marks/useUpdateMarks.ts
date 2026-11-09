import { useMutation, useQueryClient } from '@tanstack/react-query'
import { patchJson } from '@/lib/api'
import type { UpdateInventoryMarksRequest } from '../types'

/** Bloqueio e tags de runas e artefatos; recarrega os dados da conta ao terminar. */
export function useUpdateMarks(wizardId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateInventoryMarksRequest) => patchJson<void>(`/accounts/${wizardId}/inventory-marks`, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['accounts', wizardId] }),
  })
}
