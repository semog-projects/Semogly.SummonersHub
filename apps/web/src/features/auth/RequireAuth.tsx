import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useCurrentUser } from './useAuth'

/** Só renderiza o conteúdo com usuário logado; senão, manda para o login guardando a página de origem. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const location = useLocation()
  const { data: user, isPending, isError } = useCurrentUser()

  if (isPending) {
    return <p className="text-sm text-muted-foreground">Carregando…</p>
  }

  if (isError) {
    return (
      <p role="alert" className="text-sm text-destructive">
        Não foi possível verificar o login. Verifique se a API está no ar.
      </p>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return children
}
