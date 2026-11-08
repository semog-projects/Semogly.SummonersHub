import { useQuery } from '@tanstack/react-query'
import { getJson } from '@/lib/api'

export interface ApiInfo {
  name: string
  version: string
  environment: string
}

/** Consulta /api/info: usado para mostrar se a API está acessível. */
export function useApiInfo() {
  return useQuery({
    queryKey: ['system', 'info'],
    queryFn: ({ signal }) => getJson<ApiInfo>('/info', signal),
    retry: false,
  })
}
