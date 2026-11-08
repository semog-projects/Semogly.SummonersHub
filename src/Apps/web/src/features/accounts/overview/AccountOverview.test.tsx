import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import type { AccountOverview as Overview } from '../types'
import { AccountOverview } from './AccountOverview'

const overview: Overview = {
  wizard: {
    wizardId: 12345678,
    name: 'TestWizard',
    level: 50,
    serverId: 4,
    version: 3,
    importedAt: '2026-11-07T23:30:00Z',
    exportedAt: '2026-11-07T23:25:00Z',
  },
  monsters: 1234,
  runes: 829,
  artifacts: 184,
  monstersByStars: [1, 2, 3, 4, 5, 6].map((key) => ({ key, count: key === 6 ? 40 : key })),
  monstersByElement: [
    { key: 'Water', count: 10 },
    { key: 'Fire', count: 0 },
    { key: 'Wind', count: 5 },
    { key: 'Light', count: 1 },
    { key: 'Dark', count: 2 },
    { key: 'Pure', count: 0 },
  ],
  monstersByAwakening: [
    { key: 0, count: 3 },
    { key: 1, count: 70 },
    { key: 2, count: 6 },
  ],
  speedRanking: [
    { monsterId: 1, masterId: 15105, name: 'Konamiya', element: 'Dark', stars: 6, level: 40, baseSpeed: 101, totalSpeed: 214, activeSets: ['Swift', 'Will'] },
    { monsterId: 2, masterId: 19811, name: 'Lapis', element: 'Water', stars: 6, level: 40, baseSpeed: 95, totalSpeed: 151, activeSets: ['Vampire'] },
  ],
}

function renderOverview() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <AccountOverview overview={overview} />
    </QueryClientProvider>,
  )
}

afterEach(() => vi.unstubAllGlobals())

describe('AccountOverview', () => {
  beforeEach(() => {
    // Bestiário para os ícones do ranking
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify([{ com2usId: 19811, name: 'Lapis', imageUrl: 'https://img/lapis.png' }]), { status: 200 }),
      ),
    )
  })

  it('mostra o invocador e os totais', () => {
    renderOverview()

    expect(screen.getByRole('heading', { name: 'TestWizard' })).toBeInTheDocument()
    expect(screen.getByText(/Nível 50 · servidor 4/)).toBeInTheDocument()
    expect(screen.getByText(/versão 3/)).toBeInTheDocument()
    expect(screen.getByText('1.234')).toBeInTheDocument()
  })

  it('mostra as distribuições com rótulos em pt-BR', () => {
    renderOverview()

    const stars = within(screen.getByRole('list', { name: 'Por estrelas' })).getAllByRole('listitem')
    expect(stars[0]).toHaveTextContent('6★40')

    const elements = within(screen.getByRole('list', { name: 'Por elemento' }))
    expect(elements.getByText('Água').parentElement).toHaveTextContent('10')
    expect(elements.getByText('Trevas')).toBeInTheDocument()

    expect(within(screen.getByRole('list', { name: 'Por despertar' })).getByText('2º despertar')).toBeInTheDocument()
  })

  it('lista o ranking de velocidade com o bônus sobre a base', async () => {
    renderOverview()

    const rows = screen.getAllByRole('row').slice(1)
    expect(rows[0]).toHaveTextContent('Konamiya')
    expect(rows[0]).toHaveTextContent('214')
    expect(rows[0]).toHaveTextContent('+113')
    expect(rows[0]).toHaveTextContent('Swift')
    expect(rows[1]).toHaveTextContent('Lapis')
    expect(await within(rows[1]).findByRole('presentation')).toHaveAttribute('src', 'https://img/lapis.png')
  })
})
