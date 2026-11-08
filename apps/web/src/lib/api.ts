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
export function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  return request<T>(path, { signal })
}

/** POST com corpo JSON em /api. */
export function postJson<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
  return request<T>(path, {
    method: 'POST',
    signal,
    body: JSON.stringify(body),
    headers: { 'Content-Type': 'application/json' },
  })
}

async function request<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, { ...init, headers: { Accept: 'application/json', ...init.headers } })

  if (!response.ok) {
    // ProblemDetails da API: o detail traz a mensagem para o usuário
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? response.statusText)
  }

  return (await response.json()) as T
}
