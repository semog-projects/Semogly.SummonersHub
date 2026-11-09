using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class RuneOptimizerTests
{
    private const long MonsterId = 1;

    private static readonly Monster Monster = new(
        MonsterId, 19811, 40, 6, Element.Water,
        new BaseStats(Hp: 10000, Attack: 700, Defense: 600, Speed: 100, Resistance: 15, Accuracy: 0, CriticalRate: 15, CriticalDamage: 50),
        [], false, null);

    private static long _nextId = 100;

    [Fact]
    public void ShouldCompleteRequiredSetAndFillFreeSlotsWithTheBest()
    {
        // Swift VEL 5 em todos os slots; Violent com VEL 20 em todos: o melhor é 4 Swift (slots com Violent pior) + 2 Violent
        Rune[] pool =
        [
            .. Enumerable.Range(1, 6).Select(slot => NewRune(slot, RuneSet.Swift, speed: 5)),
            .. Enumerable.Range(1, 6).Select(slot => NewRune(slot, RuneSet.Violent, speed: 20)),
        ];

        var best = RuneOptimizer.Optimize(Monster, [], pool, Request([RuneSet.Swift], [PveStat.Speed]))[0];

        Assert.Equal(4, best.Runes.Count(r => r.Set == RuneSet.Swift));
        Assert.Equal(2, best.Runes.Count(r => r.Set == RuneSet.Violent));
        Assert.Contains(RuneSet.Swift, best.Stats.ActiveSets);

        // 100 base + 25 do Swift + 4 × 5 + 2 × 20
        Assert.Equal(185, best.Stats.Total.Speed);
        Assert.Equal(185m, best.Score);
    }

    [Fact]
    public void ShouldRespectMainStatPerSlot()
    {
        Rune[] pool =
        [
            .. Enumerable.Range(1, 6).Where(slot => slot != 2).Select(slot => NewRune(slot, RuneSet.Swift, speed: 5)),
            NewRune(2, RuneSet.Swift, speed: 0, main: RuneStat.Speed, mainValue: 42),
            NewRune(2, RuneSet.Swift, speed: 0, main: RuneStat.HpPercent, mainValue: 63), // melhor para HP, mas fora do filtro
        ];
        var request = Request([RuneSet.Swift], [PveStat.Hp]) with
        {
            MainStatsBySlot = new Dictionary<int, IReadOnlyList<RuneStat>> { [2] = [RuneStat.Speed] },
        };

        var best = RuneOptimizer.Optimize(Monster, [], pool, request)[0];

        Assert.Equal(RuneStat.Speed, best.Runes.Single(r => r.Slot == 2).MainStat.Stat);
    }

    [Fact]
    public void Minimums_ShouldTradeFocusForTheRequiredStat()
    {
        // Slot 6: uma runa com muito HP e pouca VEL, outra com VEL suficiente para o mínimo
        Rune[] pool =
        [
            .. Enumerable.Range(1, 5).Select(slot => NewRune(slot, RuneSet.Swift, speed: 10)),
            NewRune(6, RuneSet.Violent, speed: 0, hpPercent: 30),
            NewRune(6, RuneSet.Violent, speed: 20, hpPercent: 5),
        ];

        var withoutMinimum = RuneOptimizer.Optimize(Monster, [], pool, Request([RuneSet.Swift], [PveStat.Hp]))[0];
        var withMinimum = RuneOptimizer.Optimize(
            Monster, [], pool, Request([RuneSet.Swift], [PveStat.Hp]) with { Minimums = [new StatRequirement(PveStat.Speed, 190)] })[0];

        Assert.Equal(0, withoutMinimum.Runes.Single(r => r.Slot == 6).Substats.Sum(s => s.Stat == RuneStat.Speed ? s.Value : 0));
        Assert.True(withMinimum.Stats.Total.Speed >= 190);
    }

    [Fact]
    public void UnreachableMinimum_ShouldReturnNoBuild()
    {
        var pool = Enumerable.Range(1, 6).Select(slot => NewRune(slot, RuneSet.Swift, speed: 5)).ToArray();
        var request = Request([RuneSet.Swift], [PveStat.Speed]) with { Minimums = [new StatRequirement(PveStat.Speed, 400)] };

        Assert.Empty(RuneOptimizer.Optimize(Monster, [], pool, request));
    }

    [Fact]
    public void MissingSlot_ShouldReturnNoBuild()
    {
        var pool = Enumerable.Range(1, 5).Select(slot => NewRune(slot, RuneSet.Swift, speed: 5)).ToArray();

        Assert.Empty(RuneOptimizer.Optimize(Monster, [], pool, Request([RuneSet.Swift], [PveStat.Speed])));
    }

    [Fact]
    public void TooManySetPieces_ShouldReturnNoBuild()
    {
        var pool = Enumerable.Range(1, 6).Select(slot => NewRune(slot, RuneSet.Swift, speed: 5)).ToArray();

        Assert.Empty(RuneOptimizer.Optimize(Monster, [], pool, Request([RuneSet.Swift, RuneSet.Violent], [PveStat.Speed])));
    }

    [Fact]
    public void AssumeMaxLevel_ShouldProjectTheMainStatButReturnTheOriginalRune()
    {
        // Slot 2 em +0 com VEL 7 de principal; em +15 vira 42
        Rune[] pool =
        [
            .. Enumerable.Range(1, 6).Where(s => s != 2).Select(slot => NewRune(slot, RuneSet.Swift, speed: 0)),
            NewRune(2, RuneSet.Swift, speed: 0, main: RuneStat.Speed, mainValue: 7, level: 0),
        ];

        var current = RuneOptimizer.Optimize(Monster, [], pool, Request([RuneSet.Swift], [PveStat.Speed]))[0];
        var projected = RuneOptimizer.Optimize(Monster, [], pool, Request([RuneSet.Swift], [PveStat.Speed]) with { AssumeMaxLevel = true })[0];

        Assert.Equal(current.Stats.Total.Speed + 35, projected.Stats.Total.Speed);
        Assert.Equal(0, projected.Runes.Single(r => r.Slot == 2).Level);
    }

    [Fact]
    public void ShouldReturnAtMostMaxResultsOrderedByScore()
    {
        var pool = Enumerable.Range(1, 6)
            .SelectMany(slot => new[] { NewRune(slot, RuneSet.Swift, speed: 5), NewRune(slot, RuneSet.Swift, speed: 8) })
            .ToArray();

        var builds = RuneOptimizer.Optimize(Monster, [], pool, Request([RuneSet.Swift], [PveStat.Speed]) with { MaxResults = 3 });

        Assert.Equal(3, builds.Count);
        Assert.Equal(builds.OrderByDescending(b => b.Score).Select(b => b.Score), builds.Select(b => b.Score));
        Assert.All(builds[0].Runes, r => Assert.Equal(8, r.Substats[0].Value));
    }

    [Fact]
    public void LargeAccount_ShouldFinishQuickly()
    {
        // Conta grande: 3000 runas aleatórias (semente fixa), 6 sets, subs variadas
        var random = new Random(42);
        RuneSet[] sets = [RuneSet.Swift, RuneSet.Violent, RuneSet.Will, RuneSet.Energy, RuneSet.Blade, RuneSet.Rage];
        RuneStat[] subStats = [RuneStat.Speed, RuneStat.HpPercent, RuneStat.AttackPercent, RuneStat.CriticalRate, RuneStat.CriticalDamage, RuneStat.Accuracy];
        var pool = Enumerable.Range(0, 3000).Select(i =>
        {
            var slot = (i % 6) + 1;
            var subs = subStats.OrderBy(_ => random.Next()).Take(4)
                .Select(stat => new RuneSubstat(stat, random.Next(4, 25), false, 0))
                .ToArray();
            return new Rune(10_000 + i, slot, 6, false, sets[random.Next(sets.Length)], 15, 5, 5,
                new RuneStatValue(slot == 2 ? RuneStat.Speed : RuneStat.HpFlat, slot == 2 ? 42 : 2448), null, subs, null);
        }).ToArray();

        var request = Request([RuneSet.Violent, RuneSet.Will], [PveStat.Speed, PveStat.Hp, PveStat.CriticalRate]) with
        {
            Minimums = [new StatRequirement(PveStat.Accuracy, 30)],
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var builds = RuneOptimizer.Optimize(Monster, [], pool, request);
        stopwatch.Stop();

        Assert.NotEmpty(builds);
        Assert.All(builds, b => Assert.True(b.Stats.Total.Accuracy >= 30));
        Assert.All(builds, b => Assert.Contains(RuneSet.Violent, b.Stats.ActiveSets));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Levou {stopwatch.Elapsed}");
    }

    [Fact]
    public void BestArtifacts_ShouldPickCompatibleArtifactsByFocusAndKeepCurrentOnTies()
    {
        Artifact[] pool =
        [
            Artifact(1, ArtifactType.Element, Element.Water, null, ArtifactEffect.HpFlat, 1500, equippedOn: MonsterId),
            Artifact(2, ArtifactType.Element, Element.Water, null, ArtifactEffect.AttackFlat, 100),
            Artifact(3, ArtifactType.Element, Element.Fire, null, ArtifactEffect.AttackFlat, 160), // outro elemento
            Artifact(4, ArtifactType.Archetype, null, Archetype.Attack, ArtifactEffect.DefenseFlat, 100, equippedOn: MonsterId),
            Artifact(5, ArtifactType.Archetype, null, Archetype.Attack, ArtifactEffect.HpFlat, 1500),
            Artifact(6, ArtifactType.Archetype, null, Archetype.Support, ArtifactEffect.AttackFlat, 100), // outro arquétipo
        ];

        var forAttack = RuneOptimizer.BestArtifacts(Monster, Archetype.Attack, pool, [PveStat.Attack]);
        var forSpeed = RuneOptimizer.BestArtifacts(Monster, Archetype.Attack, pool, [PveStat.Speed]);

        // ATK: o de elemento com ATK; no de arquétipo nenhum tem ATK compatível, fica o atual
        Assert.Equal([2L, 4L], forAttack.Select(a => a.GameId));

        // VEL não depende do principal: mantém os dois atuais
        Assert.Equal([1L, 4L], forSpeed.Select(a => a.GameId));
    }

    private static Artifact Artifact(long id, ArtifactType type, Element? element, Archetype? archetype, ArtifactEffect main, int value, long? equippedOn = null) =>
        new(id, type, element, archetype, 5, 5, 15, new ArtifactStatValue(main, value), [], equippedOn);

    private static OptimizerRequest Request(RuneSet[] sets, PveStat[] focus) =>
        new(sets, new Dictionary<int, IReadOnlyList<RuneStat>>(), focus, [], AssumeMaxLevel: false);

    private static Rune NewRune(
        int slot,
        RuneSet set,
        int speed,
        int hpPercent = 0,
        RuneStat? main = null,
        int mainValue = 0,
        int level = 15)
    {
        // Principal fixo dos slots ímpares; nos pares, HP flat (neutro para os testes) salvo indicação
        var mainStat = main ?? (slot switch { 1 => RuneStat.AttackFlat, 3 => RuneStat.DefenseFlat, _ => RuneStat.HpFlat });
        RuneSubstat[] subs = speed > 0 || hpPercent > 0
            ? [.. new[] { (RuneStat.Speed, speed), (RuneStat.HpPercent, hpPercent) }.Where(s => s.Item2 > 0).Select(s => new RuneSubstat(s.Item1, s.Item2, false, 0))]
            : [];

        return new Rune(Interlocked.Increment(ref _nextId), slot, 6, false, set, level, 5, 5,
            new RuneStatValue(mainStat, mainValue), null, subs, equippedMonsterId: null);
    }
}
