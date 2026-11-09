import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { BestiaryMonster } from '@/features/bestiary/useBestiary'
import type { MonsterDefinition } from '@/features/game-data/useMonsterDefinitions'
import { BestiaryMonsterPage } from './BestiaryMonsterPage'
import { BestiaryPage } from './BestiaryPage'

const lapis: MonsterDefinition = {
  com2usId: 19811,
  familyId: 19800,
  name: 'Lapis',
  element: 'Water',
  archetype: 'Attack',
  naturalStars: 3,
  awakenLevel: 1,
  isHomunculus: false,
  imageUrl: 'https://example.test/lapis.png',
}

const list: MonsterDefinition[] = [
  lapis,
  { ...lapis, com2usId: 19812, name: 'Tyron', element: 'Fire' },
  { ...lapis, com2usId: 19801, name: 'Fairy', awakenLevel: 0 },
]

const detail: BestiaryMonster = {
  monster: lapis,
  maxLevelStats: { hp: 9885, attack: 736, defense: 582, speed: 101, resistance: 15, accuracy: 0, criticalRate: 15, criticalDamage: 50 },
  leaderSkill: { attribute: 'Attack Power', amount: 25, area: 'General', element: null },
  awakenBonus: 'Increases the Critical Rate',
  sources: ['Water Scroll'],
  skills: [
    {
      id: 1,
      name: 'Magic Arrow',
      slot: 1,
      description: 'Attacks with a magic arrow.',
      cooldown: null,
      hits: 2,
      passive: false,
      aoe: false,
      maxLevel: 5,
      formulaText: '{ATK}*2.0',
      levelProgress: ['Damage +10%'],
      iconUrl: null,
    },
    {
      id: 2,
      name: 'Water Bomb',
      slot: 3,
      description: null,
      cooldown: 4,
      hits: 1,
      passive: false,
      aoe: true,
      maxLevel: 3,
      formulaText: null,
      levelProgress: [],
      iconUrl: null,
    },
  ],
  family: [{ ...lapis, com2usId: 19801, name: 'Fairy', awakenLevel: 0 }],
}

function renderAt(path: string, body: unknown) {
  vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(body), { status: 200 })))
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/bestiario" element={<BestiaryPage />} />
          <Route path="/bestiario/:com2usId" element={<BestiaryMonsterPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

afterEach(() => vi.unstubAllGlobals())

describe('BestiaryPage', () => {
  it('lista os monstros e filtra pelos parâmetros da URL', async () => {
    renderAt('/bestiario?elemento=Water', list)

    const items = await screen.findAllByRole('listitem')
    expect(items.map((item) => within(item).getByRole('link').textContent)).toEqual([
      expect.stringContaining('Lapis'),
      expect.stringContaining('Fairy'),
    ])
    expect(screen.getByText('2 monstros')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Buscar monstro'), { target: { value: 'fai' } })
    expect(screen.getByText('1 monstro')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Fairy/ })).toHaveAttribute('href', '/bestiario/19801')
  })
})

describe('BestiaryMonsterPage', () => {
  it('mostra atributos, líder, skills, onde obter e família', async () => {
    renderAt('/bestiario/19811', detail)

    expect(await screen.findByRole('heading', { name: 'Lapis' })).toBeInTheDocument()
    expect(screen.getByText(/Aumenta o ATK dos aliados em 25%/)).toBeInTheDocument()
    expect(screen.getByText('Increases the Critical Rate')).toBeInTheDocument()

    const stats = screen.getByRole('table', { name: 'Atributos' })
    expect(within(stats).getByRole('row', { name: /VEL/ })).toHaveTextContent('101')

    const first = screen.getByRole('region', { name: 'Skill 1: Magic Arrow' })
    expect(first).toHaveTextContent('{ATK}*2.0 × 2 golpes')
    expect(first).toHaveTextContent('Skill-ups (1): Damage +10%')
    const third = screen.getByRole('region', { name: 'Skill 3: Water Bomb' })
    expect(third).toHaveTextContent('Recarga: 4 turnos')
    expect(third).toHaveTextContent('Em área')

    expect(screen.getByRole('list', { name: 'Onde obter' })).toHaveTextContent('Water Scroll')
    expect(within(screen.getByRole('list', { name: 'Família' })).getByRole('link')).toHaveAttribute('href', '/bestiario/19801')
  })
})
