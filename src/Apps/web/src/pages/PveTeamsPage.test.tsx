import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { PveTeamCheck } from '@/features/accounts/types'
import { PveTeamsPage } from './PveTeamsPage'

const total = { hp: 10000, attack: 800, defense: 600, speed: 150, criticalRate: 15, criticalDamage: 50, resistance: 15, accuracy: 30 }

const teams: PveTeamCheck[] = [
  {
    id: 'gb',
    dungeon: 'GiantsKeep',
    name: 'Clássico F2P',
    notes: 'O Giant é de Fogo.',
    readySlots: 1,
    isReady: false,
    slots: [
      {
        monsterName: 'Bernard',
        element: 'Wind',
        role: 'Barra de ataque',
        requirements: [{ stat: 'Speed', min: 180 }],
        status: 'BelowRequirements',
        monster: { monsterId: 5, masterId: 11513, name: 'Bernard', element: 'Wind', stars: 6, level: 40, total },
        unmet: [{ stat: 'Speed', required: 180, current: 150 }],
      },
      {
        monsterName: 'Veromos',
        element: 'Dark',
        role: 'Cleanse',
        requirements: [],
        status: 'Ready',
        monster: { monsterId: 6, masterId: 17115, name: 'Veromos', element: 'Dark', stars: 6, level: 40, total },
        unmet: [],
      },
      {
        monsterName: 'Belladeon',
        element: 'Light',
        role: 'Quebra de DEF',
        requirements: [{ stat: 'Accuracy', min: 45 }],
        status: 'Missing',
        monster: null,
        unmet: [],
      },
    ],
  },
  {
    id: 'pc',
    dungeon: 'PunishersCrypt',
    name: 'Núcleo Verdehile',
    notes: null,
    readySlots: 1,
    isReady: true,
    slots: [
      {
        monsterName: 'Verdehile',
        element: 'Fire',
        role: 'Barra em crítico',
        requirements: [{ stat: 'CriticalRate', min: 85 }],
        status: 'Ready',
        monster: { monsterId: 7, masterId: 14712, name: 'Verdehile', element: 'Fire', stars: 6, level: 40, total },
        unmet: [],
      },
    ],
  },
]

function renderPage() {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string) => new Response(JSON.stringify(url.includes('game-data') ? [] : teams), { status: 200 })),
  )
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/contas/1/times']}>
        <Routes>
          <Route path="/contas/:wizardId/times" element={<PveTeamsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

afterEach(() => vi.unstubAllGlobals())

describe('PveTeamsPage', () => {
  it('agrupa por dungeon e mostra o estado de cada vaga', async () => {
    renderPage()

    const giants = await screen.findByRole('region', { name: "Giant's Keep" })
    expect(screen.getByRole('region', { name: "Punisher's Crypt" })).toHaveTextContent('Pronto')
    expect(screen.getByText(/1 de 2 times prontos/)).toBeInTheDocument()

    expect(within(giants).getByText('1 de 3 prontos')).toBeInTheDocument()

    const bernard = within(giants).getByRole('listitem', { name: 'Bernard (Vento)' })
    expect(bernard).toHaveTextContent('mín. VEL 180')
    expect(bernard).toHaveTextContent('Falta: VEL 150 de VEL 180')
    expect(within(bernard).getByRole('link', { name: 'Bernard' })).toHaveAttribute('href', '/contas/1/monstros/5')

    const belladeon = within(giants).getByRole('listitem', { name: 'Belladeon (Luz)' })
    expect(belladeon).toHaveTextContent('mín. PRE 45%')
    expect(belladeon).toHaveTextContent('Não tem')
    expect(within(belladeon).queryByRole('link')).not.toBeInTheDocument()
  })
})
