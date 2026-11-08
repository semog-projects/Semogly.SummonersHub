using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class PveTeamEvaluatorTests
{
    private static readonly PveTeam Team = new(
        "gb-test",
        Dungeon.GiantsKeep,
        "Teste",
        null,
        [
            new PveTeamSlot("Bernard", Element.Wind, "Barra de ataque", [new StatRequirement(PveStat.Speed, 180)]),
            new PveTeamSlot("Belladeon", Element.Light, "Quebra de DEF", [new StatRequirement(PveStat.Accuracy, 45)]),
            new PveTeamSlot("Veromos", Element.Dark, "Cleanse", []),
        ]);

    [Fact]
    public void ShouldClassifyEachSlot()
    {
        PveCandidate[] monsters =
        [
            Candidate(1, "Bernard", Element.Wind, speed: 190),
            Candidate(2, "Belladeon", Element.Light, accuracy: 30),
        ];

        var result = PveTeamEvaluator.Evaluate(Team, monsters);

        Assert.Equal([PveSlotStatus.Ready, PveSlotStatus.BelowRequirements, PveSlotStatus.Missing], result.Slots.Select(s => s.Status));
        Assert.Equal([new UnmetRequirement(PveStat.Accuracy, 45, 30)], result.Slots[1].Unmet);
        Assert.Null(result.Slots[2].Candidate);
        Assert.Equal(1, result.ReadySlots);
        Assert.False(result.IsReady);
    }

    [Fact]
    public void ShouldMatchByNameAndElement()
    {
        // Bernard de outro elemento não serve
        var result = PveTeamEvaluator.Evaluate(Team, [Candidate(1, "Bernard", Element.Fire, speed: 250)]);

        Assert.Equal(PveSlotStatus.Missing, result.Slots[0].Status);
    }

    [Fact]
    public void ShouldPickTheCandidateClosestToTheRequirements()
    {
        PveCandidate[] monsters =
        [
            Candidate(1, "Bernard", Element.Wind, speed: 150, stars: 6),
            Candidate(2, "Bernard", Element.Wind, speed: 175, stars: 5),
            Candidate(3, "Bernard", Element.Wind, speed: 120, stars: 6),
        ];

        var slot = PveTeamEvaluator.Evaluate(Team, monsters).Slots[0];

        Assert.Equal(2, slot.Candidate?.MonsterId);
        Assert.Equal(PveSlotStatus.BelowRequirements, slot.Status);
    }

    [Fact]
    public void SameMonster_ShouldNotFillTwoSlots()
    {
        var team = Team with
        {
            Slots = [new PveTeamSlot("Veromos", Element.Dark, "Cleanse", []), new PveTeamSlot("Veromos", Element.Dark, "Cleanse", [])],
        };

        var result = PveTeamEvaluator.Evaluate(team, [Candidate(1, "Veromos", Element.Dark)]);

        Assert.Equal([PveSlotStatus.Ready, PveSlotStatus.Missing], result.Slots.Select(s => s.Status));
    }

    [Fact]
    public void AllSlotsReady_ShouldMakeTeamReady()
    {
        PveCandidate[] monsters =
        [
            Candidate(1, "Bernard", Element.Wind, speed: 180),
            Candidate(2, "Belladeon", Element.Light, accuracy: 45),
            Candidate(3, "Veromos", Element.Dark),
        ];

        Assert.True(PveTeamEvaluator.Evaluate(Team, monsters).IsReady);
    }

    private static PveCandidate Candidate(long id, string name, Element element, int speed = 100, int accuracy = 0, int stars = 6) =>
        new(id, 1, name, element, stars, 40, new StatValues(10000, 800, 600, speed, 15, 50, 15, accuracy));
}
