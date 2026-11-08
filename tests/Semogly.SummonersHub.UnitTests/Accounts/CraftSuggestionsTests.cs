using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class CraftSuggestionsTests
{
    private static readonly CraftItemCode LegendarySwiftSpeed = new(RuneSet.Swift, RuneStat.Speed, 5);

    [Fact]
    public void Grind_ShouldGainFromCurrentGrindToTheRange()
    {
        // Grind lendário de VEL: 4 a 5. Sub com VEL 10 + 1 de grind
        var rune = NewRune(1, RuneSet.Swift, [Sub(RuneStat.Speed, 10, grind: 1), Sub(RuneStat.HpPercent, 8)]);

        var suggestion = Assert.Single(CraftSuggestions.For(CraftItemType.Grindstone, LegendarySwiftSpeed, [rune]));

        Assert.Equal(0, suggestion.SubstatIndex);
        Assert.Equal(11, suggestion.CurrentValue);
        Assert.Equal(14, suggestion.MinValue);
        Assert.Equal(15, suggestion.MaxValue);
        Assert.Equal(RuneEfficiency.SubstatPoints(RuneStat.Speed, 3), suggestion.MinEfficiencyGain);
        Assert.Equal(RuneEfficiency.SubstatPoints(RuneStat.Speed, 4), suggestion.MaxEfficiencyGain);
    }

    [Fact]
    public void Grind_ShouldSkipRunesThatCannotImprove()
    {
        Rune[] runes =
        [
            NewRune(1, RuneSet.Swift, [Sub(RuneStat.Speed, 10, grind: 5)]), // grind já no máximo
            NewRune(2, RuneSet.Swift, [Sub(RuneStat.HpPercent, 8)]), // sem a sub
            NewRune(3, RuneSet.Violent, [Sub(RuneStat.Speed, 10)]), // outro set
            NewRune(4, RuneSet.Swift, [], innate: new RuneStatValue(RuneStat.Speed, 5)), // só na inata
        ];

        Assert.Empty(CraftSuggestions.For(CraftItemType.Grindstone, LegendarySwiftSpeed, runes));
    }

    [Fact]
    public void Immemorial_ShouldApplyToAnySet()
    {
        var immemorial = LegendarySwiftSpeed with { Set = RuneSet.Immemorial };
        var rune = NewRune(1, RuneSet.Violent, [Sub(RuneStat.Speed, 10)]);

        Assert.Single(CraftSuggestions.For(CraftItemType.ImmemorialGrindstone, immemorial, [rune]));
    }

    [Fact]
    public void Gem_ShouldReplaceTheWeakestSubAndCountTheLostGrind()
    {
        // Gem lendária de VEL: 7 a 10. A pior sub é ATK flat 5 (+0 de grind)
        var rune = NewRune(1, RuneSet.Swift, [Sub(RuneStat.HpPercent, 20, grind: 5), Sub(RuneStat.AttackFlat, 5), Sub(RuneStat.CriticalRate, 9)]);

        var suggestion = Assert.Single(CraftSuggestions.For(CraftItemType.EnchantedGem, LegendarySwiftSpeed, [rune]));

        Assert.Equal(RuneStat.AttackFlat, suggestion.TargetStat);
        Assert.Equal((7, 10), (suggestion.MinValue, suggestion.MaxValue));
        var lost = RuneEfficiency.SubstatPoints(RuneStat.AttackFlat, 5);
        Assert.Equal(RuneEfficiency.SubstatPoints(RuneStat.Speed, 10) - lost, suggestion.MaxEfficiencyGain);
    }

    [Fact]
    public void Gem_ShouldOnlyReplaceTheSameStatWhenTheRuneAlreadyHasIt()
    {
        var rune = NewRune(1, RuneSet.Swift, [Sub(RuneStat.Speed, 4), Sub(RuneStat.AttackFlat, 5)]);

        var suggestion = Assert.Single(CraftSuggestions.For(CraftItemType.EnchantedGem, LegendarySwiftSpeed, [rune]));

        Assert.Equal(RuneStat.Speed, suggestion.TargetStat);
        Assert.Equal(4, suggestion.CurrentValue);
    }

    [Fact]
    public void Gem_ShouldRespectGameRestrictions()
    {
        var defenseGem = new CraftItemCode(RuneSet.Swift, RuneStat.DefensePercent, 5);
        Rune[] runes =
        [
            NewRune(1, RuneSet.Swift, [Sub(RuneStat.AttackFlat, 5, gemmed: true)]), // já tem gem
            NewRune(2, RuneSet.Swift, [Sub(RuneStat.AttackFlat, 5)], slot: 1), // slot 1 não aceita DEF
            NewRune(3, RuneSet.Swift, [Sub(RuneStat.AttackFlat, 5)], main: RuneStat.DefensePercent), // já é o principal
        ];

        Assert.Empty(CraftSuggestions.For(CraftItemType.EnchantedGem, defenseGem, runes));
    }

    [Fact]
    public void AncientItems_ShouldBeIgnoredBecauseTheirRangesAreUnknown()
    {
        var rune = NewRune(1, RuneSet.Swift, [Sub(RuneStat.Speed, 10)]);

        Assert.Empty(CraftSuggestions.For(CraftItemType.AncientGrindstone, LegendarySwiftSpeed, [rune]));
    }

    private static RuneSubstat Sub(RuneStat stat, int value, int grind = 0, bool gemmed = false) => new(stat, value, gemmed, grind);

    private static Rune NewRune(
        long id,
        RuneSet set,
        RuneSubstat[] subs,
        RuneStatValue? innate = null,
        int slot = 2,
        RuneStat main = RuneStat.AttackPercent) =>
        new(id, slot, 6, false, set, 15, 5, 5, new RuneStatValue(main, 63), innate, subs, null);
}
