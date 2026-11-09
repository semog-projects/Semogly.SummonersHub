/**
 * CSV no formato que o Excel em português abre direto: separador ";", vírgula decimal e BOM UTF-8
 * (sem o BOM, o Excel lê os acentos errado).
 */
export type CsvCell = string | number | boolean | null | undefined

const decimalFormat = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2, useGrouping: false })

export function toCsv(header: string[], rows: CsvCell[][]): string {
  return [header, ...rows].map((row) => row.map(formatCell).join(';')).join('\r\n')
}

function formatCell(cell: CsvCell): string {
  if (cell === null || cell === undefined) return ''
  if (typeof cell === 'boolean') return cell ? 'Sim' : 'Não'
  const text = typeof cell === 'number' ? decimalFormat.format(cell) : cell
  // Aspas quando o texto tem separador, aspas ou quebra de linha (aspas internas são dobradas)
  return /[;"\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text
}

/** Baixa o CSV no navegador, sem passar pela API. */
export function downloadCsv(fileName: string, csv: string) {
  const blob = new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}
