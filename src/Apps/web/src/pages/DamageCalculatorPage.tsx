import { ArrowLeft } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { controlClass } from '@/features/accounts/shared/filterStyles'
import type { MonsterSkills } from '@/features/accounts/types'
import { useMonsterSkills } from '@/features/accounts/useAccounts'
import { formatNumber } from '@/lib/format'
import { skillDamage, type DamageContext, type HitType } from '@/lib/game/damage'
import { cn } from '@/lib/utils'
import { QueryStatus } from './QueryStatus'

const HIT_COLUMNS: Array<{ hit: HitType; label: string }> = [
  { hit: 'normal', label: 'Normal' },
  { hit: 'critical', label: 'Crítico' },
  { hit: 'crushing', label: 'Esmagador' },
  { hit: 'glancing', label: 'Glancing' },
]

/** Alvo padrão: sem DEF (dano bruto) e com HP cheio. */
interface TargetForm {
  defense: string
  hp: string
  speed: string
  attack: string
  hpPercent: string
  attackerHpPercent: string
}

const DEFAULT_TARGET: TargetForm = { defense: '0', hp: '0', speed: '0', attack: '0', hpPercent: '100', attackerHpPercent: '100' }

const TARGET_FIELDS: Array<{ key: keyof TargetForm; label: string }> = [
  { key: 'defense', label: 'DEF do alvo' },
  { key: 'hp', label: 'HP máx. do alvo' },
  { key: 'hpPercent', label: 'HP atual do alvo (%)' },
  { key: 'speed', label: 'VEL do alvo' },
  { key: 'attack', label: 'ATK do alvo' },
  { key: 'attackerHpPercent', label: 'Seu HP atual (%)' },
]

export function DamageCalculatorPage() {
  const params = useParams()
  const wizardId = Number(params.wizardId)
  const monsterId = Number(params.monsterId)
  const query = useMonsterSkills(wizardId, monsterId)

  return (
    <div className="space-y-4">
      <Link
        to={`/contas/${wizardId}/monstros/${monsterId}`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" aria-hidden /> Voltar ao monstro
      </Link>

      <QueryStatus query={query} notFound="Monstro não encontrado no último import." loading="Carregando habilidades…">
        {(monster) => <Calculator monster={monster} />}
      </QueryStatus>
    </div>
  )
}

function Calculator({ monster }: { monster: MonsterSkills }) {
  const [target, setTarget] = useState<TargetForm>(DEFAULT_TARGET)
  const number = (value: string) => Math.max(0, Number(value) || 0)

  const ctx: DamageContext = {
    attacker: {
      attack: monster.total.attack,
      defense: monster.total.defense,
      speed: monster.total.speed,
      hp: monster.total.hp,
      level: monster.level,
      hpRate: Math.min(100, number(target.attackerHpPercent)) / 100,
    },
    target: {
      defense: number(target.defense),
      hp: number(target.hp),
      speed: number(target.speed),
      attack: number(target.attack),
      hpRate: Math.min(100, number(target.hpPercent)) / 100,
    },
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-heading text-2xl font-semibold">Dano: {monster.name ?? `Monstro ${monster.masterId}`}</h1>
        <p className="text-sm text-muted-foreground">
          ATK {formatNumber(monster.total.attack)} · DEF {formatNumber(monster.total.defense)} · VEL{' '}
          {formatNumber(monster.total.speed)} · HP {formatNumber(monster.total.hp)} · Dano Crít. {monster.total.criticalDamage}%
        </p>
      </div>

      <Card>
        <CardContent>
          <form aria-label="Alvo" className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6" onSubmit={(e) => e.preventDefault()}>
            {TARGET_FIELDS.map(({ key, label }) => (
              <label key={key} className="space-y-1 text-sm">
                <span className="block text-xs text-muted-foreground">{label}</span>
                <input
                  type="number"
                  min={0}
                  inputMode="numeric"
                  aria-label={label}
                  value={target[key]}
                  onChange={(event) => setTarget({ ...target, [key]: event.target.value })}
                  className={cn(controlClass, 'w-full')}
                />
              </label>
            ))}
          </form>
        </CardContent>
      </Card>

      <div className="overflow-x-auto">
        <table className="w-full text-sm" aria-label="Dano das habilidades">
          <thead className="text-left text-xs text-muted-foreground">
            <tr className="border-b">
              <th scope="col" className="py-2 pr-3 font-medium">
                Habilidade
              </th>
              <th scope="col" className="py-2 pr-3 font-medium">
                Fórmula
              </th>
              {HIT_COLUMNS.map(({ hit, label }) => (
                <th key={hit} scope="col" className="py-2 pr-3 text-right font-medium">
                  {label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {monster.skills.map((skill) => {
              const results = HIT_COLUMNS.map(({ hit }) => skillDamage(skill, ctx, hit, monster.total.criticalDamage))
              const unsupported = skill.formula !== null && results[0] === null

              return (
                <tr key={skill.skillId} className="border-b align-top last:border-0">
                  <th scope="row" className="py-2 pr-3 text-left font-normal">
                    <p className="font-medium">
                      {skill.slot > 0 && `S${skill.slot} · `}
                      {skill.name ?? `Habilidade ${skill.skillId}`}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      nv. {skill.level}/{skill.maxLevel}
                      {skill.damageBonus > 0 && ` · skill-ups +${skill.damageBonus}%`}
                      {skill.hits > 1 && ` · ${skill.hits} golpes`}
                    </p>
                    <div className="mt-1 flex gap-1">
                      {skill.aoe && <Badge variant="outline">Em área</Badge>}
                      {skill.passive && <Badge variant="outline">Passiva</Badge>}
                    </div>
                  </th>
                  <td className="py-2 pr-3 font-mono text-xs text-muted-foreground">{skill.formulaText ?? '—'}</td>
                  {skill.formula === null || unsupported ? (
                    <td colSpan={HIT_COLUMNS.length} className="py-2 pr-3 text-right text-xs text-muted-foreground">
                      {unsupported ? 'Fórmula ainda não suportada pela calculadora' : 'Sem dano'}
                    </td>
                  ) : (
                    results.map((damage, i) => (
                      <td key={HIT_COLUMNS[i].hit} className="py-2 pr-3 text-right tabular-nums">
                        <span className="font-medium">{formatNumber(Math.round(damage!.total))}</span>
                        {skill.hits > 1 && (
                          <span className="block text-xs text-muted-foreground">{formatNumber(Math.round(damage!.perHit))} por golpe</span>
                        )}
                      </td>
                    ))
                  )}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <p className="text-xs text-muted-foreground">
        Dano de um uso = multiplicador da fórmula × (1 + skill-ups) × tipo de golpe × 1000 ÷ (1140 + 3,5 × DEF do alvo).
        Crítico soma o Dano Crít.; esmagador +30%; glancing −30%. Fora do cálculo: líder, buffs/debuffs, sets de efeito,
        subs de artefato e passivas.
      </p>
    </div>
  )
}
