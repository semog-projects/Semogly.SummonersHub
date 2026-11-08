import { useParams } from 'react-router'
import { AccountOverview } from '@/features/accounts/overview/AccountOverview'
import { useAccountOverview } from '@/features/accounts/useAccounts'
import { QueryStatus } from './QueryStatus'

export function AccountOverviewPage() {
  const wizardId = Number(useParams().wizardId)
  const query = useAccountOverview(wizardId)

  return (
    <QueryStatus query={query} notFound="Conta não encontrada." loading="Carregando painel…">
      {(overview) => <AccountOverview overview={overview} />}
    </QueryStatus>
  )
}
