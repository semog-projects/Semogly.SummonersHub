import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { SelectFilter } from '../shared/filterControls'
import { controlClass } from '../shared/filterStyles'
import { formFromPreset, type OptimizerForm } from './optimizerOptions'
import { useDeleteOptimizerPreset, useOptimizerPresets, useSaveOptimizerPreset } from './useOptimizerPresets'

/** Limite do nome do preset (o mesmo da API). */
const MAX_NAME_LENGTH = 40

interface Props {
  form: OptimizerForm
  /** Formulário padrão deste monstro, base para aplicar os critérios do preset. */
  initial: OptimizerForm
  onApply: (form: OptimizerForm) => void
}

/** Aplicar, salvar e excluir presets de critérios do otimizador. */
export function OptimizerPresetsBar({ form, initial, onApply }: Props) {
  const presets = useOptimizerPresets()
  const save = useSaveOptimizerPreset()
  const remove = useDeleteOptimizerPreset()
  const [selected, setSelected] = useState<string | null>(null)
  const [name, setName] = useState('')

  const options = (presets.data ?? []).map((p) => ({ value: p.id, label: p.name }))
  const current = presets.data?.find((p) => p.id === selected)
  const error = save.error ?? remove.error

  return (
    <div role="group" aria-label="Presets" className="flex flex-wrap items-center gap-2 text-sm">
      <SelectFilter
        label="Preset"
        allLabel={options.length === 0 ? 'Nenhum preset salvo' : 'Escolha um preset'}
        value={selected}
        options={options}
        onChange={(id) => {
          setSelected(id)
          const preset = presets.data?.find((p) => p.id === id)
          if (preset) {
            onApply(formFromPreset(initial, preset.criteria))
            setName(preset.name)
          }
        }}
      />
      {current && (
        <Button variant="ghost" size="sm" disabled={remove.isPending} onClick={() => remove.mutate(current.id, { onSuccess: () => setSelected(null) })}>
          Excluir preset
        </Button>
      )}
      <input
        type="text"
        aria-label="Nome do preset"
        placeholder="Nome do preset"
        maxLength={MAX_NAME_LENGTH}
        value={name}
        onChange={(event) => setName(event.target.value)}
        className={cn(controlClass, 'w-40')}
      />
      <Button
        variant="outline"
        size="sm"
        disabled={name.trim() === '' || save.isPending}
        onClick={() => save.mutate({ name: name.trim(), form }, { onSuccess: (preset) => setSelected(preset.id) })}
      >
        Salvar preset
      </Button>
      {error && (
        <span role="alert" className="text-xs text-destructive">
          {error.message}
        </span>
      )}
    </div>
  )
}
