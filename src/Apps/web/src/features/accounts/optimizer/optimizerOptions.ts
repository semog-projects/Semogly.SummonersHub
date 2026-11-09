import { EQUIPPABLE_RUNE_SETS, RUNE_STATS } from '@/lib/game/labels'
import type { MonsterDetail, OptimizeRunesRequest, OptimizerStat, StatValues } from '../types'

/** Atributos do otimizador com a chave equivalente em StatValues e o rótulo da tela. */
export const OPTIMIZER_STATS: Array<{ stat: OptimizerStat; key: keyof StatValues; label: string; percent: boolean }> = [
  { stat: 'Hp', key: 'hp', label: 'HP', percent: false },
  { stat: 'Attack', key: 'attack', label: 'ATK', percent: false },
  { stat: 'Defense', key: 'defense', label: 'DEF', percent: false },
  { stat: 'Speed', key: 'speed', label: 'VEL', percent: false },
  { stat: 'CriticalRate', key: 'criticalRate', label: 'Taxa Crít.', percent: true },
  { stat: 'CriticalDamage', key: 'criticalDamage', label: 'Dano Crít.', percent: true },
  { stat: 'Resistance', key: 'resistance', label: 'RES', percent: true },
  { stat: 'Accuracy', key: 'accuracy', label: 'PRE', percent: true },
  { stat: 'EffectiveHp', key: 'effectiveHp', label: 'HP Efet.', percent: false },
]

export const MIN_WEIGHT = 0.1
export const MAX_WEIGHT = 10

export const MAX_FOCUS = 5

/** Sets que formam bônus (Intangible e Immemorial não). */
export const OPTIMIZER_SETS = EQUIPPABLE_RUNE_SETS.filter((set) => set !== 'Intangible')

/** Principais possíveis em cada slot variável. */
export const MAIN_STATS_BY_SLOT: Record<2 | 4 | 6, number[]> = {
  2: [8, 2, 4, 6, 1, 3, 5],
  4: [9, 10, 2, 4, 6, 1, 3, 5],
  6: [2, 4, 6, 11, 12, 1, 3, 5],
}

export function mainStatLabel(type: number): string {
  const stat = RUNE_STATS[type]
  return stat ? `${stat.label}${stat.percent ? '%' : ''}` : `Atributo ${type}`
}

/** Estado do formulário; campos numéricos vazios ficam como string vazia. */
export interface OptimizerForm {
  sets: Array<string | null>
  mainStats: Record<2 | 4 | 6, number | null>
  focus: OptimizerStat[]
  weights: Partial<Record<OptimizerStat, string>>
  minimums: Partial<Record<OptimizerStat, string>>
  maximums: Partial<Record<OptimizerStat, string>>
  includeEquippedOnOthers: boolean
  assumeMaxLevel: boolean
  optimizeArtifacts: boolean
}

/** Formulário inicial a partir da build atual: mesmos sets, mesmos principais e foco em VEL. */
export function initialForm(monster: MonsterDetail): OptimizerForm {
  const mainOf = (slot: number) => monster.runes.find((r) => r.slot === slot)?.mainStat.type ?? null
  const sets = monster.activeSets.filter((set) => OPTIMIZER_SETS.includes(set)).slice(0, 3)
  return {
    sets: [sets[0] ?? null, sets[1] ?? null, sets[2] ?? null],
    mainStats: { 2: mainOf(2), 4: mainOf(4), 6: mainOf(6) },
    focus: ['Speed'],
    weights: {},
    minimums: {},
    maximums: {},
    includeEquippedOnOthers: false,
    assumeMaxLevel: false,
    optimizeArtifacts: false,
  }
}

export function toRequest(form: OptimizerForm): OptimizeRunesRequest {
  const mainStats: Record<string, number[]> = {}
  for (const slot of [2, 4, 6] as const) {
    const stat = form.mainStats[slot]
    if (stat !== null) mainStats[slot] = [stat]
  }

  return {
    sets: form.sets.filter((set): set is string => set !== null),
    mainStats,
    focus: form.focus,
    minimums: OPTIMIZER_STATS.flatMap(({ stat }) => {
      const value = positive(form.minimums[stat])
      return value === null ? [] : [{ stat, min: value }]
    }),
    maximums: OPTIMIZER_STATS.flatMap(({ stat }) => {
      const value = positive(form.maximums[stat])
      return value === null ? [] : [{ stat, max: value }]
    }),
    // Só pesos diferentes de 1, dos atributos que estão no foco
    weights: Object.fromEntries(
      form.focus.flatMap((stat) => {
        const value = Number(form.weights[stat])
        return form.weights[stat] && value >= MIN_WEIGHT && value <= MAX_WEIGHT && value !== 1 ? [[stat, value]] : []
      }),
    ),
    includeEquippedOnOthers: form.includeEquippedOnOthers,
    assumeMaxLevel: form.assumeMaxLevel,
    optimizeArtifacts: form.optimizeArtifacts,
  }
}

/** Atributos com máximo menor que o mínimo (o formulário não deixa enviar). */
export function conflictingLimits(form: OptimizerForm): OptimizerStat[] {
  return OPTIMIZER_STATS.flatMap(({ stat }) => {
    const min = positive(form.minimums[stat])
    const max = positive(form.maximums[stat])
    return min !== null && max !== null && max < min ? [stat] : []
  })
}

function positive(text: string | undefined): number | null {
  const value = Number(text)
  return text && Number.isFinite(value) && value > 0 ? value : null
}

/** Peças pedidas pelos sets (4 para Swift, Violent...; 2 para os demais). */
export function requiredPieces(sets: Array<string | null>): number {
  const fourPieces = ['Swift', 'Rage', 'Fatal', 'Despair', 'Vampire', 'Violent']
  return sets.reduce((sum, set) => sum + (set === null ? 0 : fourPieces.includes(set) ? 4 : 2), 0)
}
