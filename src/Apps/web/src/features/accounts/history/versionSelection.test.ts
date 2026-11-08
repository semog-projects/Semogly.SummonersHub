import { selectVersions } from './versionSelection'

const params = (query: string) => new URLSearchParams(query)

describe('selectVersions', () => {
  it('sem parâmetros compara a penúltima com a última', () => {
    expect(selectVersions(params(''), [1, 3, 2])).toEqual({ from: 2, to: 3 })
  })

  it('usa as versões da URL', () => {
    expect(selectVersions(params('de=1&para=3'), [1, 2, 3])).toEqual({ from: 1, to: 3 })
  })

  it('só "para": compara com a versão anterior a ela', () => {
    expect(selectVersions(params('para=2'), [1, 2, 3])).toEqual({ from: 1, to: 2 })
  })

  it('ignora versões que não existem', () => {
    expect(selectVersions(params('de=7&para=9'), [1, 2])).toEqual({ from: 1, to: 2 })
  })

  it('com uma versão só não há o que comparar', () => {
    expect(selectVersions(params(''), [1])).toEqual({ from: null, to: 1 })
  })

  it('versões iguais deixam a comparada vazia', () => {
    expect(selectVersions(params('de=2&para=2'), [1, 2])).toEqual({ from: 2, to: null })
  })
})
