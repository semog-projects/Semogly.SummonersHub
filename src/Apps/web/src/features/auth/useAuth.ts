import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, getJson, postJson } from '@/lib/api'

export interface CurrentUser {
  email: string
  isEmailConfirmed: boolean
}

export interface Credentials {
  email: string
  password: string
}

export const currentUserKey = ['auth', 'me'] as const

/** Usuário logado (GET /api/auth/manage/info). Sem sessão, resolve como null em vez de erro. */
export function useCurrentUser() {
  return useQuery({
    queryKey: currentUserKey,
    queryFn: async ({ signal }) => {
      try {
        return await getJson<CurrentUser>('/auth/manage/info', signal)
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) return null
        throw error
      }
    },
    retry: false,
    staleTime: Infinity,
  })
}

/** Login com cookie HttpOnly: o token nunca fica acessível ao JavaScript. */
export function useLogin() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (credentials: Credentials) => login(credentials),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: currentUserKey }),
  })
}

/** Cadastro seguido de login automático. */
export function useRegister() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (credentials: Credentials) => {
      await postJson<void>('/auth/register', credentials)
      await login(credentials)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: currentUserKey }),
  })
}

export function useLogout() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => postJson<void>('/auth/logout'),
    // Descarta tudo que veio da sessão anterior (contas, resumos...)
    onSettled: () => {
      queryClient.clear()
      queryClient.setQueryData(currentUserKey, null)
    },
  })
}

async function login(credentials: Credentials) {
  try {
    await postJson<void>('/auth/login?useCookies=true', credentials)
  } catch (error) {
    // O Identity devolve o SignInResult no detail: "Failed", "Lockedout" ou "NotAllowed"
    if (error instanceof ApiError && error.status === 401) {
      throw new ApiError(
        401,
        error.message === 'Lockedout'
          ? 'Muitas tentativas sem sucesso. Tente de novo em alguns minutos.'
          : 'E-mail ou senha inválidos.',
        error.title,
      )
    }
    throw error
  }
}

/** Provedores de login externo configurados na API (ex.: ["Google"]). Sem configuração, lista vazia. */
export function useExternalProviders() {
  return useQuery({
    queryKey: ['auth', 'providers'],
    queryFn: ({ signal }) => getJson<string[]>('/auth/providers', signal),
    retry: false,
    staleTime: Infinity,
  })
}

/** Endereço que inicia o login externo (navegação completa: o provedor redireciona de volta para a API). */
export function externalLoginUrl(provider: string, returnUrl: string): string {
  return `/api/auth/external/${encodeURIComponent(provider.toLowerCase())}?returnUrl=${encodeURIComponent(returnUrl)}`
}

/** Mensagens para o ?erro= que a API devolve quando o login externo falha. */
export const EXTERNAL_LOGIN_ERRORS: Record<string, string> = {
  emailnotverified: 'O Google não confirmou o e-mail dessa conta.',
  blocked: 'Conta bloqueada temporariamente. Tente de novo em alguns minutos.',
  failed: 'Não foi possível entrar com o Google. Tente de novo.',
}
