import { ArrowLeft } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { MonsterIcon } from '@/features/accounts/monsters/MonsterIcon'
import { ELEMENT_TEXT_CLASSES } from '@/features/accounts/overview/elementStyles'
import { leaderSkillText } from '@/features/bestiary/leaderSkill'
import { useBestiaryMonster, type BestiaryMonster, type BestiaryStats } from '@/features/bestiary/useBestiary'
import { formatNumber } from '@/lib/format'
import { ARCHETYPE_NAME_LABELS, AWAKEN_LABELS, ELEMENT_NAME_LABELS } from '@/lib/game/labels'
import { QueryStatus } from './QueryStatus'

const STATS: Array<{ key: keyof BestiaryStats; label: string; percent?: boolean }> = [
  { key: 'hp', label: 'HP' },
  { key: 'attack', label: 'ATK' },
  { key: 'defense', label: 'DEF' },
  { key: 'speed', label: 'VEL' },
  { key: 'criticalRate', label: 'Taxa Crít.', percent: true },
  { key: 'criticalDamage', label: 'Dano Crít.', percent: true },
  { key: 'resistance', label: 'RES', percent: true },
  { key: 'accuracy', label: 'PRE', percent: true },
]

export function BestiaryMonsterPage() {
  const navigate = useNavigate()
  const params = useParams()
  const query = useBestiaryMonster(Number(params.com2usId))

  return (
    <div className="space-y-4">
      <button
        type="button"
        onClick={() =>
          // Volta pelo histórico para manter os filtros; aberto por link direto, vai para o bestiário
          (window.history.state as { idx?: number } | null)?.idx ? navigate(-1) : navigate('/bestiario')
        }
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" aria-hidden /> Voltar
      </button>

      <QueryStatus query={query} notFound="Monstro não encontrado no bestiário." loading="Carregando monstro…">
        {(data) => <BestiaryMonsterView data={data} />}
      </QueryStatus>
    </div>
  )
}

function BestiaryMonsterView({ data }: { data: BestiaryMonster }) {
  const { monster, maxLevelStats } = data

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-4">
        <MonsterIcon imageUrl={monster.imageUrl} className="size-16 rounded-lg" />
        <div className="space-y-0.5">
          <h1 className="font-heading text-2xl font-semibold">{monster.name}</h1>
          <p className="text-sm text-muted-foreground">
            <span className={ELEMENT_TEXT_CLASSES[monster.element]}>{ELEMENT_NAME_LABELS[monster.element]}</span> ·{' '}
            {ARCHETYPE_NAME_LABELS[monster.archetype]} · {monster.naturalStars}★ natural · {AWAKEN_LABELS[monster.awakenLevel]}
          </p>
          {data.leaderSkill && (
            <p className="text-sm">
              <span className="text-muted-foreground">Líder:</span> {leaderSkillText(data.leaderSkill)}
            </p>
          )}
          {data.awakenBonus && (
            <p className="text-sm">
              <span className="text-muted-foreground">Bônus de despertar:</span> {data.awakenBonus}
            </p>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[18rem_1fr]">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Atributos 6★ nv. 40</CardTitle>
            </CardHeader>
            <CardContent>
              {maxLevelStats ? (
                <table className="w-full text-sm" aria-label="Atributos">
                  <tbody>
                    {STATS.map((stat) => (
                      <tr key={stat.key}>
                        <th scope="row" className="py-1 text-left font-normal text-muted-foreground">
                          {stat.label}
                        </th>
                        <td className="py-1 text-right font-semibold tabular-nums">
                          {formatNumber(maxLevelStats[stat.key])}
                          {stat.percent && '%'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <p className="text-sm text-muted-foreground">O bestiário não informa os atributos.</p>
              )}
            </CardContent>
          </Card>

          {data.sources.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Onde obter</CardTitle>
              </CardHeader>
              <CardContent>
                <ul className="list-inside list-disc space-y-1 text-sm" aria-label="Onde obter">
                  {data.sources.map((source) => (
                    <li key={source}>{source}</li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Skills</CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            {data.skills.length === 0 && <p className="text-sm text-muted-foreground">Sem skills no bestiário.</p>}
            {data.skills.map((skill) => (
              <section key={skill.id} aria-label={`Skill ${skill.slot}: ${skill.name}`} className="flex gap-3">
                {skill.iconUrl ? (
                  <img src={skill.iconUrl} alt="" loading="lazy" className="size-10 shrink-0 rounded-md" />
                ) : (
                  <span className="block size-10 shrink-0 rounded-md bg-muted" />
                )}
                <div className="min-w-0 space-y-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="font-medium">{skill.name}</h2>
                    {skill.passive && <Badge variant="secondary">Passiva</Badge>}
                    {skill.aoe && <Badge variant="secondary">Em área</Badge>}
                    {skill.cooldown !== null && <Badge variant="outline">Recarga: {skill.cooldown} turnos</Badge>}
                  </div>
                  {skill.description && <p className="text-sm">{skill.description}</p>}
                  {skill.formulaText && (
                    <p className="text-xs text-muted-foreground">
                      Multiplicador: <code>{skill.formulaText}</code>
                      {skill.hits > 1 && ` × ${skill.hits} golpes`}
                    </p>
                  )}
                  {skill.levelProgress.length > 0 && (
                    <p className="text-xs text-muted-foreground">
                      Skill-ups ({skill.levelProgress.length}): {skill.levelProgress.join(' · ')}
                    </p>
                  )}
                </div>
              </section>
            ))}
          </CardContent>
        </Card>
      </div>

      {data.family.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Família</CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-5" aria-label="Família">
              {data.family.map((member) => (
                <li key={member.com2usId}>
                  <Link
                    to={`/bestiario/${member.com2usId}`}
                    className="flex items-center gap-2 rounded-lg border p-2 transition-colors hover:bg-muted/50"
                  >
                    <MonsterIcon imageUrl={member.imageUrl} className="size-10" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium">{member.name}</p>
                      <p className="text-xs text-muted-foreground">
                        <span className={ELEMENT_TEXT_CLASSES[member.element]}>{ELEMENT_NAME_LABELS[member.element]}</span> ·{' '}
                        {AWAKEN_LABELS[member.awakenLevel]}
                      </p>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
