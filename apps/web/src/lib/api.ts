/** Erro de uma chamada à API, com o status HTTP e o ProblemDetails (quando houver). */
export class ApiError extends Error {
  readonly status: number
  /** Título do ProblemDetails (código do erro, ex.: "Accounts.NotFound" ou "LockedOut"). */
  readonly title?: string

  constructor(status: number, message: string, title?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.title = title
  }
}

/** GET tipado em /api. Em desenvolvimento o Vite faz proxy para a API (mesma origem, cookie de login incluso). */
export function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  return request<T>(path, { signal })
}

/** POST com corpo JSON em /api. Respostas sem corpo (204, login) resolvem como undefined. */
export function postJson<T>(path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  return request<T>(path, {
    method: 'POST',
    signal,
    body: body === undefined ? undefined : JSON.stringify(body),
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
  })
}

interface ProblemDetails {
  title?: string
  detail?: string
  /** ValidationProblemDetails: mensagens por campo/código. */
  errors?: Record<string, string[]>
}

async function request<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    credentials: 'same-origin',
    headers: { Accept: 'application/json', ...init.headers },
  })

  const text = await response.text()
  const body = text ? safeParse(text) : undefined

  if (!response.ok) {
    const problem = (body ?? {}) as ProblemDetails
    // 401 sem corpo: a sessão expirou ou o usuário saiu em outra aba
    const fallback = response.status === 401 ? 'Sua sessão expirou. Entre novamente.' : response.statusText
    throw new ApiError(response.status, problemMessage(problem) ?? fallback, problem.title)
  }

  return body as T
}

/** Mensagem para o usuário: o detail do ProblemDetails ou, em erros de validação, as mensagens sem repetição. */
function problemMessage(problem: ProblemDetails): string | undefined {
  if (problem.errors) {
    return [...new Set(Object.values(problem.errors).flat())].join(' ')
  }
  return problem.detail ?? problem.title
}

function safeParse(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}
