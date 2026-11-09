import { combatSpeed, simulateTurns, ticksToAct } from './speedTune'

describe('combatSpeed', () => {
  it('soma líder e torre como percentual da VEL base, arredondado para cima', () => {
    // Base 111, total 250, líder 24% + torre 15% = ceil(43,29) = 44
    expect(combatSpeed(111, 250, 24, 15)).toBe(294)
    expect(combatSpeed(111, 250, 0, 0)).toBe(250)
  })
})

describe('ticksToAct', () => {
  it('ticks até encher a barra, considerando a barra inicial', () => {
    // 300 de VEL: 21% por tick → 5 ticks (105%)
    expect(ticksToAct(300)).toBe(5)
    // Começando com 30%: faltam 70% → 4 ticks (84%)
    expect(ticksToAct(300, 30)).toBe(4)
    expect(ticksToAct(300, 100)).toBe(0)
    expect(ticksToAct(0)).toBe(Number.POSITIVE_INFINITY)
  })
})

describe('simulateTurns', () => {
  it('o mais rápido age primeiro e o mais lento depois', () => {
    const turns = simulateTurns(
      [
        { id: 'lento', name: 'Lento', speed: 150, startAtb: 0 },
        { id: 'rapido', name: 'Rápido', speed: 300, startAtb: 0 },
      ],
      4,
    )

    // Rápido: 21%/tick → age nos ticks 5 e 10; Lento: 10,5%/tick → tick 10 (105%). No tick 10 os dois têm 105%:
    // o empate fica com a VEL maior
    expect(turns).toEqual([
      { tick: 5, unitId: 'rapido' },
      { tick: 10, unitId: 'rapido' },
      { tick: 10, unitId: 'lento' },
      { tick: 15, unitId: 'rapido' },
    ])
  })

  it('barra inicial adianta o primeiro turno', () => {
    const turns = simulateTurns(
      [
        { id: 'a', name: 'A', speed: 200, startAtb: 0 },
        { id: 'b', name: 'B', speed: 150, startAtb: 50 },
      ],
      1,
    )

    // A: 14%/tick → 8 ticks; B: 50% + 10,5%/tick → 5 ticks
    expect(turns).toEqual([{ tick: 5, unitId: 'b' }])
  })

  it('empate de barra fica com a VEL maior', () => {
    const turns = simulateTurns(
      [
        { id: 'a', name: 'A', speed: 200, startAtb: 100 },
        { id: 'b', name: 'B', speed: 250, startAtb: 100 },
      ],
      2,
    )
    expect(turns.map((t) => t.unitId)).toEqual(['b', 'a'])
  })

  it('sem unidades com VEL não há turnos', () => {
    expect(simulateTurns([{ id: 'x', name: 'X', speed: 0, startAtb: 0 }], 3)).toEqual([])
  })
})
