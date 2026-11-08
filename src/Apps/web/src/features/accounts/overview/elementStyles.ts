import type { ElementName } from '@/lib/game/labels'

/** Cores dos elementos, próximas das usadas no jogo. */
export const ELEMENT_BAR_CLASSES: Record<ElementName, string> = {
  Water: 'bg-sky-500',
  Fire: 'bg-orange-500',
  Wind: 'bg-amber-400',
  Light: 'bg-yellow-100',
  Dark: 'bg-violet-500',
  Pure: 'bg-emerald-500',
}

export const ELEMENT_TEXT_CLASSES: Record<ElementName, string> = {
  Water: 'text-sky-400',
  Fire: 'text-orange-400',
  Wind: 'text-amber-300',
  Light: 'text-yellow-100',
  Dark: 'text-violet-400',
  Pure: 'text-emerald-400',
}

/** Cor da raridade como no jogo: comum, mágica, rara, heroica e lendária. */
export const RARITY_TEXT_CLASSES: Record<number, string> = {
  1: 'text-muted-foreground',
  2: 'text-emerald-400',
  3: 'text-sky-400',
  4: 'text-fuchsia-400',
  5: 'text-orange-400',
}

export const RARITY_BAR_CLASSES: Record<number, string> = {
  1: 'bg-zinc-400',
  2: 'bg-emerald-500',
  3: 'bg-sky-500',
  4: 'bg-fuchsia-500',
  5: 'bg-orange-500',
}
