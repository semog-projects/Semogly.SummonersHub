import type { MonsterDetail } from '../types'
import { conflictingLimits, initialForm, requiredPieces, toRequest } from './optimizerOptions'

const monster = {
  activeSets: ['Violent', 'Will'],
  runes: [
    { slot: 2, mainStat: { type: 8, value: 42 } },
    { slot: 4, mainStat: { type: 10, value: 80 } },
  ],
} as unknown as MonsterDetail

describe('optimizerOptions', () => {
  it('começa com os sets e principais atuais e foco em VEL', () => {
    expect(initialForm(monster)).toEqual({
      sets: ['Violent', 'Will', null],
      mainStats: { 2: 8, 4: 10, 6: null },
      focus: ['Speed'],
      weights: {},
      minimums: {},
      maximums: {},
      includeEquippedOnOthers: false,
      assumeMaxLevel: false,
      optimizeArtifacts: false,
    })
  })

  it('monta a requisição só com o que foi preenchido', () => {
    const form = {
      ...initialForm(monster),
      focus: ['Speed' as const, 'EffectiveHp' as const],
      weights: { Speed: '3', EffectiveHp: '1', Hp: '5' },
      minimums: { Speed: '200', Accuracy: '', Hp: 'x' },
      maximums: { Speed: '230' },
    }

    expect(toRequest(form)).toEqual({
      sets: ['Violent', 'Will'],
      mainStats: { 2: [8], 4: [10] },
      focus: ['Speed', 'EffectiveHp'],
      minimums: [{ stat: 'Speed', min: 200 }],
      maximums: [{ stat: 'Speed', max: 230 }],
      weights: { Speed: 3 },
      includeEquippedOnOthers: false,
      assumeMaxLevel: false,
      optimizeArtifacts: false,
    })
  })

  it('aponta máximo menor que o mínimo', () => {
    const form = { ...initialForm(monster), minimums: { Speed: '200' }, maximums: { Speed: '180', Hp: '50000' } }
    expect(conflictingLimits(form)).toEqual(['Speed'])
  })

  it('conta as peças dos sets', () => {
    expect(requiredPieces(['Violent', 'Will', null])).toBe(6)
    expect(requiredPieces(['Swift', 'Swift', null])).toBe(8)
    expect(requiredPieces([null, null, null])).toBe(0)
  })
})
