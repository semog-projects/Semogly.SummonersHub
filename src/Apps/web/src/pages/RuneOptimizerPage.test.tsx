import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { MonsterDetail, OptimizeRunesResponse, RuneDetail } from '@/features/accounts/types'
import { RuneOptimizerPage } from './RuneOptimizerPage'

const stats = { hp: 10000, attack: 700, defense: 600, speed: 150, criticalRate: 15, criticalDamage: 50, resistance: 15, accuracy: 0, effectiveHp: 27400 }

function rune(runeId: number, slot: number, set: string): RuneDetail {
  return {
    runeId,
    slot,
    stars: 6,
    isAncient: false,
    set,
    level: 15,
    rarity: 5,
    originalRarity: 5,
    mainStat: { type: slot === 2 ? 8 : 1, value: slot === 2 ? 42 : 2448 },
    innateStat: null,
    substats: [{ type: 8, value: 10, isGemmed: false, grindValue: 0 }],
    efficiency: 80,
    maxEfficiency: 80,
  }
}

const detail = {
  monsterId: 11,
  masterId: 19811,
  name: 'Lapis',
  element: 'Water',
  archetype: 'Attack',
  stars: 6,
  level: 40,
  awakenLevel: 1,
  base: stats,
  total: stats,
  activeSets: ['Violent', 'Will'],
  skills: [],
  runes: [rune(1, 2, 'Violent')],
  artifacts: [],
} as unknown as MonsterDetail

const lapis = { monsterId: 11, masterId: 19811, name: 'Lapis', element: 'Water' as const }
const tyron = { monsterId: 12, masterId: 11811, name: 'Tyron', element: 'Water' as const }

const response: OptimizeRunesResponse = {
  current: { score: 150, total: stats, activeSets: ['Violent', 'Will'], runes: [], artifacts: [] },
  builds: [
    {
      score: 180,
      total: { ...stats, speed: 180 },
      activeSets: ['Violent', 'Will'],
      runes: [
        { rune: rune(1, 2, 'Violent'), equippedOn: lapis, locked: false, tags: [] },
        { rune: rune(2, 4, 'Violent'), equippedOn: tyron, locked: false, tags: [] },
        { rune: rune(3, 6, 'Will'), equippedOn: null, locked: false, tags: [] },
      ],
      artifacts: [
        {
          artifact: {
            artifactId: 9,
            type: 'Element',
            element: 'Water',
            archetype: null,
            level: 15,
            rarity: 5,
            originalRarity: 5,
            mainStat: { type: 101, value: 100 },
            substats: [],
          },
          equippedOn: null,
          locked: false,
          tags: [],
        },
      ],
    },
  ],
}

function renderPage() {
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
    const body = url.includes('game-data') ? [] : init?.method === 'POST' ? response : detail
    return new Response(JSON.stringify(body), { status: 200 })
  })
  vi.stubGlobal('fetch', fetchMock)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/contas/1/monstros/11/otimizar']}>
        <Routes>
          <Route path="/contas/:wizardId/monstros/:monsterId/otimizar" element={<RuneOptimizerPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return fetchMock
}

afterEach(() => vi.unstubAllGlobals())

describe('RuneOptimizerPage', () => {
  it('preenche com a build atual, envia os critérios e mostra as builds', async () => {
    const fetchMock = renderPage()

    expect(await screen.findByRole('heading', { name: /Otimizar runas: Lapis/ })).toBeInTheDocument()
    expect(screen.getByLabelText('Set 1')).toHaveValue('Violent')
    expect(screen.getByLabelText('Set 2')).toHaveValue('Will')
    expect(screen.getByLabelText('Principal do slot 2')).toHaveValue('8')

    fireEvent.click(screen.getByRole('button', { name: 'HP' }))
    fireEvent.change(screen.getByLabelText('Mínimo de PRE'), { target: { value: '45' } })
    fireEvent.change(screen.getByLabelText('Máximo de VEL'), { target: { value: '220' } })
    fireEvent.change(screen.getByLabelText('Peso de VEL'), { target: { value: '2' } })
    fireEvent.click(screen.getByLabelText(/Trocar artefatos/))
    fireEvent.click(screen.getByRole('button', { name: 'Otimizar' }))

    const build = await screen.findByLabelText('Build 1')
    const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')!
    expect(post[0]).toBe('/api/accounts/1/monsters/11/optimize')
    expect(JSON.parse(post[1]!.body as string)).toMatchObject({
      sets: ['Violent', 'Will'],
      mainStats: { 2: [8] },
      focus: ['Speed', 'Hp'],
      minimums: [{ stat: 'Accuracy', min: 45 }],
      maximums: [{ stat: 'Speed', max: 220 }],
      weights: { Speed: 2 },
      optimizeArtifacts: true,
    })

    expect(build).toHaveTextContent('180(+30)')
    expect(build).toHaveTextContent('1 runa(s) de outros monstros')
    const runes = within(build).getByRole('list', { name: 'Runas da build' })
    expect(runes).toHaveTextContent('Já equipada')
    expect(runes).toHaveTextContent('Do inventário')
    expect(within(runes).getByRole('link', { name: 'Tyron' })).toHaveAttribute('href', '/contas/1/monstros/12')
    expect(within(build).getByRole('list', { name: 'Artefatos da build' })).toHaveTextContent('Elemento Água ATK +100 · do inventário')
  })

  it('bloqueia máximo menor que o mínimo', async () => {
    renderPage()
    await screen.findByLabelText('Set 1')

    fireEvent.change(screen.getByLabelText('Mínimo de VEL'), { target: { value: '200' } })
    fireEvent.change(screen.getByLabelText('Máximo de VEL'), { target: { value: '180' } })

    expect(screen.getByRole('alert')).toHaveTextContent('O máximo não pode ser menor que o mínimo.')
    expect(screen.getByRole('button', { name: 'Otimizar' })).toBeDisabled()
  })

  it('bloqueia sets que passam de 6 peças', async () => {
    renderPage()
    await screen.findByLabelText('Set 1')

    fireEvent.change(screen.getByLabelText('Set 2'), { target: { value: 'Swift' } })

    expect(screen.getByText(/8 de 6 peças/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Otimizar' })).toBeDisabled()
  })
})
