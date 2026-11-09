/**
 * Calculadora de dano a partir das fórmulas do SWARFARM.
 *
 * A fórmula bruta é uma lista de grupos. Cada grupo de valor é avaliado em sequência, da esquerda para a direita
 * (operando, operador, operando...), e os grupos são combinados também em sequência pelos grupos que só têm um
 * operador. Ex.: [["ATK","*",1.0],["*"],["ATTACK_SPEED","+",140],["/"],[50]] = ATK × (VEL + 140) ÷ 50.
 */

type Token = string | number
type Operator = '+' | '-' | '*' | '/' | '^'

const OPERATORS = new Set<Token>(['+', '-', '*', '/', '^'])

/** Atributos do atacante e do alvo usados pelas fórmulas. */
export interface DamageContext {
  attacker: { attack: number; defense: number; speed: number; hp: number; level: number; hpRate: number }
  target: { hp: number; defense: number; speed: number; attack: number; hpRate: number }
}

/** Variáveis do SWARFARM suportadas; fórmulas com outras (dano fixo, contadores de batalha...) ficam de fora. */
function variable(name: string, ctx: DamageContext): number | undefined {
  const { attacker: a, target: t } = ctx
  switch (name) {
    case 'ATK':
      return a.attack
    case 'DEF':
      return a.defense
    case 'ATTACK_SPEED':
      return a.speed
    case 'ATTACK_TOT_HP':
      return a.hp
    case 'ATTACK_CUR_HP':
      return a.hp * a.hpRate
    case 'ATTACK_CUR_HP_RATE':
      return a.hpRate
    case 'ATTACK_LV':
      return a.level
    case 'TARGET_TOT_HP':
      return t.hp
    case 'TARGET_CUR_HP':
      return t.hp * t.hpRate
    case 'TARGET_CUR_HP_RATE':
      return t.hpRate
    case 'TARGET_DEF':
      return t.defense
    case 'TARGET_SPEED':
      return t.speed
    case 'TARGET_ATK':
      return t.attack
    default:
      return undefined
  }
}

function apply(left: number, operator: Operator, right: number): number {
  switch (operator) {
    case '+':
      return left + right
    case '-':
      return left - right
    case '*':
      return left * right
    case '/':
      return left / right
    case '^':
      return left ** right
  }
}

/** Avalia uma sequência operando-operador-operando... ; nulo se algum token não for suportado. */
function evaluateSequence(tokens: Array<number | Operator>): number | null {
  let result: number | null = null
  let pending: Operator | null = null

  for (const token of tokens) {
    if (typeof token === 'number') {
      if (result === null) result = token
      else if (pending !== null) {
        result = apply(result, pending, token)
        pending = null
      } else return null
    } else {
      if (result === null || pending !== null) return null
      pending = token
    }
  }

  return pending === null ? result : null
}

/**
 * Multiplicador da fórmula (dano bruto de um golpe, antes de skill-ups, crítico e DEF do alvo),
 * ou nulo se a fórmula usa algo que a calculadora não suporta.
 */
export function evaluateFormula(formula: string, ctx: DamageContext): number | null {
  let groups: Token[][]
  try {
    groups = JSON.parse(formula) as Token[][]
  } catch {
    return null
  }

  const resolve = (token: Token): number | Operator | null => {
    if (typeof token === 'number') return token
    if (OPERATORS.has(token)) return token as Operator
    return variable(token, ctx) ?? null
  }

  const sequence: Array<number | Operator> = []
  for (const group of groups) {
    if (!Array.isArray(group) || group.length === 0) return null

    // Grupo que só tem um operador liga o grupo anterior ao próximo
    if (group.length === 1 && typeof group[0] === 'string' && OPERATORS.has(group[0])) {
      sequence.push(group[0] as Operator)
      continue
    }

    const tokens = group.map(resolve)
    if (tokens.some((t) => t === null)) return null
    const value = evaluateSequence(tokens as Array<number | Operator>)
    if (value === null) return null
    sequence.push(value)
  }

  const result = evaluateSequence(sequence)
  return result !== null && Number.isFinite(result) ? result : null
}

export type HitType = 'normal' | 'critical' | 'crushing' | 'glancing'

/** Fator do tipo de golpe: crítico soma o Dano Crít.; esmagador +30%; glancing −30%. */
export function hitFactor(hit: HitType, criticalDamage: number): number {
  switch (hit) {
    case 'normal':
      return 1
    case 'critical':
      return 1 + criticalDamage / 100
    case 'crushing':
      return 1.3
    case 'glancing':
      return 0.7
  }
}

/** Fração do dano que passa pela DEF do alvo: 1000 ÷ (1140 + 3,5 × DEF), a mesma relação do HP efetivo. */
export function defenseFactor(targetDefense: number): number {
  return 1000 / (1140 + 3.5 * Math.max(0, targetDefense))
}

export interface SkillDamage {
  perHit: number
  total: number
}

/** Dano de um uso da habilidade: multiplicador × skill-ups × tipo de golpe × DEF do alvo, por golpe e no total. */
export function skillDamage(
  skill: { formula: string | null; hits: number; damageBonus: number },
  ctx: DamageContext,
  hit: HitType,
  criticalDamage: number,
): SkillDamage | null {
  if (!skill.formula) return null
  const multiplier = evaluateFormula(skill.formula, ctx)
  if (multiplier === null) return null

  const perHit = multiplier * (1 + skill.damageBonus / 100) * hitFactor(hit, criticalDamage) * defenseFactor(ctx.target.defense)
  return { perHit, total: perHit * Math.max(1, skill.hits) }
}
