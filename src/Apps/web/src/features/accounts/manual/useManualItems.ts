import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getJson, postJson, request } from '@/lib/api'
import type { ElementName } from '@/lib/game/labels'

export interface ManualMonster {
  /** Id negativo: é o id do monstro nas rotas da conta. */
  gameId: number
  masterId: number
  name: string | null
  element: ElementName | null
  awakenLevel: number
  createdAt: string
}

export type ManualCraftType = 'EnchantedGem' | 'Grindstone'

export interface ManualCraftItem {
  gameId: number
  type: ManualCraftType
  set: string
  /** Código do atributo no jogo. */
  stat: number
  rarity: number
  amount: number
  createdAt: string
}

export interface ManualItems {
  monsters: ManualMonster[]
  craftItems: ManualCraftItem[]
}

/** Monstro do bestiário que dá para criar à mão. */
export interface MonsterSearchResult {
  com2usId: number
  name: string
  element: ElementName
  awakenLevel: number
  naturalStars: number
  imageUrl: string | null
}

export interface NewManualCraftItem {
  type: ManualCraftType
  set: string
  stat: number
  rarity: number
  amount: number
}

const itemsKey = (wizardId: number) => ['accounts', wizardId, 'manual-items']

export function useManualItems(wizardId: number) {
  return useQuery({
    queryKey: itemsKey(wizardId),
    queryFn: ({ signal }) => getJson<ManualItems>(`/accounts/${wizardId}/manual-items`, signal),
  })
}

/** Itens manuais mudam monstros, inventário e sugestões: recarrega tudo da conta. */
function useInvalidateAccount(wizardId: number) {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: ['accounts', wizardId] })
}

export function useCreateManualMonster(wizardId: number) {
  const invalidate = useInvalidateAccount(wizardId)
  return useMutation({
    mutationFn: (masterId: number) => postJson<ManualMonster>(`/accounts/${wizardId}/manual-items/monsters`, { masterId }),
    onSuccess: invalidate,
  })
}

export function useCreateManualCraftItem(wizardId: number) {
  const invalidate = useInvalidateAccount(wizardId)
  return useMutation({
    mutationFn: (item: NewManualCraftItem) => postJson<ManualCraftItem>(`/accounts/${wizardId}/manual-items/craft-items`, item),
    onSuccess: invalidate,
  })
}

export function useDeleteManualItem(wizardId: number) {
  const invalidate = useInvalidateAccount(wizardId)
  return useMutation({
    mutationFn: (gameId: number) => request<void>(`/accounts/${wizardId}/manual-items/${gameId}`, { method: 'DELETE' }),
    onSuccess: invalidate,
  })
}

/** Busca no bestiário a partir de 2 letras. */
export function useMonsterSearch(name: string) {
  const term = name.trim()
  return useQuery({
    queryKey: ['game-data', 'monster-search', term.toLowerCase()],
    queryFn: ({ signal }) => getJson<MonsterSearchResult[]>(`/game-data/monsters/search?name=${encodeURIComponent(term)}`, signal),
    enabled: term.length >= 2,
    staleTime: Infinity,
  })
}
