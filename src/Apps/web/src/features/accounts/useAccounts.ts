import { useQuery } from '@tanstack/react-query'
import { getJson } from '@/lib/api'
import type {
  AccountOverview,
  AccountSummary,
  ArtifactListItem,
  InventoryStats,
  MonsterDetail,
  MonsterListItem,
  PveTeamCheck,
  RuneListItem,
  SnapshotComparison,
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

/** Todas as versões (imports) do invocador, da mais recente para a mais antiga. */
export function useSnapshots(wizardId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'snapshots'],
    queryFn: ({ signal }) => getJson<AccountSummary[]>(`/accounts/${wizardId}/snapshots`, signal),
    enabled: Number.isSafeInteger(wizardId) && wizardId > 0,
  })
}

export function useSnapshotComparison(wizardId: number, from: number | null, to: number | null) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'snapshots', 'compare', from, to],
    queryFn: ({ signal }) => getJson<SnapshotComparison>(`/accounts/${wizardId}/snapshots/compare?from=${from}&to=${to}`, signal),
    enabled: Number.isSafeInteger(wizardId) && from !== null && to !== null && from !== to,
  })
}

/** Times de referência das dungeons conferidos contra a conta. */
export function usePveTeams(wizardId: number) {
  return useQuery({
    queryKey: ['accounts', wizardId, 'pve-teams'],
    queryFn: ({ signal }) => getJson<PveTeamCheck[]>(`/accounts/${wizardId}/pve-teams`, signal),
    enabled: Number.isSafeInteger(wizardId) && wizardId > 0,
  })
}
