import { EQUIPPABLE_RUNE_SETS } from '@/lib/game/labels'
import type { ManualCraftType } from './useManualItems'

/** Atributos que aceitam grind (HP, ATK, DEF, em valor e %, e VEL). */
export const GRINDABLE_STATS = [1, 2, 3, 4, 5, 6, 8]

/** Atributos que uma gem pode dar. */
export const GEM_STATS = [1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 12]

/** Sets de gems e grinds (Intangible não tem). */
export const CRAFT_SETS = EQUIPPABLE_RUNE_SETS.filter((set) => set !== 'Intangible')

export const CRAFT_TYPE_OPTIONS: Array<{ value: ManualCraftType; label: string }> = [
  { value: 'EnchantedGem', label: 'Gem' },
  { value: 'Grindstone', label: 'Grind' },
]

export function statsFor(type: ManualCraftType): number[] {
  return type === 'Grindstone' ? GRINDABLE_STATS : GEM_STATS
}
