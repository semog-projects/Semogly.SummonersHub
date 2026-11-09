import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getJson, postJson, request } from '@/lib/api'
import type { OptimizerPreset } from '../types'
import type { OptimizerForm } from './optimizerOptions'

const KEY = ['optimizer-presets']

/** Presets do otimizador do usuário (valem para qualquer invocador e dispositivo). */
export function useOptimizerPresets() {
  return useQuery({ queryKey: KEY, queryFn: ({ signal }) => getJson<OptimizerPreset[]>('/optimizer-presets', signal) })
}

export function useSaveOptimizerPreset() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ name, form }: { name: string; form: OptimizerForm }) =>
      postJson<OptimizerPreset>('/optimizer-presets', { name, criteria: form }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: KEY }),
  })
}

export function useDeleteOptimizerPreset() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => request<void>(`/optimizer-presets/${id}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: KEY }),
  })
}
