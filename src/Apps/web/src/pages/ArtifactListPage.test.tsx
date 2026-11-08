import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { ArtifactListItem } from '@/features/accounts/types'
import { ArtifactListPage } from './ArtifactListPage'

const artifacts: ArtifactListItem[] = [
  {
    artifact: {
      artifactId: 1,
      type: 'Element',
      element: 'Water',
      archetype: null,
      level: 15,
      rarity: 5,
      originalRarity: 5,
      mainStat: { type: 101, value: 100 },
      substats: [{ type: 221, value: 25, upgrades: 1 }],
    },
    equippedOn: { monsterId: 11, masterId: 19811, name: 'Lapis', element: 'Water' },
  },
  {
    artifact: {
      artifactId: 2,
      type: 'Archetype',
      element: null,
      archetype: 'Attack',
      level: 15,
      rarity: 4,
      originalRarity: 4,
      mainStat: { type: 100, value: 1500 },
      substats: [{ type: 221, value: 31, upgrades: 2 }],
    },
    equippedOn: null,
  },
]

function renderPage(path: string) {
  vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(artifacts), { status: 200 })))
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/contas/:wizardId/artefatos" element={<ArtifactListPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const bodyRows = () => screen.getAllByRole('row').slice(1)

afterEach(() => vi.unstubAllGlobals())

describe('ArtifactListPage', () => {
  it('lista com principal, subs e o monstro que equipa', async () => {
    renderPage('/contas/1/artefatos')
    await screen.findByText(/2 de 2 artefatos/)

    const [first, second] = bodyRows()
    expect(first).toHaveTextContent('Elemento Água')
    expect(first).toHaveTextContent('ATK +100')
    expect(first).toHaveTextContent('Dano adicional de 25% da VEL')
    expect(within(first).getByRole('link', { name: 'Lapis' })).toHaveAttribute('href', '/contas/1/monstros/11')
    expect(second).toHaveTextContent('Arquétipo Ataque')
    expect(second).toHaveTextContent('Inventário')
  })

  it('escolher uma sub ordena pelo valor dela', async () => {
    renderPage('/contas/1/artefatos')
    await screen.findByText(/2 de 2 artefatos/)

    fireEvent.change(screen.getByLabelText('Sub'), { target: { value: '221' } })

    expect(screen.getByRole('columnheader', { name: /Valor da sub/ })).toHaveAttribute('aria-sort', 'descending')
    expect(bodyRows().map((row) => within(row).getAllByRole('cell')[3].textContent)).toEqual(['31', '25'])
  })

  it('o tipo esconde o filtro do outro lado', async () => {
    renderPage('/contas/1/artefatos?tipo=arquetipo')
    await screen.findByText(/1 de 2 artefatos/)

    expect(screen.queryByLabelText('Elemento')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Arquétipo')).toBeInTheDocument()
  })
})
