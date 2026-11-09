import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { PveTeamCheck } from '@/features/accounts/types'
import { PveTeamsPage } from './PveTeamsPage'

const total = { hp: 10000, attack: 800, defense: 600, speed: 150, criticalRate: 15, criticalDamage: 50, resistance: 15, accuracy: 30, effectiveHp: 30000 }

const teams: PveTeamCheck[] = [
  {
    id: 'gb',
    dungeon: 'GiantsKeep',
    dungeonStats: { floor: 12, bossResistance: 70, maxResistance: 70, bossSpeed: 121, maxSpeed: 169 },
    name: 'Clássico F2P',
    notes: 'O Giant é de Fogo.',
    readySlots: 1,
    isReady: false,
    slots: [
      {
        monsterName: 'Bernard',
        element: 'Wind',
        role: 'Barra de ataque',
        debuffer: false,
        requirements: [{ stat: 'Speed', min: 180 }],
        status: 'BelowRequirements',
        monster: { monsterId: 5, masterId: 11513, name: 'Bernard', element: 'Wind', stars: 6, level: 40, total },
        unmet: [{ stat: 'Speed', required: 180, current: 150 }],
      },
      {
        monsterName: 'Veromos',
        element: 'Dark',
        role: 'Cleanse',
        debuffer: false,
        requirements: [],
        status: 'Ready',
        monster: { monsterId: 6, masterId: 17115, name: 'Veromos', element: 'Dark', stars: 6, level: 40, total },
        unmet: [],
      },
      {
        monsterName: 'Belladeon',
        element: 'Light',
        role: 'Quebra de DEF',
        debuffer: true,
        requirements: [{ stat: 'Accuracy', min: 55 }],
        status: 'Missing',
        monster: null,
        unmet: [],
      },
    ],
  },
  {
    id: 'pc',
    dungeon: 'PunishersCrypt',
    dungeonStats: null,
    name: 'Núcleo Verdehile',
    notes: null,
    readySlots: 1,
    isReady: true,
    slots: [
      {
        monsterName: 'Verdehile',
        element: 'Fire',
        role: 'Barra em crítico',
        debuffer: false,
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
    expect(belladeon).toHaveTextContent('mín. PRE 55% (PRE = RES do boss − 15)')
    expect(giants).toHaveTextContent('B12: RES do boss 70%')
    expect(belladeon).toHaveTextContent('Não tem')
    expect(within(belladeon).queryByRole('link')).not.toBeInTheDocument()
  })
})
