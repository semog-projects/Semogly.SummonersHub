import type { UseQueryResult } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { ApiError } from '@/lib/api'

interface Props<T> {
  query: UseQueryResult<T, Error>
  loading: string
  /** Mensagem para 404 (conta ou monstro inexistente, ou de outro usuário). */
  notFound: string
  children: (data: T) => ReactNode
}

/** Estados de carregamento e erro comuns às páginas da conta. */
export function QueryStatus<T>({ query, loading, notFound, children }: Props<T>) {
  if (query.isPending) {
    return <p className="text-sm text-muted-foreground">{loading}</p>
  }

  if (query.isError) {
    return query.error instanceof ApiError && query.error.status === 404 ? (
      <p role="alert" className="text-sm">
        {notFound}{' '}
        <Link to="/importar" className="font-medium underline underline-offset-4">
          Importar uma conta
        </Link>
      </p>
    ) : (
      <p role="alert" className="text-sm text-destructive">
        Não foi possível carregar: {query.error.message}
      </p>
    )
  }

  return children(query.data)
}
