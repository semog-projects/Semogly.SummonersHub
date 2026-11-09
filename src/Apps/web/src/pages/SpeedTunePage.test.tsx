import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { MonsterListItem } from '@/features/accounts/types'
import { SpeedTunePage } from './SpeedTunePage'

const stats = { hp: 10000, attack: 700, defense: 600, speed: 100, criticalRate: 15, criticalDamage: 50, resistance: 15, accuracy: 0, effectiveHp: 30000 }

const list: MonsterListItem[] = [
  { monsterId: 1, masterId: 11513, name: 'Bernard', element: 'Wind', archetype: 'Support', stars: 6, level: 40, awakenLevel: 1, total: { ...stats, speed: 250 }, activeSets: ['Swift'], runes: 6, artifacts: 2, averageRuneEfficiency: 90 },
  { monsterId: 2, masterId: 11014, name: 'Belladeon', element: 'Light', archetype: 'Defense', stars: 6, level: 40, awakenLevel: 1, total: { ...stats, speed: 200 }, activeSets: ['Swift'], runes: 6, artifacts: 2, averageRuneEfficiency: 85 },
]

// Detalhe só com o que a página usa (VEL base e total)
const detail = (m: MonsterListItem, baseSpeed: number) => ({ ...m, base: { ...stats, speed: baseSpeed }, skills: [], runes: [], artifacts: [] })

function renderPage() {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string) => {
      const body = url.endsWith('/dungeons')
        ? [{ dungeon: 'GiantsKeep', floor: 12, bossSpeed: 121, maxSpeed: 169, bossResistance: 70, maxResistance: 70 }]
        : url.endsWith('/monsters/1')
          ? detail(list[0], 111)
          : url.endsWith('/monsters/2')
            ? detail(list[1], 100)
            : list
      return new Response(JSON.stringify(body), { status: 200 })
    }),
  )
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/contas/1/speed-tune']}>
        <Routes>
          <Route path="/contas/:wizardId/speed-tune" element={<SpeedTunePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

afterEach(() => vi.unstubAllGlobals())

describe('SpeedTunePage', () => {
  it('calcula a VEL de combate, os ticks e compara com o inimigo da dungeon', async () => {
    renderPage()
    await screen.findByLabelText('Monstro 1')

    fireEvent.change(screen.getByLabelText('Monstro 1'), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText('Monstro 2'), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText('Líder de VEL (%)'), { target: { value: '24' } })
    await vi.waitFor(() => expect(screen.getByLabelText('Dungeon')).toHaveTextContent("Giant's Keep B12"))
    fireEvent.change(screen.getByLabelText('Dungeon'), { target: { value: 'GiantsKeep' } })

    const table = await screen.findByRole('table', { name: 'Velocidade do time' })
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(3))

    // Bernard: 250 + ceil(111 × 39%) = 294 → 5 ticks; Belladeon: 200 + 39 = 239 → 6 ticks; inimigo 169 → 9 ticks
    const [bernard, belladeon] = within(table).getAllByRole('row').slice(1)
    expect(bernard).toHaveTextContent('Bernard1112502945')
    expect(bernard).toHaveTextContent('Age antes')
    expect(belladeon).toHaveTextContent('Belladeon1002002396')

    const turns = within(screen.getByRole('region', { name: 'Primeiros turnos' })).getAllByRole('listitem')
    expect(turns[0]).toHaveTextContent('tick 5 · Bernard')
    expect(turns[1]).toHaveTextContent('tick 6 · Belladeon')
    expect(turns.some((t) => t.textContent?.includes('Inimigo mais rápido'))).toBe(true)
  })
})
