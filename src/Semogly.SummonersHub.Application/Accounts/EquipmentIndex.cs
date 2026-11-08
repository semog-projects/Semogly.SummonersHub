using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts;

/// <summary>
/// Runas e artefatos do snapshot agrupados pelo monstro que os equipa. Evita percorrer o inventário
/// inteiro para cada monstro ao calcular a conta toda.
/// </summary>
internal sealed class EquipmentIndex(AccountSnapshot snapshot)
{
    private readonly ILookup<long?, Rune> _runes = snapshot.Runes.ToLookup(r => r.EquippedMonsterId);
    private readonly ILookup<long?, Artifact> _artifacts = snapshot.Artifacts.ToLookup(a => a.EquippedMonsterId);

    public IEnumerable<Rune> RunesOf(long monsterId) => _runes[monsterId];

    public IEnumerable<Artifact> ArtifactsOf(long monsterId) => _artifacts[monsterId];

    public MonsterStats StatsOf(Monster monster) =>
        MonsterStatsCalculator.Calculate(monster, RunesOf(monster.GameId), ArtifactsOf(monster.GameId));
}
