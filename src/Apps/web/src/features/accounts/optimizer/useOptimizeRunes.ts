import { useMutation } from '@tanstack/react-query'
import { postJson } from '@/lib/api'
import type { OptimizeRunesRequest, OptimizeRunesResponse } from '../types'

/** Pede as builds ao otimizador; não altera nada na conta. */
export function useOptimizeRunes(wizardId: number, monsterId: number) {
  return useMutation({
    mutationFn: (request: OptimizeRunesRequest) =>
      postJson<OptimizeRunesResponse>(`/accounts/${wizardId}/monsters/${monsterId}/optimize`, request),
  })
}
