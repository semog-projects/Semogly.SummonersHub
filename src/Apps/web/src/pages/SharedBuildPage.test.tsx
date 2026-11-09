import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { MonsterDetail } from '@/features/accounts/types'
import { SharedBuildPage } from './SharedBuildPage'

const stats = { hp: 9885, attack: 736, defense: 582, speed: 95, criticalRate: 15, criticalDamage: 50, resistance: 15, accuracy: 0, effectiveHp: 31405 }

const monster: MonsterDetail = {
  monsterId: 11,
  masterId: 19811,
  name: 'Lapis',
  element: 'Water',
  archetype: 'Attack',
  stars: 6,
  level: 40,
  awakenLevel: 1,
  base: stats,
  total: { ...stats, speed: 151 },
  activeSets: ['Vampire'],
  averageRuneEfficiency: null,
  skills: [],
  runes: [],
  artifacts: [],
}

function renderPage(status: number) {
  const fetchMock = vi.fn(async (url: string) =>
    url.includes('game-data')
      ? new Response('[]', { status: 200 })
      : status === 200
        ? new Response(JSON.stringify({ monster, importedAt: '2026-11-08T12:00:00Z' }), { status })
        : new Response(JSON.stringify({ title: 'SharedBuilds.NotFound', detail: 'x' }), { status }),
  )
  vi.stubGlobal('fetch', fetchMock)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/b/abcdefghijklmnopqrstuv']}>
        <Routes>
          <Route path="/b/:token" element={<SharedBuildPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return fetchMock
}

afterEach(() => vi.unstubAllGlobals())

describe('SharedBuildPage', () => {
  it('mostra a build sem as ações da conta', async () => {
    const fetchMock = renderPage(200)

    expect(await screen.findByRole('heading', { name: 'Lapis' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/shared/abcdefghijklmnopqrstuv', expect.anything())
    expect(screen.getByText(/Build compartilhada · import de/)).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /Otimizar runas/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /Calcular dano/ })).not.toBeInTheDocument()
  })

  it('avisa quando o link não existe ou foi revogado', async () => {
    renderPage(404)

    expect(await screen.findByRole('alert')).toHaveTextContent('não existe ou foi revogado')
  })
})
