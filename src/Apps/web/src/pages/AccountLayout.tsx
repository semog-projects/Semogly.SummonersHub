import { NavLink, Outlet, useParams } from 'react-router'
import { useAccounts } from '@/features/accounts/useAccounts'
import { cn } from '@/lib/utils'

/** Moldura das páginas de uma conta: troca de invocador e abas (visão geral, monstros, runas, inventário...). */
export function AccountLayout() {
  const { wizardId } = useParams()

  const tabClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      '-mb-px border-b-2 border-transparent px-1 pb-2 text-sm text-muted-foreground transition-colors hover:text-foreground',
      isActive && 'border-primary font-medium text-foreground',
    )

  return (
    <div className="space-y-6">
      <AccountSwitcher />

      <nav aria-label="Seções da conta" className="flex gap-6 overflow-x-auto border-b whitespace-nowrap">
        <NavLink to={`/contas/${wizardId}`} end className={tabClass}>
          Visão geral
        </NavLink>
        <NavLink to={`/contas/${wizardId}/monstros`} className={tabClass}>
          Monstros
        </NavLink>
        <NavLink to={`/contas/${wizardId}/runas`} className={tabClass}>
          Runas
        </NavLink>
        <NavLink to={`/contas/${wizardId}/artefatos`} className={tabClass}>
          Artefatos
        </NavLink>
        <NavLink to={`/contas/${wizardId}/inventario`} className={tabClass}>
          Inventário
        </NavLink>
        <NavLink to={`/contas/${wizardId}/times`} className={tabClass}>
          Times PvE
        </NavLink>
        <NavLink to={`/contas/${wizardId}/historico`} className={tabClass}>
          Histórico
        </NavLink>
      </nav>

      <Outlet />
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
