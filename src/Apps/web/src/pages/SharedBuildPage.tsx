import { useParams } from 'react-router'
import { MonsterDetailView } from '@/features/accounts/monsters/MonsterDetailView'
import { useSharedBuild } from '@/features/accounts/share/useShareLinks'
import { ApiError } from '@/lib/api'
import { formatDateTime } from '@/lib/format'

/** Build compartilhada por link: pública e somente leitura. */
export function SharedBuildPage() {
  const token = useParams().token ?? ''
  const query = useSharedBuild(token)

  if (query.isPending) {
    return <p className="text-sm text-muted-foreground">Carregando build…</p>
  }

  if (query.isError) {
    return (
      <p role="alert" className="text-sm">
        {query.error instanceof ApiError && query.error.status === 404
          ? 'Este link de build não existe ou foi revogado.'
          : `Não foi possível carregar: ${query.error.message}`}
      </p>
    )
  }

  return (
    <div className="space-y-4">
      <p className="text-xs text-muted-foreground">Build compartilhada · import de {formatDateTime(query.data.importedAt)}</p>
      <MonsterDetailView monster={query.data.monster} readOnly />
    </div>
  )
}
