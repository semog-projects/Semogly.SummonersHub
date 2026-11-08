import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { selectClass, optionClass } from './filterStyles'

/** Controles de filtro comuns às listas da conta (monstros, runas, artefatos). */

interface SelectFilterProps<T extends string | number> {
  label: string
  /** Texto da opção vazia (sem filtro). */
  allLabel: string
  value: T | null
  options: Array<{ value: T; label: string }>
  onChange: (value: T | null) => void
}

/** Select com uma opção "todos" que limpa o filtro. Valores numéricos voltam como número. */
export function SelectFilter<T extends string | number>({ label, allLabel, value, options, onChange }: SelectFilterProps<T>) {
  return (
    <select
      aria-label={label}
      value={value ?? ''}
      onChange={(event) => {
        const raw = event.target.value
        const option = options.find((o) => String(o.value) === raw)
        onChange(option ? option.value : null)
      }}
      className={selectClass}
    >
      <option value="" className={optionClass}>
        {allLabel}
      </option>
      {options.map((option) => (
        <option key={option.value} value={option.value} className={optionClass}>
          {option.label}
        </option>
      ))}
    </select>
  )
}

export function Chip({ active, onClick, children }: { active: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={cn(
        'h-8 rounded-md border px-2 text-sm tabular-nums transition-colors hover:bg-muted',
        active && 'border-primary bg-muted font-medium',
      )}
    >
      {children}
    </button>
  )
}
