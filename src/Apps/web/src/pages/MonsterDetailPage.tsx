import { ArrowLeft } from 'lucide-react'
import { useNavigate, useParams } from 'react-router'
import { MonsterDetailView } from '@/features/accounts/monsters/MonsterDetailView'
import { ShareBuildPanel } from '@/features/accounts/share/ShareBuildPanel'
import { useMonsterDetail } from '@/features/accounts/useAccounts'
import { QueryStatus } from './QueryStatus'

export function MonsterDetailPage() {
  const navigate = useNavigate()
  const params = useParams()
  const query = useMonsterDetail(Number(params.wizardId), Number(params.monsterId))

  return (
    <div className="space-y-4">
      <button
        type="button"
        onClick={() =>
          // Volta pelo histórico para manter os filtros da lista; aberto por link direto, vai para a lista
          (window.history.state as { idx?: number } | null)?.idx ? navigate(-1) : navigate('..', { relative: 'path' })
        }
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" aria-hidden /> Voltar
      </button>

      <QueryStatus query={query} notFound="Monstro não encontrado no último import." loading="Carregando monstro…">
        {(monster) => (
          <div className="space-y-6">
            <MonsterDetailView monster={monster} wizardId={Number(params.wizardId)} />
            <ShareBuildPanel wizardId={Number(params.wizardId)} monsterId={monster.monsterId} />
          </div>
        )}
      </QueryStatus>
    </div>
  )
}
