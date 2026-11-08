import { useParams } from 'react-router'
import { InventoryStatsView } from '@/features/accounts/inventory/InventoryStatsView'
import { useInventoryStats } from '@/features/accounts/useAccounts'
import { QueryStatus } from './QueryStatus'

export function InventoryPage() {
  const wizardId = Number(useParams().wizardId)
  const query = useInventoryStats(wizardId)

  return (
    <QueryStatus query={query} notFound="Conta não encontrada." loading="Carregando inventário…">
      {(inventory) => <InventoryStatsView inventory={inventory} />}
    </QueryStatus>
  )
}
