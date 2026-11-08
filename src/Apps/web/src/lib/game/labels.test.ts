import { decodeCraftTypeId, formatArtifactEffect, formatRuneStat } from './labels'

describe('labels do jogo', () => {
  it.each([
    [8, 24, 'VEL +24'],
    [4, 63, 'ATK +63%'],
    [1, 2448, 'HP +2448'],
    [10, 80, 'Dano Crít. +80%'],
  ])('formata atributo de runa %i', (type, value, expected) => {
    expect(formatRuneStat(type, value)).toBe(expected)
  })

  it('formata sub de artefato com o valor', () => {
    expect(formatArtifactEffect(221, 31)).toBe('Dano adicional de 31% da VEL')
  })

  it('não quebra com códigos desconhecidos', () => {
    expect(formatRuneStat(77, 1)).toBe('Atributo 77 +1')
    expect(formatArtifactEffect(999, 5)).toBe('Efeito 999 (5)')
  })

  it.each([
    [150905, { setId: 15, statType: 9, rarity: 5 }],
    [20804, { setId: 2, statType: 8, rarity: 4 }],
    [40915, { setId: 4, statType: 9, rarity: 5 }],
  ])('decodifica craft_type_id %i', (code, expected) => {
    expect(decodeCraftTypeId(code)).toEqual(expected)
  })
})
