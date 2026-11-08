import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { AccountSummary, SnapshotComparison } from '@/features/accounts/types'
import { HistoryPage } from './HistoryPage'

function summary(version: number, monsters: number): AccountSummary {
  return {
    wizardId: 1,
    wizardName: 'Tester',
    wizardLevel: 50,
    snapshotId: `s${version}`,
    version,
    importedAt: '2026-11-0' + version + 'T12:00:00Z',
    exportedAt: null,
    monsters,
    runes: 100,
    artifacts: 10,
    craftItems: 0,
  }
}

const comparison: SnapshotComparison = {
  from: { version: 1, importedAt: '2026-11-01T12:00:00Z', exportedAt: null },
  to: { version: 2, importedAt: '2026-11-02T12:00:00Z', exportedAt: null },
  wizardLevel: { from: 50, to: 51 },
  monsters: { from: 2, to: 3 },
  addedMonsters: [{ monsterId: 99, masterId: 19811, name: 'Lapis', element: 'Water', stars: 6, level: 40 }],
  removedMonsters: [],
  progressedMonsters: [
    {
      monsterId: 12,
      masterId: 15105,
      name: 'Konamiya',
      element: 'Dark',
      stars: { from: 5, to: 6 },
      level: { from: 35, to: 40 },
      awakenLevel: { from: 1, to: 1 },
      speed: { from: 180, to: 214 },
    },
  ],
  runes: { total: { from: 100, to: 100 }, added: 3, removed: 3, upgraded: 2 },
  artifacts: { total: { from: 10, to: 10 }, added: 0, removed: 0, upgraded: 0 },
  averageRuneEfficiency: { from: 70, to: 72.5 },
  topNewRunes: [],
}

function renderPage(path: string, snapshots: AccountSummary[]) {
  const fetchMock = vi.fn(async (url: string) => {
    const body = url.includes('game-data') ? [] : url.includes('compare') ? comparison : snapshots
    return new Response(JSON.stringify(body), { status: 200 })
  })
  vi.stubGlobal('fetch', fetchMock)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/contas/:wizardId/historico" element={<HistoryPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return fetchMock
}

afterEach(() => vi.unstubAllGlobals())

describe('HistoryPage', () => {
  it('lista os imports e compara a penúltima com a última versão', async () => {
    const fetchMock = renderPage('/contas/1/historico', [summary(2, 3), summary(1, 2)])

    expect(await screen.findByRole('list', { name: 'Monstros novos' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/accounts/1/snapshots/compare?from=1&to=2', expect.anything())

    expect(within(screen.getByRole('table', { name: 'Imports' })).getAllByRole('row')).toHaveLength(3)
    expect(within(screen.getByRole('list', { name: 'Monstros novos' })).getByRole('link', { name: 'Lapis' })).toHaveAttribute(
      'href',
      '/contas/1/monstros/99',
    )

    const progress = within(screen.getByRole('table', { name: 'Evolução dos monstros' })).getByRole('row', { name: /Konamiya/ })
    expect(progress).toHaveTextContent('5 → 6(+1)')
    expect(progress).toHaveTextContent('180 → 214(+34)')
    expect(screen.getByText('70% →')).toBeInTheDocument()
  })

  it('com um import só, avisa que não há o que comparar', async () => {
    const fetchMock = renderPage('/contas/1/historico', [summary(1, 2)])

    expect(await screen.findByText(/Só há um import desta conta/)).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
