import type { StatValues } from '../types'

/** Atributos na ordem da tela do jogo. Os percentuais são exibidos com %. */
export const STAT_COLUMNS: Array<{ key: keyof StatValues; label: string; percent: boolean }> = [
  { key: 'hp', label: 'HP', percent: false },
  { key: 'attack', label: 'ATK', percent: false },
  { key: 'defense', label: 'DEF', percent: false },
  { key: 'speed', label: 'VEL', percent: false },
  { key: 'criticalRate', label: 'Taxa Crít.', percent: true },
  { key: 'criticalDamage', label: 'Dano Crít.', percent: true },
  { key: 'resistance', label: 'RES', percent: true },
  { key: 'accuracy', label: 'PRE', percent: true },
]
