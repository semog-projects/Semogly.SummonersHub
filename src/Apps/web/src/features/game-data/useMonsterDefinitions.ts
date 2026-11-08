import { useQuery } from '@tanstack/react-query'
import { getJson } from '@/lib/api'

export interface MonsterDefinition {
  com2usId: number
  familyId: number
  name: string
  element: 'Water' | 'Fire' | 'Wind' | 'Light' | 'Dark' | 'Pure'
  archetype: 'None' | 'Attack' | 'Defense' | 'Hp' | 'Support' | 'Material'
  naturalStars: number
  awakenLevel: number
  isHomunculus: boolean
  imageUrl: string | null
}

/** Mesmo limite do GetMonstersQuery na API. */
const MAX_IDS_PER_REQUEST = 500

/**
 * Busca no bestiário os monstros informados (pelo unit_master_id do export) e devolve um mapa por id.
 * Dado estático: fica em cache durante toda a sessão.
 */
export function useMonsterDefinitions(com2usIds: number[]) {
  const ids = [...new Set(com2usIds)].sort((a, b) => a - b)

  return useQuery({
    queryKey: ['game-data', 'monsters', ids],
    enabled: ids.length > 0,
    staleTime: Infinity,
    queryFn: async ({ signal }) => {
      const batches = chunk(ids, MAX_IDS_PER_REQUEST).map((batch) =>
        getJson<MonsterDefinition[]>(`/game-data/monsters?${batch.map((id) => `ids=${id}`).join('&')}`, signal),
      )
      const monsters = (await Promise.all(batches)).flat()
      return new Map(monsters.map((monster) => [monster.com2usId, monster]))
    },
  })
}

function chunk<T>(items: T[], size: number): T[][] {
  const chunks: T[][] = []
  for (let i = 0; i < items.length; i += size) chunks.push(items.slice(i, i + size))
  return chunks
}
