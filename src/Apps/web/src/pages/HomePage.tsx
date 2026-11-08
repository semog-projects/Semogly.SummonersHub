import { Navigate } from 'react-router'
import { useAccounts } from '@/features/accounts/useAccounts'
import { ImportCard } from '@/features/import/ImportCard'

/** Entrada após o login: abre o painel do invocador mais recente ou, sem nenhum, pede o primeiro import. */
export function HomePage() {
  const { data: accounts, isPending, isError, error } = useAccounts()

  if (isPending) {
    return <p className="text-sm text-muted-foreground">Carregando…</p>
  }

  if (isError) {
    return (
      <p role="alert" className="text-sm text-destructive">
        Não foi possível carregar suas contas: {error.message}
      </p>
    )
  }

  if (accounts.length > 0) {
    return <Navigate to={`/contas/${accounts[0].wizardId}`} replace />
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-heading text-2xl font-semibold">Bem-vindo ao SummonersHub</h1>
        <p className="text-sm text-muted-foreground">Importe sua conta para ver o painel.</p>
      </div>
      <ImportCard />
    </div>
  )
}
