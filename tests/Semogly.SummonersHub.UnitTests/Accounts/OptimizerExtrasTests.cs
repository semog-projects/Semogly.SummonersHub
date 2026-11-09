using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

/// <summary>Sets de equipe, grinds simulados e presets do otimizador.</summary>
public class OptimizerExtrasTests
{
    private const long MonsterId = 1;

    private static readonly Monster Monster = new(
        MonsterId, 19811, 40, 6, Element.Water,
        new BaseStats(Hp: 10000, Attack: 701, Defense: 600, Speed: 100, Resistance: 15, Accuracy: 0, CriticalRate: 15, CriticalDamage: 50),
        [], false, null);

    [Fact]
    public void TeamSetBuffs_ShouldAddPercentOfBaseOrFlatPoints()
    {
        var total = new StatValues(10000, 701, 600, 100, 15, 50, 15, 0);

        var buffed = TeamSetBuffs.Apply(total, Monster.BaseStats, [RuneSet.Fight, RuneSet.Fight, RuneSet.Accuracy, RuneSet.Swift]);

        // 2 × ceil(701 × 8%) = 2 × 57; PRE +10; Swift não é set de equipe
        Assert.Equal(701 + 114, buffed.Attack);
        Assert.Equal(10, buffed.Accuracy);
        Assert.Equal(total.Speed, buffed.Speed);
    }

    [Fact]
    public void WithMaxGrinds_ShouldRaiseOnlyGrindableSubsBelowTheLegendaryMax()
    {
        RuneSubstat[] subs =
        [
            new(RuneStat.Speed, 10, false, 2),
            new(RuneStat.AttackPercent, 8, false, 10),
            new(RuneStat.CriticalRate, 6, false, 0),
        ];
        var rune = new Rune(1, 2, 6, false, RuneSet.Swift, 15, 5, 5, new RuneStatValue(RuneStat.Speed, 42), null, subs, null);

        var grinded = RuneProjection.WithMaxGrinds(rune).Substats;

        Assert.Equal(5, grinded[0].GrindValue); // VEL: grind lendário até 5
        Assert.Equal(10, grinded[1].GrindValue); // ATK%: já no máximo
        Assert.Equal(0, grinded[2].GrindValue); // Taxa Crít. não aceita grind
    }

    [Fact]
    public void Optimizer_ShouldApplyTeamSetsAndSimulatedGrinds()
    {
        var pool = Enumerable.Range(1, 6)
            .Select(slot => new Rune(slot, slot, 6, false, RuneSet.Swift, 15, 5, 5,
                new RuneStatValue(slot == 2 ? RuneStat.Speed : RuneStat.HpFlat, slot == 2 ? 42 : 0), null,
                [new RuneSubstat(RuneStat.Speed, 10, false, 0)], null))
            .ToArray();
        var request = new OptimizerRequest([RuneSet.Swift], new Dictionary<int, IReadOnlyList<RuneStat>>(), [PveStat.Speed, PveStat.Attack], [], AssumeMaxLevel: false);

        var plain = RuneOptimizer.Optimize(Monster, [], pool, request)[0];
        var boosted = RuneOptimizer.Optimize(Monster, [], pool, request with { SimulateGrinds = true, TeamSets = [RuneSet.Fight] })[0];

        // 6 subs de VEL com grind lendário (+5 cada); Fight: +ceil(701 × 8%)
        Assert.Equal(plain.Stats.Total.Speed + 30, boosted.Stats.Total.Speed);
        Assert.Equal(plain.Stats.Total.Attack + 57, boosted.Stats.Total.Attack);
        Assert.True(boosted.Score > plain.Score);
    }

    [Fact]
    public void GemOptions_ShouldFollowTheGameRules()
    {
        // Slot 1 (ATK fixo) com uma sub já gemada: só ela pode ser trocada, sem repetir atributos nem virar DEF
        RuneSubstat[] subs =
        [
            new(RuneStat.Speed, 10, false, 0),
            new(RuneStat.HpPercent, 8, true, 0),
            new(RuneStat.CriticalRate, 6, false, 0),
        ];
        var rune = new Rune(7, 1, 6, false, RuneSet.Violent, 15, 5, 5, new RuneStatValue(RuneStat.AttackFlat, 160), null, subs, null);

        var options = RuneProjection.GemOptions(rune).ToList();

        Assert.All(options, gem => Assert.Equal(RuneStat.HpPercent, gem.From));
        Assert.Equal(
            [RuneStat.HpFlat, RuneStat.AttackPercent, RuneStat.CriticalDamage, RuneStat.Resistance, RuneStat.Accuracy],
            options.Select(gem => gem.To));
        Assert.Equal(13, options.Single(gem => gem.To == RuneStat.AttackPercent).Value); // gem lendária: ATK% até 13
    }

    [Fact]
    public void Optimizer_ShouldSimulateTheBestGemForTheFocus()
    {
        // Runas sem VEL: com gems, a sub de RES vira VEL (gem lendária até 10) e a grind lendária soma +5
        var pool = Enumerable.Range(1, 6)
            .Select(slot => new Rune(slot, slot, 6, false, RuneSet.Swift, 15, 5, 5,
                new RuneStatValue(slot == 2 ? RuneStat.HpPercent : RuneStat.HpFlat, 0), null,
                [new RuneSubstat(RuneStat.Resistance, 6, false, 0)], null))
            .ToArray();
        var request = new OptimizerRequest([RuneSet.Swift], new Dictionary<int, IReadOnlyList<RuneStat>>(), [PveStat.Speed], [], AssumeMaxLevel: false);

        var plain = RuneOptimizer.Optimize(Monster, [], pool, request)[0];
        var gemmed = RuneOptimizer.Optimize(Monster, [], pool, request with { SimulateGems = true, SimulateGrinds = true });

        Assert.Empty(plain.Gems);
        Assert.Equal(plain.Stats.Total.Speed + (6 * 15), gemmed[0].Stats.Total.Speed);
        Assert.Equal(6, gemmed[0].Gems.Count);
        Assert.All(gemmed[0].Gems, gem => Assert.Equal((RuneStat.Resistance, RuneStat.Speed, 10), (gem.From, gem.To, gem.Value)));
        Assert.Single(gemmed); // as mesmas runas com outras gems não se repetem no resultado
    }

    [Fact]
    public void Preset_ShouldNormalizeAndValidateTheName()
    {
        var preset = OptimizerPreset.Create(Guid.NewGuid(), "  Violent   VEL ", "{}", DateTimeOffset.UnixEpoch);

        Assert.Equal("Violent VEL", preset.Name);
        Assert.Throws<DomainException>(() => OptimizerPreset.Create(Guid.NewGuid(), "   ", "{}", DateTimeOffset.UnixEpoch));
        Assert.Throws<DomainException>(() => OptimizerPreset.Create(Guid.NewGuid(), new string('x', 41), "{}", DateTimeOffset.UnixEpoch));
    }
}
