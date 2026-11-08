import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, vi } from 'vitest'
import sampleText from './__fixtures__/export-sample.json?raw'
import { ImportCard } from './ImportCard'

function renderCard() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <ImportCard />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function selectFile(content: string, name = 'export.json') {
  const input = screen.getByLabelText(/arraste o arquivo/i)
  fireEvent.change(input, { target: { files: [new File([content], name, { type: 'application/json' })] } })
}

function mockFetch(status: number, body: unknown) {
  const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status }))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => vi.unstubAllGlobals())

describe('ImportCard', () => {
  it('mostra o resumo do export após selecionar o arquivo', async () => {
    renderCard()

    selectFile(sampleText, 'conta.json')

    expect(await screen.findByText(/TestWizard/)).toBeInTheDocument()
    expect(screen.getByText('conta.json')).toBeInTheDocument()
    expect(screen.getByText('Dados sensíveis removidos')).toBeInTheDocument()
    expect(screen.getByText('12 equipadas')).toBeInTheDocument()
  })

  it('mostra erro para arquivo inválido', async () => {
    renderCard()

    selectFile('{"command":"Other"}')

    expect(await screen.findByRole('alert')).toHaveTextContent('não parece ser um export do SW Exporter')
  })

  it('envia apenas o payload sanitizado e mostra a versão criada', async () => {
    const fetchMock = mockFetch(201, { snapshotId: 'x', wizardId: 12345678, version: 1, created: true })
    renderCard()
    selectFile(sampleText)

    fireEvent.click(await screen.findByRole('button', { name: 'Enviar' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Conta importada (versão 1).')
    expect(screen.getByRole('link', { name: 'Ver painel' })).toHaveAttribute('href', '/contas/12345678')
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/imports')
    expect(init.method).toBe('POST')
    expect(init.body).not.toContain('FAKE-SESSION-KEY')
    expect(JSON.parse(init.body).wizard.id).toBe(12345678)
  })

  it('avisa quando o conteúdo já tinha sido importado', async () => {
    mockFetch(200, { snapshotId: 'x', wizardId: 12345678, version: 3, created: false })
    renderCard()
    selectFile(sampleText)

    fireEvent.click(await screen.findByRole('button', { name: 'Enviar' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Nada mudou desde a versão 3')
  })

  it('mostra a mensagem de erro da API', async () => {
    mockFetch(400, { title: 'Accounts.UnknownGameCodes', detail: 'O export contém códigos do jogo ainda não suportados: set 77.' })
    renderCard()
    selectFile(sampleText)

    fireEvent.click(await screen.findByRole('button', { name: 'Enviar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('set 77')
  })
})
