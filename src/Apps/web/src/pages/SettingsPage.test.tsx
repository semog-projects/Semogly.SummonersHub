import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { SettingsPage } from './SettingsPage'

const backup = { format: 'summonershub-settings', version: 1, exportedAt: '2026-11-09T12:00:00Z', optimizerPresets: [] }

function renderPage(restoreStatus = 200) {
  let preferences = { theme: 'Dark' }
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
    if (url.endsWith('/settings/preferences')) {
      if (init?.method === 'PUT') preferences = JSON.parse(init.body as string)
      return new Response(JSON.stringify(preferences), { status: 200 })
    }
    if (url.endsWith('/settings/backup') && init?.method === 'POST') {
      return restoreStatus === 200
        ? new Response(JSON.stringify({ presets: 2, marks: 5, skippedPresets: 0, skippedMarks: 1 }), { status: 200 })
        : new Response(JSON.stringify({ title: 'Settings.Invalid', detail: 'Arquivo desconhecido.' }), { status: restoreStatus })
    }
    return new Response(JSON.stringify(backup), { status: 200 })
  })
  vi.stubGlobal('fetch', fetchMock)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <SettingsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return fetchMock
}

function uploadFile(content: string) {
  const file = new File([content], 'backup.json', { type: 'application/json' })
  fireEvent.change(screen.getByLabelText('Arquivo de backup'), { target: { files: [file] } })
}

afterEach(() => {
  vi.unstubAllGlobals()
  localStorage.clear()
  document.documentElement.classList.remove('dark')
})

describe('SettingsPage', () => {
  it('mostra o tema salvo e troca o tema na hora, salvando no servidor', async () => {
    const fetchMock = renderPage()

    await waitFor(() => expect(screen.getByRole('radio', { name: 'Escuro' })).toHaveAttribute('aria-checked', 'true'))
    fireEvent.click(screen.getByRole('radio', { name: 'Claro' }))

    expect(document.documentElement).not.toHaveClass('dark')
    await waitFor(() => expect(screen.getByRole('radio', { name: 'Claro' })).toHaveAttribute('aria-checked', 'true'))
    const put = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')!
    expect(JSON.parse(put[1]!.body as string)).toEqual({ theme: 'Light' })
  })

  it('restaura um backup e mostra o resumo', async () => {
    const fetchMock = renderPage()

    uploadFile(JSON.stringify(backup))

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Restaurados 2 preset(s) e 5 marcação(ões). 1 marcação(ões) de outras contas ignorada(s).',
    )
    const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')!
    expect(JSON.parse(post[1]!.body as string)).toEqual(backup)
  })

  it('avisa quando o arquivo não é JSON ou o servidor recusa', async () => {
    renderPage(400)

    uploadFile('isto não é json')
    expect(await screen.findByRole('alert')).toHaveTextContent('O arquivo não é um JSON válido.')

    uploadFile(JSON.stringify({ format: 'outro' }))
    expect(await screen.findByText('Arquivo desconhecido.')).toBeInTheDocument()
  })
})
