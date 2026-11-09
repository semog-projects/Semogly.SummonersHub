import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getJson, postJson, request } from '@/lib/api'
import type { ShareLink, SharedBuild } from '../types'

const linksKey = (wizardId: number, monsterId: number) => ['accounts', wizardId, 'monsters', monsterId, 'shares']

export function useShareLinks(wizardId: number, monsterId: number) {
  return useQuery({
    queryKey: linksKey(wizardId, monsterId),
    queryFn: ({ signal }) => getJson<ShareLink[]>(`/accounts/${wizardId}/monsters/${monsterId}/shares`, signal),
  })
}

export function useCreateShareLink(wizardId: number, monsterId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => postJson<ShareLink>(`/accounts/${wizardId}/monsters/${monsterId}/shares`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: linksKey(wizardId, monsterId) }),
  })
}

export function useRevokeShareLink(wizardId: number, monsterId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (token: string) => request<void>(`/shares/${token}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: linksKey(wizardId, monsterId) }),
  })
}

/** Build compartilhada (público, sem login). */
export function useSharedBuild(token: string) {
  return useQuery({
    queryKey: ['shared', token],
    queryFn: ({ signal }) => getJson<SharedBuild>(`/shared/${token}`, signal),
    retry: false,
  })
}

/** Endereço público do link. */
export function shareUrl(token: string): string {
  return `${window.location.origin}/b/${token}`
}
