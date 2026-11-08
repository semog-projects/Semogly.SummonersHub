import { useQuery } from '@tanstack/react-query'
import { getJson } from '@/lib/api'
import type { AccountOverview, AccountSummary } from './types'

/** Invocadores do usuário logado, do import mais recente para o mais antigo. */
export function useAccounts() {
  return useQuery({
    queryKey: ['accounts'],
    queryFn: ({ signal }) => getJson<AccountSummary[]>('/accounts', signal),
  })
}

export function useAccountOverview(wizardId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'overview'],
    queryFn: ({ signal }) => getJson<AccountOverview>(`/accounts/${wizardId}/overview`, signal),
    enabled: Number.isSafeInteger(wizardId) && wizardId > 0,
  })
}
