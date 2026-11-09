import { defenseFactor, evaluateFormula, hitFactor, skillDamage, type DamageContext } from './damage'

const ctx: DamageContext = {
  attacker: { attack: 2000, defense: 800, speed: 200, hp: 30000, level: 40, hpRate: 1 },
  target: { hp: 50000, defense: 1000, speed: 150, attack: 1500, hpRate: 0.5 },
}

describe('evaluateFormula', () => {
  it('multiplicador simples', () => {
    expect(evaluateFormula('[["ATK", "*", 3.6]]', ctx)).toBeCloseTo(7200)
  })

  it('grupos ligados por operador, em sequência (Ambush: ATK × (VEL + 140) ÷ 50)', () => {
    expect(evaluateFormula('[["ATK", "*", 1.0], ["*"], ["ATTACK_SPEED", "+", 140], ["/"], [50]]', ctx)).toBeCloseTo((2000 * 340) / 50)
  })

  it('soma de termos com HP máximo e do alvo', () => {
    expect(evaluateFormula('[["ATK", "*", 4.1], ["+"], ["TARGET_TOT_HP", "*", 0.1]]', ctx)).toBeCloseTo(8200 + 5000)
  })

  it('porcentagem de HP atual do alvo (Chow Down: ATK × (8,8 − 3,3 × HP%))', () => {
    expect(evaluateFormula('[["ATK", "*", 1.0], ["*"], ["TARGET_CUR_HP_RATE", "*", -3.3, "+", 8.8]]', ctx)).toBeCloseTo(2000 * (8.8 - 1.65))
  })

  it('potência com a DEF do alvo', () => {
    const formula = '[[0.85, "*", 1], ["^"], ["TARGET_DEF", "-", 900.0, "/", 100.0], ["*"], ["ATK", "*", 2.4], ["+"], ["ATK", "*", 4.8]]'
    expect(evaluateFormula(formula, ctx)).toBeCloseTo(0.85 ** 1 * 4800 + 9600)
  })

  it('nulo para variáveis não suportadas, dano fixo ou JSON inválido', () => {
    expect(evaluateFormula('[["ABSORPTION_TOT_CNT", "*", 2]]', ctx)).toBeNull()
    expect(evaluateFormula('[["ATTACK_LOSS_HP", "FIXED"]]', ctx)).toBeNull()
    expect(evaluateFormula('não é json', ctx)).toBeNull()
  })
})

describe('dano', () => {
  it('fatores de golpe e de DEF', () => {
    expect(hitFactor('critical', 150)).toBe(2.5)
    expect(hitFactor('crushing', 150)).toBe(1.3)
    expect(hitFactor('glancing', 150)).toBe(0.7)
    expect(defenseFactor(0)).toBeCloseTo(1000 / 1140)
    expect(defenseFactor(1000)).toBeCloseTo(1000 / 4640)
  })

  it('aplica skill-ups, golpe, DEF e número de golpes', () => {
    const damage = skillDamage({ formula: '[["ATK", "*", 1.9]]', hits: 4, damageBonus: 35 }, ctx, 'critical', 150)

    const perHit = 3800 * 1.35 * 2.5 * (1000 / 4640)
    expect(damage?.perHit).toBeCloseTo(perHit)
    expect(damage?.total).toBeCloseTo(perHit * 4)
  })

  it('nulo sem fórmula', () => {
    expect(skillDamage({ formula: null, hits: 1, damageBonus: 0 }, ctx, 'normal', 0)).toBeNull()
  })
})
