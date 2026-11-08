import type { MonsterDetail } from '../types'
import { initialForm, requiredPieces, toRequest } from './optimizerOptions'

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
      minimums: {},
      includeEquippedOnOthers: false,
      assumeMaxLevel: false,
    })
  })

  it('monta a requisição só com o que foi preenchido', () => {
    const form = { ...initialForm(monster), minimums: { Speed: '200', Accuracy: '', Hp: 'x' } }

    expect(toRequest(form)).toEqual({
      sets: ['Violent', 'Will'],
      mainStats: { 2: [8], 4: [10] },
      focus: ['Speed'],
      minimums: [{ stat: 'Speed', min: 200 }],
      includeEquippedOnOthers: false,
      assumeMaxLevel: false,
    })
  })

  it('conta as peças dos sets', () => {
    expect(requiredPieces(['Violent', 'Will', null])).toBe(6)
    expect(requiredPieces(['Swift', 'Swift', null])).toBe(8)
    expect(requiredPieces([null, null, null])).toBe(0)
  })
})
