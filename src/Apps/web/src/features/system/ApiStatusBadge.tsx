import { Badge } from '@/components/ui/badge'
import { useApiInfo } from './useApiInfo'

export function ApiStatusBadge() {
  const { data, isPending, isError } = useApiInfo()

  if (isPending) {
    return <Badge variant="outline">Verificando API…</Badge>
  }

  if (isError) {
    return <Badge variant="destructive">API indisponível</Badge>
  }

  return <Badge variant="secondary">API online · {data.environment}</Badge>
}
