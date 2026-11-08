import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { CraftSuggestionGroup, RuneDetail } from '@/features/accounts/types'
import { CraftSuggestionsPage } from './CraftSuggestionsPage'

const rune: RuneDetail = {
  runeId: 1,
  slot: 4,
  stars: 6,
  isAncient: false,
  set: 'Vampire',
  level: 15,
  rarity: 5,
  originalRarity: 5,
  mainStat: { type: 10, value: 80 },
  innateStat: null,
  substats: [{ type: 8, value: 15, isGemmed: false, grindValue: 0 }],
  efficiency: 80,
  maxEfficiency: 80,
}

const groups: CraftSuggestionGroup[] = [
  {
    type: 'Grindstone',
    set: 'Vampire',
    stat: 8,
    rarity: 5,
    amount: 2,
    range: { min: 4, max: 5 },
    suggestions: [
      {
        rune,
        equippedOn: { monsterId: 11, masterId: 19811, name: 'Lapis', element: 'Water' },
        targetStat: 8,
        currentValue: 15,
        minValue: 19,
        maxValue: 20,
        minEfficiencyGain: 4.76,
        maxEfficiencyGain: 5.95,
      },
    ],
  },
  { type: 'AncientGem', set: 'Immemorial', stat: 4, rarity: 5, amount: 1, range: null, suggestions: [] },
]

function renderPage(path: string) {
  const fetchMock = vi.fn(async () => new Response(JSON.stringify(groups), { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/contas/:wizardId/gems-grinds" element={<CraftSuggestionsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return fetchMock
}

afterEach(() => vi.unstubAllGlobals())

describe('CraftSuggestionsPage', () => {
  it('mostra o item, a faixa e as runas com o ganho de eficiência', async () => {
    renderPage('/contas/1/gems-grinds')

    const grind = await screen.findByRole('generic', { name: 'Grind Vampire VEL' })
    expect(grind).toHaveTextContent('×2')
    expect(grind).toHaveTextContent('Grind de 4 a 5')

    const row = within(grind).getAllByRole('row')[1]
    expect(row).toHaveTextContent('Vampire (4) +15')
    expect(row).toHaveTextContent('VEL +15 → VEL +19 a 20')
    expect(row).toHaveTextContent('+4,76 pp a +5,95 pp')
    expect(row).toHaveTextContent('80% → 85,95%')
    expect(within(row).getByRole('link', { name: 'Lapis' })).toHaveAttribute('href', '/contas/1/monstros/11')

    expect(screen.getByRole('generic', { name: 'Gem Ancient qualquer set ATK %' })).toHaveTextContent('ainda não têm sugestão')
  })

  it('por padrão considera só runas equipadas e alterna para incluir o inventário', async () => {
    const fetchMock = renderPage('/contas/1/gems-grinds')
    await screen.findByText(/Grind de 4 a 5/)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/accounts/1/craft-suggestions?equippedOnly=true', expect.anything())

    fireEvent.click(screen.getByLabelText('Incluir runas do inventário'))

    await vi.waitFor(() =>
      expect(fetchMock).toHaveBeenLastCalledWith('/api/accounts/1/craft-suggestions?equippedOnly=false', expect.anything()),
    )
  })
})
