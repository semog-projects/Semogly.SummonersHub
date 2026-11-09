import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import { ExternalLogin } from './ExternalLogin'

function renderWith(providers: string[], error: string | null = null) {
  vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(providers), { status: 200 })))
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <ExternalLogin returnUrl="/contas/1" error={error} />
    </QueryClientProvider>,
  )
}

afterEach(() => vi.unstubAllGlobals())

describe('ExternalLogin', () => {
  it('mostra o botão do Google voltando para a página pedida', async () => {
    renderWith(['Google'])

    const link = await screen.findByRole('link', { name: 'Entrar com Google' })
    expect(link).toHaveAttribute('href', '/api/auth/external/google?returnUrl=%2Fcontas%2F1')
  })

  it('esconde o botão quando o Google não está configurado', async () => {
    renderWith([])

    await new Promise((resolve) => setTimeout(resolve, 0))
    expect(screen.queryByRole('link', { name: 'Entrar com Google' })).not.toBeInTheDocument()
  })

  it('explica o erro devolvido pela API', () => {
    renderWith([], 'emailnotverified')

    expect(screen.getByRole('alert')).toHaveTextContent('O Google não confirmou o e-mail dessa conta.')
  })
})
