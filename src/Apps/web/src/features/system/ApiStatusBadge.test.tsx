import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import { ApiStatusBadge } from './ApiStatusBadge'

function renderWithQuery() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <ApiStatusBadge />
    </QueryClientProvider>,
  )
}

afterEach(() => vi.unstubAllGlobals())

describe('ApiStatusBadge', () => {
  it('mostra API online quando /api/info responde', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ name: 'Semogly.SummonersHub', version: '1.0', environment: 'Development' })),
      ),
    )

    renderWithQuery()

    expect(await screen.findByText('API online · Development')).toBeInTheDocument()
  })

  it('mostra API indisponível quando a chamada falha', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 502 })))

    renderWithQuery()

    expect(await screen.findByText('API indisponível')).toBeInTheDocument()
  })
})
