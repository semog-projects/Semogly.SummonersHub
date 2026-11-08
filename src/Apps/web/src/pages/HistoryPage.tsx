import { useParams, useSearchParams } from 'react-router'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { SnapshotComparisonView } from '@/features/accounts/history/SnapshotComparisonView'
import { selectVersions } from '@/features/accounts/history/versionSelection'
import { SelectFilter } from '@/features/accounts/shared/filterControls'
import type { AccountSummary } from '@/features/accounts/types'
import { useSnapshotComparison, useSnapshots } from '@/features/accounts/useAccounts'
import { formatDateTime, formatNumber } from '@/lib/format'
import { QueryStatus } from './QueryStatus'

export function HistoryPage() {
  const wizardId = Number(useParams().wizardId)
  const query = useSnapshots(wizardId)

  return (
    <QueryStatus query={query} notFound="Conta não encontrada." loading="Carregando histórico…">
      {(snapshots) => <History wizardId={wizardId} snapshots={snapshots} />}
    </QueryStatus>
  )
}

function History({ wizardId, snapshots }: { wizardId: number; snapshots: AccountSummary[] }) {
  const [searchParams, setSearchParams] = useSearchParams()
  const { from, to } = selectVersions(
    searchParams,
    snapshots.map((s) => s.version),
  )
  const comparison = useSnapshotComparison(wizardId, from, to)

  const options = snapshots.map((s) => ({ value: s.version, label: `Versão ${s.version} · ${formatDateTime(s.importedAt)}` }))
  const setVersions = (next: { from: number | null; to: number | null }) => {
    const params = new URLSearchParams()
    if (next.from !== null) params.set('de', String(next.from))
    if (next.to !== null) params.set('para', String(next.to))
    setSearchParams(params, { replace: true })
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Imports</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table className="w-full text-sm" aria-label="Imports">
              <thead className="text-left text-xs text-muted-foreground">
                <tr className="border-b">
                  <th scope="col" className="py-2 pr-3 font-medium">
                    Versão
                  </th>
                  <th scope="col" className="py-2 pr-3 font-medium">
                    Importado em
                  </th>
                  <th scope="col" className="py-2 pr-3 font-medium">
                    Exportado em
                  </th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">
                    Nível
                  </th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">
                    Monstros
                  </th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">
                    Runas
                  </th>
                  <th scope="col" className="py-2 pr-3 text-right font-medium">
                    Artefatos
                  </th>
                </tr>
              </thead>
              <tbody>
                {snapshots.map((snapshot) => (
                  <tr key={snapshot.version} className="border-b last:border-0">
                    <td className="py-2 pr-3 font-medium">{snapshot.version}</td>
                    <td className="py-2 pr-3">{formatDateTime(snapshot.importedAt)}</td>
                    <td className="py-2 pr-3 text-muted-foreground">
                      {snapshot.exportedAt ? formatDateTime(snapshot.exportedAt) : '—'}
                    </td>
                    <td className="py-2 pr-3 text-right tabular-nums">{snapshot.wizardLevel}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{formatNumber(snapshot.monsters)}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{formatNumber(snapshot.runes)}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{formatNumber(snapshot.artifacts)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {snapshots.length < 2 ? (
        <p className="text-sm text-muted-foreground">
          Só há um import desta conta. Importe um export mais novo para comparar as versões.
        </p>
      ) : (
        <section aria-label="Comparação" className="space-y-4">
          <div className="flex flex-wrap items-center gap-3 text-sm">
            <span>Comparar</span>
            <SelectFilter
              label="Versão de referência"
              allLabel="Escolha a versão"
              value={from}
              options={options}
              onChange={(version) => setVersions({ from: version, to })}
            />
            <span>com</span>
            <SelectFilter
              label="Versão comparada"
              allLabel="Escolha a versão"
              value={to}
              options={options}
              onChange={(version) => setVersions({ from, to: version })}
            />
          </div>

          {from === null || to === null ? (
            <p className="text-sm text-muted-foreground">Escolha duas versões diferentes.</p>
          ) : (
            <QueryStatus query={comparison} notFound="Versão não encontrada." loading="Comparando versões…">
              {(data) => <SnapshotComparisonView wizardId={wizardId} comparison={data} />}
            </QueryStatus>
          )}
        </section>
      )}
    </div>
  )
}
