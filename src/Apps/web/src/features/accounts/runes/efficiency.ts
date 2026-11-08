import type { RuneDetail } from '../types'

/**
 * Cor da eficiência na escala de raridade do jogo, só como apoio visual:
 * 100%+ lendária, 85%+ heroica, 70%+ rara, 55%+ mágica e abaixo disso comum.
 */
export function efficiencyClass(efficiency: number): string {
  if (efficiency >= 100) return 'text-orange-400'
  if (efficiency >= 85) return 'text-fuchsia-400'
  if (efficiency >= 70) return 'text-sky-400'
  if (efficiency >= 55) return 'text-emerald-400'
  return 'text-muted-foreground'
}

/** Código do atributo de velocidade nas runas. */
export const SPEED_STAT = 8

/** Velocidade da runa fora do principal: sub com grind ou inata (uma runa não repete atributo). */
export function runeSpeed(rune: RuneDetail): number {
  const sub = rune.substats.find((s) => s.type === SPEED_STAT)
  if (sub) return sub.value + sub.grindValue
  return rune.innateStat?.type === SPEED_STAT ? rune.innateStat.value : 0
}
