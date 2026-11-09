import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import type { ShareLink } from '../types'
import { ShareBuildPanel } from './ShareBuildPanel'

afterEach(() => vi.unstubAllGlobals())

describe('ShareBuildPanel', () => {
  it('gera o link, lista e revoga', async () => {
    let links: ShareLink[] = []
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        links = [{ token: 'tokentokentokentokento', version: 2, createdAt: '2026-11-08T12:00:00Z' }]
        return new Response(JSON.stringify(links[0]), { status: 200 })
      }
      if (init?.method === 'DELETE') {
        links = []
        return new Response(null, { status: 204 })
      }
      return new Response(JSON.stringify(url.endsWith('/shares') ? links : []), { status: 200 })
    })
    vi.stubGlobal('fetch', fetchMock)
    Object.assign(navigator, { clipboard: { writeText: vi.fn(async () => {}) } })

    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <ShareBuildPanel wizardId={1} monsterId={11} />
      </QueryClientProvider>,
    )
    expect(await screen.findByText('Nenhum link gerado.')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /Gerar link/ }))

    const list = await screen.findByRole('list', { name: 'Links gerados' })
    expect(list).toHaveTextContent('/b/tokentokentokentokento')
    expect(list).toHaveTextContent('versão 2')
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(expect.stringMatching(/\/b\/tokentokentokentokento$/))

    fireEvent.click(within(list).getByRole('button', { name: 'Revogar' }))
    expect(await screen.findByText('Nenhum link gerado.')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/shares/tokentokentokentokento', expect.objectContaining({ method: 'DELETE' }))
  })
})
