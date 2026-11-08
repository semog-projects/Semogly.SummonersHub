import { RUNE_STATS } from '@/lib/game/labels'
import type { CraftItemStats, CraftSuggestionGroup } from '../types'

export type CraftItemType = CraftItemStats['type']

export const CRAFT_TYPE_LABELS: Record<CraftItemType, string> = {
  EnchantedGem: 'Gem',
  Grindstone: 'Grind',
  ImmemorialGem: 'Gem Immemorial',
  ImmemorialGrindstone: 'Grind Immemorial',
  AncientGem: 'Gem Ancient',
  AncientGrindstone: 'Grind Ancient',
}

export const isGemType = (type: CraftItemType) => type.endsWith('Gem')

/** Rótulo do atributo com "%" separado, ex.: "ATK %", "VEL". */
export function craftStatLabel(type: number): string {
  const stat = RUNE_STATS[type]
  if (!stat) return `Atributo ${type}`
  return [2, 4, 6].includes(type) ? `${stat.label} %` : stat.label
}

/** "Grind Vampire VEL", "Gem qualquer set ATK %". */
export function craftGroupTitle(group: CraftSuggestionGroup): string {
  const set = group.set === 'Immemorial' ? 'qualquer set' : group.set
  return `${CRAFT_TYPE_LABELS[group.type]} ${set} ${craftStatLabel(group.stat)}`
}
