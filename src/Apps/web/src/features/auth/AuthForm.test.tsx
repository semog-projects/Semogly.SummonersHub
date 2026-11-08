import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import { AuthForm } from './AuthForm'

function renderForm(onSuccess = vi.fn()) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <AuthForm onSuccess={onSuccess} />
    </QueryClientProvider>,
  )
  return onSuccess
}

function fill(email: string, password: string) {
  fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: email } })
  fireEvent.change(screen.getByLabelText('Senha'), { target: { value: password } })
}

/** Cada chamada ao fetch consome a próxima resposta da lista. */
function mockFetch(...responses: Array<[number, unknown?]>) {
  const fetchMock = vi.fn()
  for (const [status, body] of responses) {
    fetchMock.mockResolvedValueOnce(new Response(body === undefined ? null : JSON.stringify(body), { status }))
  }
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => vi.unstubAllGlobals())

describe('AuthForm', () => {
  it('faz login com cookie e avisa o sucesso', async () => {
    const fetchMock = mockFetch([200], [200, { email: 'a@b.com', isEmailConfirmed: false }])
    const onSuccess = renderForm()
    fill('a@b.com', 'Senha123forte')

    fireEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    await waitFor(() => expect(onSuccess).toHaveBeenCalled())
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/auth/login?useCookies=true')
    expect(JSON.parse(init.body)).toEqual({ email: 'a@b.com', password: 'Senha123forte' })
  })

  it('mostra mensagem amigável para credenciais inválidas', async () => {
    mockFetch([401, { title: 'Unauthorized', status: 401, detail: 'Failed' }])
    const onSuccess = renderForm()
    fill('a@b.com', 'errada')

    fireEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('E-mail ou senha inválidos.')
    expect(onSuccess).not.toHaveBeenCalled()
  })

  it('avisa quando a conta está bloqueada por tentativas', async () => {
    mockFetch([401, { title: 'Unauthorized', status: 401, detail: 'Lockedout' }])
    renderForm()
    fill('a@b.com', 'errada')

    fireEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Muitas tentativas')
  })

  it('cadastra e já entra com a conta nova', async () => {
    const fetchMock = mockFetch([200], [200], [200, { email: 'novo@b.com', isEmailConfirmed: false }])
    const onSuccess = renderForm()
    fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    fill('novo@b.com', 'Senha123forte')

    fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))

    await waitFor(() => expect(onSuccess).toHaveBeenCalled())
    expect(fetchMock.mock.calls[0][0]).toBe('/api/auth/register')
    expect(fetchMock.mock.calls[1][0]).toBe('/api/auth/login?useCookies=true')
  })

  it('mostra as mensagens de validação do cadastro sem repetir', async () => {
    mockFetch([
      400,
      {
        title: 'One or more validation errors occurred.',
        errors: {
          DuplicateUserName: ['O e-mail a@b.com já está cadastrado.'],
          DuplicateEmail: ['O e-mail a@b.com já está cadastrado.'],
        },
      },
    ])
    renderForm()
    fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))
    fill('a@b.com', 'Senha123forte')

    fireEvent.click(screen.getByRole('button', { name: 'Criar conta' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/^O e-mail a@b.com já está cadastrado.$/)
  })
})
