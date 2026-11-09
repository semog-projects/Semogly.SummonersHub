using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class InventoryMarkTests
{
    [Fact]
    public void Tags_ShouldBeNormalizedAndNotRepeat()
    {
        var mark = InventoryMark.Create(1, InventoryItemKind.Rune, 10);

        mark.AddTag("  Siege   defesa ");
        mark.AddTag("siege DEFESA");
        mark.AddTag("RTA");

        Assert.Equal(["Siege defesa", "RTA"], mark.Tags);
    }

    [Fact]
    public void RemoveTag_ShouldIgnoreCase()
    {
        var mark = InventoryMark.Create(1, InventoryItemKind.Rune, 10);
        mark.AddTag("RTA");

        mark.RemoveTag("rta");

        Assert.Empty(mark.Tags);
    }

    [Fact]
    public void IsEmpty_ShouldConsiderLockAndTags()
    {
        var mark = InventoryMark.Create(1, InventoryItemKind.Artifact, 10);
        Assert.True(mark.IsEmpty);

        mark.SetLocked(true);
        Assert.False(mark.IsEmpty);

        mark.SetLocked(false);
        mark.AddTag("x");
        Assert.False(mark.IsEmpty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("uma tag comprida demais para caber")]
    public void InvalidTag_ShouldThrow(string tag) =>
        Assert.Throws<DomainException>(() => InventoryMark.Create(1, InventoryItemKind.Rune, 10).AddTag(tag));

    [Fact]
    public void TooManyTags_ShouldThrow()
    {
        var mark = InventoryMark.Create(1, InventoryItemKind.Rune, 10);
        for (var i = 0; i < InventoryMark.MaxTags; i++)
        {
            mark.AddTag($"tag {i}");
        }

        Assert.Throws<DomainException>(() => mark.AddTag("mais uma"));
    }
}
