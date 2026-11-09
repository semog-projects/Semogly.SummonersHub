import type { FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import { Chip, SelectFilter } from '../shared/filterControls'
import { controlClass } from '../shared/filterStyles'
import {
  conflictingLimits,
  MAIN_STATS_BY_SLOT,
  mainStatLabel,
  MAX_FOCUS,
  MAX_WEIGHT,
  MIN_WEIGHT,
  OPTIMIZER_SETS,
  OPTIMIZER_STATS,
  requiredPieces,
  type OptimizerForm,
} from './optimizerOptions'

const SET_OPTIONS = OPTIMIZER_SETS.map((set) => ({ value: set, label: set }))

interface Props {
  form: OptimizerForm
  onChange: (form: OptimizerForm) => void
  onSubmit: () => void
  pending: boolean
}

export function OptimizerFormView({ form, onChange, onSubmit, pending }: Props) {
  const update = (patch: Partial<OptimizerForm>) => onChange({ ...form, ...patch })
  const pieces = requiredPieces(form.sets)
  const conflicts = conflictingLimits(form)
  const invalid = pieces > 6 || form.focus.length === 0 || conflicts.length > 0

  function toggleFocus(stat: OptimizerForm['focus'][number]) {
    const focus = form.focus.includes(stat) ? form.focus.filter((s) => s !== stat) : [...form.focus, stat]
    if (focus.length <= MAX_FOCUS) update({ focus })
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    if (!invalid) onSubmit()
  }

  return (
    <Card>
      <CardContent>
        <form onSubmit={submit} className="space-y-5" aria-label="Critérios do otimizador">
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">Sets</legend>
            <div className="flex flex-wrap items-center gap-2">
              {form.sets.map((set, i) => (
                <SelectFilter
                  key={i}
                  label={`Set ${i + 1}`}
                  allLabel={i === 0 ? 'Qualquer set' : 'Nenhum'}
                  value={set}
                  options={SET_OPTIONS}
                  onChange={(value) => update({ sets: form.sets.map((s, j) => (j === i ? value : s)) })}
                />
              ))}
              <span className={cn('text-xs', pieces > 6 ? 'text-destructive' : 'text-muted-foreground')}>
                {pieces} de 6 peças{pieces > 6 && ': escolha menos sets'}
              </span>
            </div>
          </fieldset>

          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">Principal dos slots 2, 4 e 6</legend>
            <div className="flex flex-wrap gap-2">
              {([2, 4, 6] as const).map((slot) => (
                <SelectFilter
                  key={slot}
                  label={`Principal do slot ${slot}`}
                  allLabel={`Slot ${slot}: qualquer`}
                  value={form.mainStats[slot]}
                  options={MAIN_STATS_BY_SLOT[slot].map((type) => ({ value: type, label: `Slot ${slot}: ${mainStatLabel(type)}` }))}
                  onChange={(value) => update({ mainStats: { ...form.mainStats, [slot]: value } })}
                />
              ))}
            </div>
          </fieldset>

          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">
              Foco <span className="font-normal text-muted-foreground">(até {MAX_FOCUS}; peso de {MIN_WEIGHT} a {MAX_WEIGHT})</span>
            </legend>
            <div role="group" aria-label="Foco" className="flex flex-wrap gap-1">
              {OPTIMIZER_STATS.map(({ stat, label }) => (
                <Chip key={stat} active={form.focus.includes(stat)} onClick={() => toggleFocus(stat)}>
                  {label}
                </Chip>
              ))}
            </div>
            {form.focus.length > 0 && (
              <div className="flex flex-wrap gap-3">
                {OPTIMIZER_STATS.filter(({ stat }) => form.focus.includes(stat)).map(({ stat, label }) => (
                  <label key={stat} className="flex items-center gap-2 text-sm">
                    <span className="text-muted-foreground">Peso {label}</span>
                    <input
                      type="number"
                      min={MIN_WEIGHT}
                      max={MAX_WEIGHT}
                      step="any"
                      placeholder="1"
                      aria-label={`Peso de ${label}`}
                      value={form.weights[stat] ?? ''}
                      onChange={(event) => update({ weights: { ...form.weights, [stat]: event.target.value } })}
                      className={cn(controlClass, 'w-16')}
                    />
                  </label>
                ))}
              </div>
            )}
          </fieldset>

          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">
              Mínimos e máximos <span className="font-normal text-muted-foreground">(atributos totais)</span>
            </legend>
            <div className="grid gap-x-6 gap-y-2 sm:grid-cols-2 lg:grid-cols-3">
              {OPTIMIZER_STATS.map(({ stat, label, percent }) => (
                <div key={stat} className="flex items-center gap-2 text-sm">
                  <span className={cn('w-20 shrink-0 text-muted-foreground', conflicts.includes(stat) && 'text-destructive')}>
                    {label}
                    {percent && ' %'}
                  </span>
                  <input
                    type="number"
                    min={0}
                    inputMode="numeric"
                    placeholder="mín."
                    aria-label={`Mínimo de ${label}`}
                    value={form.minimums[stat] ?? ''}
                    onChange={(event) => update({ minimums: { ...form.minimums, [stat]: event.target.value } })}
                    className={cn(controlClass, 'w-full min-w-0')}
                  />
                  <input
                    type="number"
                    min={0}
                    inputMode="numeric"
                    placeholder="máx."
                    aria-label={`Máximo de ${label}`}
                    value={form.maximums[stat] ?? ''}
                    onChange={(event) => update({ maximums: { ...form.maximums, [stat]: event.target.value } })}
                    className={cn(controlClass, 'w-full min-w-0')}
                  />
                </div>
              ))}
            </div>
            {conflicts.length > 0 && (
              <p role="alert" className="text-xs text-destructive">
                O máximo não pode ser menor que o mínimo.
              </p>
            )}
          </fieldset>

          <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={form.includeEquippedOnOthers}
                onChange={(event) => update({ includeEquippedOnOthers: event.target.checked })}
              />
              Usar runas de outros monstros
            </label>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={form.assumeMaxLevel}
                onChange={(event) => update({ assumeMaxLevel: event.target.checked })}
              />
              Considerar runas em +15
              <span className="text-xs text-muted-foreground">(principal no máximo, subs pela média)</span>
            </label>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={form.optimizeArtifacts}
                onChange={(event) => update({ optimizeArtifacts: event.target.checked })}
              />
              Trocar artefatos
              <span className="text-xs text-muted-foreground">(pelo principal)</span>
            </label>
            <Button type="submit" disabled={invalid || pending} className="sm:ml-auto">
              {pending ? 'Otimizando…' : 'Otimizar'}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}
