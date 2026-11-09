import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import type { ManualItems } from '@/features/accounts/manual/useManualItems'
import { ManualItemsPage } from './ManualItemsPage'

const lapis = { com2usId: 19811, name: 'Lapis', element: 'Water', awakenLevel: 1, naturalStars: 4, imageUrl: null }

function renderPage() {
  const items: ManualItems = {
    monsters: [{ gameId: -1, masterId: 19811, name: 'Lapis', element: 'Water', awakenLevel: 1, createdAt: '2026-11-09T00:00:00Z' }],
    craftItems: [{ gameId: -2, type: 'EnchantedGem', set: 'Violent', stat: 8, rarity: 5, amount: 2, createdAt: '2026-11-09T00:00:00Z' }],
  }
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
    if (url.includes('/monsters/search')) return new Response(JSON.stringify([lapis]), { status: 200 })
    if (init?.method === 'DELETE') return new Response(null, { status: 204 })
    if (init?.method === 'POST') return new Response(JSON.stringify({ gameId: -3 }), { status: 200 })
    return new Response(JSON.stringify(items), { status: 200 })
  })
  vi.stubGlobal('fetch', fetchMock)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/contas/1/manuais']}>
        <Routes>
          <Route path="/contas/:wizardId/manuais" element={<ManualItemsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return fetchMock
}

function bodyOf(fetchMock: ReturnType<typeof renderPage>, urlPart: string) {
  const call = fetchMock.mock.calls.find(([url, init]) => init?.method === 'POST' && String(url).includes(urlPart))!
  return JSON.parse(call[1]!.body as string)
}

afterEach(() => vi.unstubAllGlobals())

describe('ManualItemsPage', () => {
  it('lista os itens manuais, com link para o monstro', async () => {
    renderPage()

    const monsters = await screen.findByRole('list', { name: 'Monstros manuais' })
    expect(within(monsters).getByRole('link', { name: 'Lapis' })).toHaveAttribute('href', '/contas/1/monstros/-1')
    expect(screen.getByRole('list', { name: 'Gems e grinds manuais' })).toHaveTextContent('Gem Violent · VEL · Lendária×2')
  })

  it('busca no bestiário e adiciona o monstro', async () => {
    const fetchMock = renderPage()

    fireEvent.change(screen.getByLabelText('Buscar monstro'), { target: { value: 'lap' } })
    fireEvent.click(await screen.findByRole('button', { name: /Adicionar Lapis/ }))

    await waitFor(() => expect(bodyOf(fetchMock, 'manual-items/monsters')).toEqual({ masterId: 19811 }))
    expect(fetchMock.mock.calls.some(([url]) => String(url) === '/api/game-data/monsters/search?name=lap')).toBe(true)
  })

  it('cria grind só com atributos que aceitam grind e exclui itens', async () => {
    const fetchMock = renderPage()

    fireEvent.change(screen.getByLabelText('Atributo'), { target: { value: '9' } })
    fireEvent.change(screen.getByLabelText('Tipo'), { target: { value: 'Grindstone' } })
    expect(screen.getByLabelText('Atributo')).toHaveValue('8') // Taxa Crít. não tem grind: volta para VEL
    fireEvent.change(screen.getByLabelText('Quantidade'), { target: { value: '3' } })
    fireEvent.click(screen.getByRole('button', { name: 'Adicionar' }))

    await waitFor(() =>
      expect(bodyOf(fetchMock, 'craft-items')).toEqual({ type: 'Grindstone', set: 'Energy', stat: 8, rarity: 5, amount: 3 }),
    )

    fireEvent.click(await screen.findByRole('button', { name: 'Excluir gem Violent VEL' }))
    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([url, init]) => init?.method === 'DELETE' && String(url) === '/api/accounts/1/manual-items/-2')).toBe(true),
    )
  })
})
