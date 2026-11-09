import { Chip } from '../shared/filterControls'
import type { MonsterBuildOption } from '../types'
import { buildLabel } from './buildLabels'

interface Props {
  builds: MonsterBuildOption[] | undefined
  /** Chave da build escolhida; nula = build normal. */
  value: string | null
  onChange: (key: string | null) => void
  label?: string
}

/** Escolhe entre a build normal e as de RTA/Siege/Arena. Some quando o monstro só tem a normal. */
export function BuildSelector({ builds, value, onChange, label = 'Build' }: Props) {
  if (!builds?.length) return null
  return (
    <div role="group" aria-label={label} className="flex flex-wrap items-center gap-1">
      <span className="mr-1 text-sm text-muted-foreground">{label}:</span>
      <Chip active={value === null} onClick={() => onChange(null)}>
        Normal
      </Chip>
      {builds.map((build) => (
        <Chip key={build.key} active={value === build.key} onClick={() => onChange(build.key)}>
          {buildLabel(build)}
        </Chip>
      ))}
    </div>
  )
}
