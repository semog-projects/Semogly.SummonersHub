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
    private readonly ILookup<long, AlternativeBuild> _builds = snapshot.Builds.ToLookup(b => b.MonsterId);
    private readonly Lazy<List<long>> _siegeDecks = new(() =>
        [.. snapshot.Builds.Where(b => b.Kind == BuildKind.Siege).Select(b => b.DeckId).Distinct().Order()]);
    private readonly Lazy<Dictionary<long, Rune>> _runesById = new(() => snapshot.Runes.ToDictionary(r => r.GameId));
    private readonly Lazy<Dictionary<long, Artifact>> _artifactsById = new(() => snapshot.Artifacts.ToDictionary(a => a.GameId));

    public IEnumerable<Rune> RunesOf(long monsterId) => _runes[monsterId];

    public IEnumerable<Artifact> ArtifactsOf(long monsterId) => _artifacts[monsterId];

    /// <summary>Builds de RTA, Siege e Arena do monstro, em ordem estável.</summary>
    public IEnumerable<AlternativeBuild> BuildsOf(long monsterId) =>
        _builds[monsterId].OrderBy(b => b.Kind).ThenBy(b => b.DeckId);

    /// <summary>Posição (1, 2, 3...) do deck de Siege entre as defesas da conta; 0 nos demais modos.</summary>
    public int NumberOf(AlternativeBuild build) =>
        build.Kind == BuildKind.Siege ? _siegeDecks.Value.IndexOf(build.DeckId) + 1 : 0;

    /// <summary>Build alternativa pela chave (<see cref="AlternativeBuild.Key"/>), ou nulo para a build normal.</summary>
    public AlternativeBuild? FindBuild(long monsterId, string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : _builds[monsterId].FirstOrDefault(b => b.Key == key);

    /// <summary>Runas do monstro na build informada (nulo = build normal).</summary>
    public IReadOnlyList<Rune> RunesOf(long monsterId, AlternativeBuild? build) =>
        build is null ? [.. RunesOf(monsterId).OrderBy(r => r.Slot)] : build.ResolveRunes(RunesOf(monsterId), _runesById.Value);

    /// <summary>Artefatos do monstro na build informada (nulo = build normal).</summary>
    public IReadOnlyList<Artifact> ArtifactsOf(long monsterId, AlternativeBuild? build) =>
        build is null ? [.. ArtifactsOf(monsterId).OrderBy(a => a.Type)] : build.ResolveArtifacts(ArtifactsOf(monsterId), _artifactsById.Value);

    /// <summary>Eficiência média (atual) das runas equipadas no monstro, ou nulo se ele não tem runas.</summary>
    public decimal? AverageRuneEfficiencyOf(long monsterId, AlternativeBuild? build = null)
    {
        var runes = RunesOf(monsterId, build);
        return runes.Count == 0 ? null : Math.Round(runes.Average(r => RuneEfficiency.Calculate(r).Current), 2);
    }

    public MonsterStats StatsOf(Monster monster, AlternativeBuild? build = null) =>
        MonsterStatsCalculator.Calculate(monster, RunesOf(monster.GameId, build), ArtifactsOf(monster.GameId, build));
}
