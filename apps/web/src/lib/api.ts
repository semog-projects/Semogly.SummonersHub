/** Erro de uma chamada à API, com o status HTTP e o ProblemDetails (quando houver). */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

/** GET tipado em /api. Em desenvolvimento o Vite faz proxy para a API (mesma origem). */
export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`/api${path}`, { signal, headers: { Accept: 'application/json' } })

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? response.statusText)
  }

  return (await response.json()) as T
}
