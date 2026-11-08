import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { MonsterDetail, MonsterListItem } from '@/features/accounts/types'
import { MonsterDetailPage } from './MonsterDetailPage'
import { MonsterListPage } from './MonsterListPage'

const stats = { hp: 9885, attack: 736, defense: 582, speed: 95, criticalRate: 15, criticalDamage: 50, resistance: 15, accuracy: 0, effectiveHp: 31405 }

const list: MonsterListItem[] = [
  { monsterId: 11, masterId: 19811, name: 'Lapis', element: 'Water', archetype: 'Attack', stars: 6, level: 40, awakenLevel: 1, total: { ...stats, speed: 151 }, activeSets: ['Vampire'], runes: 6, artifacts: 2, averageRuneEfficiency: 80.5 },
  { monsterId: 12, masterId: 15105, name: 'Konamiya', element: 'Dark', archetype: 'Support', stars: 6, level: 40, awakenLevel: 1, total: { ...stats, speed: 214 }, activeSets: ['Swift', 'Will'], runes: 6, artifacts: 2, averageRuneEfficiency: 80.5 },
  { monsterId: 13, masterId: 13715, name: 'Tyron', element: 'Water', archetype: 'Attack', stars: 6, level: 40, awakenLevel: 1, total: { ...stats, speed: 195 }, activeSets: ['Violent'], runes: 6, artifacts: 0, averageRuneEfficiency: 91.2 },
]

const detail: MonsterDetail = {
  monsterId: 11,
  masterId: 19811,
  name: 'Lapis',
  element: 'Water',
  archetype: 'Attack',
  stars: 6,
  level: 40,
  awakenLevel: 1,
  base: stats,
  total: { ...stats, hp: 14281, speed: 151 },
  activeSets: ['Vampire'],
  averageRuneEfficiency: 85.71,
  skills: [{ skillId: 1, level: 5 }],
  runes: [
    {
      runeId: 1,
      slot: 2,
      stars: 6,
      isAncient: true,
      set: 'Vampire',
      level: 15,
      rarity: 5,
      originalRarity: 5,
      mainStat: { type: 8, value: 42 },
      innateStat: { type: 9, value: 6 },
      substats: [
        { type: 2, value: 10, isGemmed: true, grindValue: 0 },
        { type: 4, value: 8, isGemmed: false, grindValue: 5 },
      ],
      efficiency: 85.71,
      maxEfficiency: 85.71,
    },
  ],
  artifacts: [
    {
      artifactId: 7,
      type: 'Element',
      element: 'Water',
      archetype: null,
      level: 15,
      rarity: 5,
      originalRarity: 5,
      mainStat: { type: 101, value: 100 },
      substats: [{ type: 221, value: 31, upgrades: 2 }],
    },
  ],
}

function renderAt(path: string, routePath: string, element: React.ReactNode, body: unknown) {
  vi.stubGlobal('fetch', vi.fn(async (url: string) => new Response(JSON.stringify(url.includes('game-data') ? [] : body), { status: 200 })))
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path={routePath} element={element} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const rowNames = () =>
  screen
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getByRole('link').textContent)

afterEach(() => vi.unstubAllGlobals())

describe('MonsterListPage', () => {
  it('lista do mais rápido para o mais lento e filtra pelo elemento da URL', async () => {
    renderAt('/contas/1/monstros?elemento=Water', '/contas/:wizardId/monstros', <MonsterListPage />, list)

    expect(await screen.findByText('Lapis')).toBeInTheDocument()
    expect(rowNames()).toEqual(['Tyron', 'Lapis'])
    expect(screen.getByText(/2 de 3 monstros/)).toBeInTheDocument()
  })

  it('reordena ao clicar no cabeçalho e inverte no segundo clique', async () => {
    renderAt('/contas/1/monstros', '/contas/:wizardId/monstros', <MonsterListPage />, list)
    await screen.findByText('Lapis')

    fireEvent.click(screen.getByRole('button', { name: 'Monstro' }))
    expect(rowNames()).toEqual(['Konamiya', 'Lapis', 'Tyron'])

    fireEvent.click(screen.getByRole('button', { name: /Monstro/ }))
    expect(rowNames()).toEqual(['Tyron', 'Lapis', 'Konamiya'])
  })

  it('filtra por set', async () => {
    renderAt('/contas/1/monstros', '/contas/:wizardId/monstros', <MonsterListPage />, list)
    await screen.findByText('Lapis')

    fireEvent.change(screen.getByLabelText('Set'), { target: { value: 'Swift' } })

    expect(rowNames()).toEqual(['Konamiya'])
  })
})

describe('MonsterDetailPage', () => {
  it('mostra atributos, runas com grind e gem, slots vazios e artefatos', async () => {
    renderAt('/contas/1/monstros/11', '/contas/:wizardId/monstros/:monsterId', <MonsterDetailPage />, detail)

    expect(await screen.findByRole('heading', { name: 'Lapis' })).toBeInTheDocument()

    const attributes = screen.getByRole('table', { name: 'Atributos' })
    const [hp, effectiveHp] = within(attributes).getAllByRole('row', { name: /HP/ })
    expect(hp).toHaveTextContent('9.88514.281+4.396')
    expect(effectiveHp).toHaveTextContent('HP Efet.')
    expect(screen.getByText(/Eficiência média das runas/)).toHaveTextContent('85,71%')

    const rune = screen.getByLabelText('Runa do slot 2')
    expect(rune).toHaveTextContent('Vampire (2) +15')
    expect(rune).toHaveTextContent('Ancient')
    expect(rune).toHaveTextContent('VEL +42')
    expect(rune).toHaveTextContent('Inata: Taxa Crít. +6%')
    expect(rune).toHaveTextContent('ATK +13%(5 grind)')
    expect(within(rune).getByText('gem')).toBeInTheDocument()
    expect(rune).toHaveTextContent('Eficiência 85,71%')
    expect(screen.getAllByText(/Slot \d vazio/)).toHaveLength(5)

    const artifact = screen.getByLabelText('Artefato de elemento água')
    expect(artifact).toHaveTextContent('ATK +100')
    expect(artifact).toHaveTextContent('Dano adicional de 31% da VEL')
  })
})
