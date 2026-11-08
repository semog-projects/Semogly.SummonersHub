import { useParams, useSearchParams } from 'react-router'
import { CraftSuggestionCard } from '@/features/accounts/craft/CraftSuggestionCard'
import { useCraftSuggestions } from '@/features/accounts/useAccounts'
import { QueryStatus } from './QueryStatus'

export function CraftSuggestionsPage() {
  const wizardId = Number(useParams().wizardId)
  const [searchParams, setSearchParams] = useSearchParams()
  const includeInventory = searchParams.get('inventario') === '1'
  const query = useCraftSuggestions(wizardId, includeInventory)

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground">
          Onde cada gem e grind mais aumenta a eficiência da runa, do pior ao melhor sorteio. Itens do mesmo set (ou
          immemorial), respeitando as regras do jogo.
        </p>
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={includeInventory}
            onChange={(event) => setSearchParams(event.target.checked ? { inventario: '1' } : {}, { replace: true })}
          />
          Incluir runas do inventário
        </label>
      </div>

      <QueryStatus query={query} notFound="Conta não encontrada." loading="Calculando sugestões…">
        {(groups) =>
          groups.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhuma gem ou grind no inventário.</p>
          ) : (
            <div className="space-y-4">
              {groups.map((group) => (
                <CraftSuggestionCard key={`${group.type}-${group.set}-${group.stat}-${group.rarity}`} wizardId={wizardId} group={group} />
              ))}
            </div>
          )
        }
      </QueryStatus>
    </div>
  )
}
