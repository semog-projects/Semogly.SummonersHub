/**
 * Speed tune: ordem de turnos pela barra de ataque (ATB).
 *
 * Modelo usado pela comunidade: a cada tick, a barra de cada monstro sobe 7% da VEL de combate; quem chega a
 * 100% age (o mais adiantado primeiro) e a barra volta a 0. A VEL de combate soma à VEL total (runas e Swift)
 * os bônus de líder e de torre/flag, que são percentuais da VEL base.
 */

/** Porcentagem da barra de ataque ganha por tick, por ponto de VEL. */
export const ATB_PER_SPEED = 0.07

/** VEL em batalha: total + (líder + torre)% da base, arredondado para cima como os demais bônus percentuais. */
export function combatSpeed(baseSpeed: number, totalSpeed: number, leaderPercent: number, towerPercent: number): number {
  return totalSpeed + Math.ceil((baseSpeed * (leaderPercent + towerPercent)) / 100)
}

/** Ticks até a primeira ação, partindo de uma barra inicial (ex.: 30% de um ganho de barra no início). */
export function ticksToAct(speed: number, startAtb = 0): number {
  if (startAtb >= 100) return 0
  if (speed <= 0) return Number.POSITIVE_INFINITY
  return Math.ceil((100 - startAtb) / (speed * ATB_PER_SPEED))
}

export interface SpeedUnit {
  id: string
  name: string
  speed: number
  /** Barra de ataque inicial (%). */
  startAtb: number
}

export interface Turn {
  tick: number
  unitId: string
}

/**
 * Primeiras ações em ordem. Num mesmo tick, quem tem a barra mais cheia age primeiro; empate fica com a VEL
 * maior e, depois, com a ordem da lista.
 */
export function simulateTurns(units: SpeedUnit[], turns: number): Turn[] {
  const active = units.filter((u) => u.speed > 0)
  if (active.length === 0) return []

  const atb = new Map(active.map((u) => [u.id, u.startAtb]))
  const result: Turn[] = []

  // Limite de segurança: com VEL > 0, cada unidade age no máximo a cada 100 ÷ (0,07 × VEL) ticks
  for (let tick = 0; result.length < turns && tick < turns * 2000; tick++) {
    if (tick > 0) {
      for (const unit of active) atb.set(unit.id, atb.get(unit.id)! + unit.speed * ATB_PER_SPEED)
    }

    for (;;) {
      const ready = active
        .map((unit, index) => ({ unit, index, atb: atb.get(unit.id)! }))
        .filter((x) => x.atb >= 100)
        .sort((a, b) => b.atb - a.atb || b.unit.speed - a.unit.speed || a.index - b.index)[0]
      if (!ready || result.length >= turns) break

      result.push({ tick, unitId: ready.unit.id })
      atb.set(ready.unit.id, 0)
    }
  }

  return result
}
