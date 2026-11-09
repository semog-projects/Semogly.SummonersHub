import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, vi } from 'vitest'
import type { RuneListItem } from '@/features/accounts/types'
import { RUNE_PAGE_SIZE } from '@/features/accounts/runes/runeFilters'
import { RuneListPage } from './RuneListPage'

function rune(runeId: number, efficiency: number, equipped: boolean): RuneListItem {
  return {
    rune: {
      runeId,
      slot: (runeId % 6) + 1,
      stars: 6,
      isAncient: false,
      set: runeId % 2 === 0 ? 'Swift' : 'Violent',
      level: 15,
      rarity: 5,
      originalRarity: 5,
      mainStat: { type: 4, value: 63 },
      innateStat: null,
      substats: [{ type: 8, value: runeId, isGemmed: false, grindValue: 0 }],
      efficiency,
      maxEfficiency: efficiency,
    },
    equippedOn: equipped ? { monsterId: 11, masterId: 19811, name: 'Lapis', element: 'Water' } : null,
  }
}

function renderPage(path: string, runes: RuneListItem[]) {
  vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(runes), { status: 200 })))
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/contas/:wizardId/runas" element={<RuneListPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const bodyRows = () => screen.getAllByRole('row').slice(1)

afterEach(() => vi.unstubAllGlobals())

describe('RuneListPage', () => {
  it('lista pela eficiência e liga a runa ao monstro que a equipa', async () => {
    renderPage('/contas/1/runas', [rune(1, 70.5, true), rune(2, 92.86, false)])

    await screen.findByText(/2 de 2 runas/)
    const [first, second] = bodyRows()
    expect(first).toHaveTextContent('Swift (3) +15')
    expect(first).toHaveTextContent('92,86%')
    expect(first).toHaveTextContent('Inventário')
    expect(within(second).getByRole('link', { name: 'Lapis' })).toHaveAttribute('href', '/contas/1/monstros/11')
  })

  it('aplica o filtro de local da URL e o de set pelo combo', async () => {
    renderPage('/contas/1/runas?local=equipada', [rune(1, 70, true), rune(2, 90, false), rune(3, 80, true)])
    await screen.findByText(/2 de 3 runas/)

    expect(bodyRows()).toHaveLength(2)
    expect(screen.getByText(/2 de 3 runas/)).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Set'), { target: { value: 'Swift' } })
    expect(screen.getByText('Nenhuma runa com esses filtros.')).toBeInTheDocument()
  })

  it('exporta em CSV todas as runas filtradas, não só a página', async () => {
    const many = Array.from({ length: RUNE_PAGE_SIZE + 5 }, (_, i) => rune(i + 1, 50, i % 2 === 0))
    renderPage('/contas/1/runas?local=equipada', many)
    await screen.findByText(/53 de 105 runas/)

    let exported: Blob | undefined
    vi.stubGlobal('URL', { ...URL, createObjectURL: (blob: Blob) => ((exported = blob), 'blob:csv'), revokeObjectURL: () => {} })
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})

    fireEvent.click(screen.getByRole('button', { name: /Exportar CSV/ }))

    expect(click).toHaveBeenCalled()
    // As linhas são separadas por CRLF (o BOM some na decodificação do Blob, mas é removido por garantia)
    const lines = (await exported!.text()).replace(String.fromCharCode(0xfeff), '').split(String.fromCharCode(13, 10))
    expect(lines[0]).toMatch(/^Id;Set;Slot/)
    expect(lines).toHaveLength(53 + 1)
    click.mockRestore()
  })

  it('pagina a tabela', async () => {
    const many = Array.from({ length: RUNE_PAGE_SIZE + 5 }, (_, i) => rune(i + 1, 50, false))
    renderPage('/contas/1/runas', many)
    await screen.findByText(/105 de 105 runas/)

    expect(bodyRows()).toHaveLength(RUNE_PAGE_SIZE)
    fireEvent.click(screen.getByRole('button', { name: /Mostrar mais \(5 restantes\)/ }))
    expect(bodyRows()).toHaveLength(RUNE_PAGE_SIZE + 5)
  })
})
