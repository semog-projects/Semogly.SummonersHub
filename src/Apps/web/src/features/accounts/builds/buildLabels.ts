import type { BuildKindName, MonsterBuildOption } from '../types'

export const BUILD_KIND_LABELS: Record<BuildKindName, string> = {
  Rta: 'RTA',
  Siege: 'Siege',
  Arena: 'Arena',
}

/** Rótulo da build alternativa: "RTA", "Siege · defesa 3", "Arena · defesa". */
export function buildLabel(option: MonsterBuildOption): string {
  if (option.kind === 'Siege') return option.number > 0 ? `Siege · defesa ${option.number}` : 'Siege'
  if (option.kind === 'Arena') return 'Arena · defesa'
  return BUILD_KIND_LABELS[option.kind]
}
