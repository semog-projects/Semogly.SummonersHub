import type { BuildKindName } from '../types'
import { BUILD_KIND_LABELS } from './buildLabels'

/** Selos dos modos em que o monstro tem build própria (RTA, defesa de Siege ou Arena). */
export function BuildKindBadges({ kinds }: { kinds: BuildKindName[] | undefined }) {
  if (!kinds?.length) return null
  return (
    <>
      {kinds.map((kind) => (
        <span
          key={kind}
          className="ml-1 rounded border border-sky-400/40 px-1 text-[10px] font-medium text-sky-400 uppercase"
          title={`Tem build de ${BUILD_KIND_LABELS[kind]}`}
        >
          {BUILD_KIND_LABELS[kind]}
        </span>
      ))}
    </>
  )
}
