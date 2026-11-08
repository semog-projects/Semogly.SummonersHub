import { useParams } from 'react-router'
import { PveTeamCard } from '@/features/accounts/pve/PveTeamCard'
import { DUNGEON_LABELS } from '@/features/accounts/pve/pveLabels'
import type { DungeonName, PveTeamCheck } from '@/features/accounts/types'
import { usePveTeams } from '@/features/accounts/useAccounts'
import { useMonsterDefinitions } from '@/features/game-data/useMonsterDefinitions'
import { QueryStatus } from './QueryStatus'

export function PveTeamsPage() {
  const wizardId = Number(useParams().wizardId)
  const query = usePveTeams(wizardId)

  return (
    <QueryStatus query={query} notFound="Conta não encontrada." loading="Conferindo times…">
      {(teams) => <PveTeams wizardId={wizardId} teams={teams} />}
    </QueryStatus>
  )
}

function PveTeams({ wizardId, teams }: { wizardId: number; teams: PveTeamCheck[] }) {
  const masterIds = teams.flatMap((team) => team.slots.flatMap((slot) => (slot.monster ? [slot.monster.masterId] : [])))
  const { data: definitions } = useMonsterDefinitions(masterIds)
  const iconOf = (masterId: number) => definitions?.get(masterId)?.imageUrl

  // A API já devolve na ordem das dungeons; o Map preserva a ordem de inserção
  const byDungeon = new Map<DungeonName, PveTeamCheck[]>()
  for (const team of teams) {
    byDungeon.set(team.dungeon, [...(byDungeon.get(team.dungeon) ?? []), team])
  }

  const ready = teams.filter((team) => team.isReady).length

  return (
    <div className="space-y-8">
      <p className="text-sm text-muted-foreground">
        {ready} de {teams.length} times prontos. Times de referência (núcleos clássicos F2P) com mínimos de atributo como ponto de
        partida: o meta muda com os patches, e qualidade de runa e ordem de turnos ainda decidem a dungeon.
      </p>

      {[...byDungeon].map(([dungeon, dungeonTeams]) => (
        <section key={dungeon} aria-label={DUNGEON_LABELS[dungeon]} className="space-y-3">
          <h2 className="font-heading text-lg font-semibold">{DUNGEON_LABELS[dungeon]}</h2>
          <div className="grid gap-4 lg:grid-cols-2">
            {dungeonTeams.map((team) => (
              <PveTeamCard key={team.id} wizardId={wizardId} team={team} iconOf={iconOf} />
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}
