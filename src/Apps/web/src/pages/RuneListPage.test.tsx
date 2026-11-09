import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { RuneListItem } from '@/features/accounts/types'
import { RUNE_PAGE_SIZE } from '@/features/accounts/runes/runeFilters'
import { RuneListPage } from './RuneListPage'

function rune(runeId: number, efficiency: number, equipped: boolean): RuneListItem {
  return {
    rune: {
      runeId,
      slot: (runeId % 6) + 1,
      stars: 6,
      isAncient: false,
      set: runeId % 2 === 0 ? 'Swift' : 'Violent',
      level: 15,
      rarity: 5,
      originalRarity: 5,
      mainStat: { type: 4, value: 63 },
      innateStat: null,
      substats: [{ type: 8, value: runeId, isGemmed: false, grindValue: 0 }],
      efficiency,
      maxEfficiency: efficiency,
    },
    equippedOn: equipped ? { monsterId: 11, masterId: 19811, name: 'Lapis', element: 'Water' } : null,
    locked: false,
    tags: [],
  }
}

function renderPage(path: string, runes: RuneListItem[]) {
  const fetchMock = vi.fn(async (_url: string, init?: RequestInit) =>
    init?.method === 'PATCH' ? new Response(null, { status: 204 }) : new Response(JSON.stringify(runes), { status: 200 }),
  )
  vi.stubGlobal('fetch', fetchMock)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/contas/:wizardId/runas" element={<RuneListPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return fetchMock
}

const bodyRows = () => screen.getAllByRole('row').slice(1)

afterEach(() => vi.unstubAllGlobals())

describe('RuneListPage', () => {
  it('lista pela eficiência e liga a runa ao monstro que a equipa', async () => {
    renderPage('/contas/1/runas', [rune(1, 70.5, true), rune(2, 92.86, false)])

    await screen.findByText(/2 de 2 runas/)
    const [first, second] = bodyRows()
    expect(first).toHaveTextContent('Swift (3) +15')
    expect(first).toHaveTextContent('92,86%')
    expect(first).toHaveTextContent('Inventário')
    expect(within(second).getByRole('link', { name: 'Lapis' })).toHaveAttribute('href', '/contas/1/monstros/11')
  })

  it('aplica o filtro de local da URL e o de set pelo combo', async () => {
    renderPage('/contas/1/runas?local=equipada', [rune(1, 70, true), rune(2, 90, false), rune(3, 80, true)])
    await screen.findByText(/2 de 3 runas/)

    expect(bodyRows()).toHaveLength(2)
    expect(screen.getByText(/2 de 3 runas/)).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Set'), { target: { value: 'Swift' } })
    expect(screen.getByText('Nenhuma runa com esses filtros.')).toBeInTheDocument()
  })

  it('bloqueia uma runa pelo cadeado e as filtradas em lote', async () => {
    const fetchMock = renderPage('/contas/1/runas?local=equipada', [rune(1, 70, true), rune(2, 90, false), rune(3, 80, true)])
    await screen.findByText(/2 de 3 runas/)
    const patches = () =>
      fetchMock.mock.calls.filter(([, init]) => init?.method === 'PATCH').map(([url, init]) => [url, JSON.parse(init!.body as string)])

    fireEvent.click(within(bodyRows()[0]).getByRole('button', { name: 'Bloquear runa' }))
    await vi.waitFor(() => expect(patches()).toHaveLength(1))
    expect(patches()[0]).toEqual(['/api/accounts/1/inventory-marks', { kind: 'Rune', itemIds: [3], locked: true }])

    fireEvent.change(screen.getByLabelText('Tag'), { target: { value: ' RTA ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar tag' }))
    await vi.waitFor(() => expect(patches()).toHaveLength(2))
    expect(patches()[1][1]).toEqual({ kind: 'Rune', itemIds: [3, 1], addTags: ['RTA'] })
  })

  it('pagina a tabela', async () => {
    const many = Array.from({ length: RUNE_PAGE_SIZE + 5 }, (_, i) => rune(i + 1, 50, false))
    renderPage('/contas/1/runas', many)
    await screen.findByText(/105 de 105 runas/)

    expect(bodyRows()).toHaveLength(RUNE_PAGE_SIZE)
    fireEvent.click(screen.getByRole('button', { name: /Mostrar mais \(5 restantes\)/ }))
    expect(bodyRows()).toHaveLength(RUNE_PAGE_SIZE + 5)
  })
})
