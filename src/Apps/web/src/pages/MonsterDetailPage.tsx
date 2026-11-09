import { ArrowLeft } from 'lucide-react'
import { useNavigate, useParams, useSearchParams } from 'react-router'
import { BuildSelector } from '@/features/accounts/builds/BuildSelector'
import { MonsterDetailView } from '@/features/accounts/monsters/MonsterDetailView'
import { ShareBuildPanel } from '@/features/accounts/share/ShareBuildPanel'
import { useMonsterDetail } from '@/features/accounts/useAccounts'
import { QueryStatus } from './QueryStatus'

export function MonsterDetailPage() {
  const navigate = useNavigate()
  const params = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const build = searchParams.get('build')
  const query = useMonsterDetail(Number(params.wizardId), Number(params.monsterId), build)

  // A build fica na URL (?build=rta) para o link abrir direto nela
  const selectBuild = (key: string | null) =>
    setSearchParams(key ? { build: key } : {}, { replace: true })

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
            <BuildSelector builds={monster.builds} value={monster.selectedBuild ?? null} onChange={selectBuild} />
            <MonsterDetailView monster={monster} wizardId={Number(params.wizardId)} />
            {!monster.isManual && <ShareBuildPanel wizardId={Number(params.wizardId)} monsterId={monster.monsterId} />}
          </div>
        )}
      </QueryStatus>
    </div>
  )
}
