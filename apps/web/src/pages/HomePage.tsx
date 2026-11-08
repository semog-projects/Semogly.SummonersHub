import { ImportCard } from '@/features/import/ImportCard'
import { ApiStatusBadge } from '@/features/system/ApiStatusBadge'

export function HomePage() {
  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="font-heading text-2xl font-semibold">Sua conta de Summoners War</h1>
        <ApiStatusBadge />
      </div>

      <ImportCard />
    </div>
  )
}
