/**
 * Versões comparadas, guardadas na URL (?de=&para=). Sem parâmetros (ou com versões que não existem),
 * compara a penúltima com a última.
 */
export function selectVersions(params: URLSearchParams, versions: number[]): { from: number | null; to: number | null } {
  const sorted = [...versions].sort((a, b) => b - a)
  const valid = (value: string | null) => {
    const version = Number(value)
    return value !== null && sorted.includes(version) ? version : null
  }

  const to = valid(params.get('para')) ?? sorted[0] ?? null
  const from = valid(params.get('de')) ?? sorted.find((v) => v < (to ?? 0)) ?? null

  return { from, to: from === to ? null : to }
}
