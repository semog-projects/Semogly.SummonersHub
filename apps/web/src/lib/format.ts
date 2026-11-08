const dateTimeFormat = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
const numberFormat = new Intl.NumberFormat('pt-BR')

/** Data e hora no fuso do navegador, ex.: 07/11/2026 20:25. */
export function formatDateTime(value: string): string {
  return dateTimeFormat.format(new Date(value))
}

/** Número com separador de milhar pt-BR, ex.: 14.281. */
export function formatNumber(value: number): string {
  return numberFormat.format(value)
}
