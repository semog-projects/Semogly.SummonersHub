using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class WizardAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Link_ShouldBeOwnedOnlyByLinkingUser()
    {
        var owner = Guid.NewGuid();

        var account = WizardAccount.Link(87538174, owner, Now);

        Assert.True(account.IsOwnedBy(owner));
        Assert.False(account.IsOwnedBy(Guid.NewGuid()));
        Assert.Equal(87538174, account.WizardId);
        Assert.Equal(Now, account.LinkedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Link_ShouldRejectInvalidWizardId(long wizardId) =>
        Assert.Throws<DomainException>(() => WizardAccount.Link(wizardId, Guid.NewGuid(), Now));

    [Fact]
    public void Link_ShouldRequireUser() =>
        Assert.Throws<DomainException>(() => WizardAccount.Link(87538174, Guid.Empty, Now));
}
