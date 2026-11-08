import { fireEvent, render, screen } from '@testing-library/react'
import sampleText from './__fixtures__/export-sample.json?raw'
import { ImportCard } from './ImportCard'


function selectFile(content: string, name = 'export.json') {
  const input = screen.getByLabelText(/arraste o arquivo/i)
  fireEvent.change(input, { target: { files: [new File([content], name, { type: 'application/json' })] } })
}

describe('ImportCard', () => {
  it('mostra o resumo do export após selecionar o arquivo', async () => {
    render(<ImportCard />)

    selectFile(sampleText, 'conta.json')

    expect(await screen.findByText(/TestWizard/)).toBeInTheDocument()
    expect(screen.getByText('conta.json')).toBeInTheDocument()
    expect(screen.getByText('Dados sensíveis removidos')).toBeInTheDocument()
    expect(screen.getByText('12 equipadas')).toBeInTheDocument()
  })

  it('mostra erro para arquivo inválido', async () => {
    render(<ImportCard />)

    selectFile('{"command":"Other"}')

    expect(await screen.findByRole('alert')).toHaveTextContent('não parece ser um export do SW Exporter')
  })
})
