import { Link, NavLink, useParams } from 'react-router'
import { AccountOverview } from '@/features/accounts/overview/AccountOverview'
import { useAccountOverview, useAccounts } from '@/features/accounts/useAccounts'
import { ApiError } from '@/lib/api'
import { cn } from '@/lib/utils'

export function AccountOverviewPage() {
  const wizardId = Number(useParams().wizardId)
  const { data: overview, isPending, isError, error } = useAccountOverview(wizardId)

  return (
    <div className="space-y-6">
      <AccountSwitcher />

      {isPending && <p className="text-sm text-muted-foreground">Carregando painel…</p>}

      {isError &&
        (error instanceof ApiError && error.status === 404 ? (
          <p role="alert" className="text-sm">
            Conta não encontrada.{' '}
            <Link to="/importar" className="font-medium underline underline-offset-4">
              Importar uma conta
            </Link>
          </p>
        ) : (
          <p role="alert" className="text-sm text-destructive">
            Não foi possível carregar o painel: {error.message}
          </p>
        ))}

      {overview && <AccountOverview overview={overview} />}
    </div>
  )
}

/** Troca de invocador, só quando o usuário tem mais de um. */
function AccountSwitcher() {
  const { data: accounts } = useAccounts()

  if (!accounts || accounts.length < 2) return null

  return (
    <nav aria-label="Contas" className="flex flex-wrap gap-2">
      {accounts.map((account) => (
        <NavLink
          key={account.wizardId}
          to={`/contas/${account.wizardId}`}
          className={({ isActive }) =>
            cn(
              'rounded-full border px-3 py-1 text-sm transition-colors hover:bg-muted',
              isActive && 'border-primary bg-muted font-medium',
            )
          }
        >
          {account.wizardName}
        </NavLink>
      ))}
    </nav>
  )
}
