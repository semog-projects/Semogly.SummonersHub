import { ArrowLeft } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { MonsterIcon } from '@/features/accounts/monsters/MonsterIcon'
import { initialForm, toRequest, type OptimizerForm } from '@/features/accounts/optimizer/optimizerOptions'
import { OptimizerFormView } from '@/features/accounts/optimizer/OptimizerFormView'
import { OptimizerResults } from '@/features/accounts/optimizer/OptimizerResults'
import { useOptimizeRunes } from '@/features/accounts/optimizer/useOptimizeRunes'
import type { MonsterDetail } from '@/features/accounts/types'
import { useMonsterDetail } from '@/features/accounts/useAccounts'
import { useMonsterDefinitions } from '@/features/game-data/useMonsterDefinitions'
import { QueryStatus } from './QueryStatus'

export function RuneOptimizerPage() {
  const params = useParams()
  const wizardId = Number(params.wizardId)
  const monsterId = Number(params.monsterId)
  const query = useMonsterDetail(wizardId, monsterId)

  return (
    <div className="space-y-4">
      <Link
        to={`/contas/${wizardId}/monstros/${monsterId}`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" aria-hidden /> Voltar ao monstro
      </Link>

      <QueryStatus query={query} notFound="Monstro não encontrado no último import." loading="Carregando monstro…">
        {(monster) => <Optimizer wizardId={wizardId} monster={monster} />}
      </QueryStatus>
    </div>
  )
}

function Optimizer({ wizardId, monster }: { wizardId: number; monster: MonsterDetail }) {
  const [form, setForm] = useState<OptimizerForm>(() => initialForm(monster))
  const optimize = useOptimizeRunes(wizardId, monster.monsterId)
  const { data: definitions } = useMonsterDefinitions([monster.masterId])

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <MonsterIcon imageUrl={definitions?.get(monster.masterId)?.imageUrl} className="size-12 rounded-lg" />
        <div>
          <h1 className="font-heading text-2xl font-semibold">Otimizar runas: {monster.name ?? `Monstro ${monster.masterId}`}</h1>
          <p className="text-sm text-muted-foreground">
            Sugestões a partir das runas do último import. Nada é alterado na conta.
          </p>
        </div>
      </div>

      <OptimizerFormView form={form} onChange={setForm} onSubmit={() => optimize.mutate(toRequest(form))} pending={optimize.isPending} />

      {optimize.isError && (
        <p role="alert" className="text-sm text-destructive">
          Não foi possível otimizar: {optimize.error.message}
        </p>
      )}
      {optimize.data && <OptimizerResults wizardId={wizardId} monsterId={monster.monsterId} result={optimize.data} />}
    </div>
  )
}
