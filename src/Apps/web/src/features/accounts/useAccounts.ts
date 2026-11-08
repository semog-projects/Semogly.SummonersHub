import { useQuery } from '@tanstack/react-query'
import { getJson } from '@/lib/api'
import type {
  AccountOverview,
  AccountSummary,
  ArtifactListItem,
  InventoryStats,
  MonsterDetail,
  MonsterListItem,
  RuneListItem,
} from './types'

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

export function useMonsters(wizardId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'monsters'],
    queryFn: ({ signal }) => getJson<MonsterListItem[]>(`/accounts/${wizardId}/monsters`, signal),
    enabled: Number.isSafeInteger(wizardId) && wizardId > 0,
  })
}

export function useMonsterDetail(wizardId: number, monsterId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'monsters', monsterId],
    queryFn: ({ signal }) => getJson<MonsterDetail>(`/accounts/${wizardId}/monsters/${monsterId}`, signal),
    enabled: Number.isSafeInteger(wizardId) && Number.isSafeInteger(monsterId),
  })
}

export function useInventoryStats(wizardId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'inventory'],
    queryFn: ({ signal }) => getJson<InventoryStats>(`/accounts/${wizardId}/inventory`, signal),
    enabled: Number.isSafeInteger(wizardId) && wizardId > 0,
  })
}

export function useRunes(wizardId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'runes'],
    queryFn: ({ signal }) => getJson<RuneListItem[]>(`/accounts/${wizardId}/runes`, signal),
    enabled: Number.isSafeInteger(wizardId) && wizardId > 0,
  })
}

export function useArtifacts(wizardId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'artifacts'],
    queryFn: ({ signal }) => getJson<ArtifactListItem[]>(`/accounts/${wizardId}/artifacts`, signal),
    enabled: Number.isSafeInteger(wizardId) && wizardId > 0,
  })
}
