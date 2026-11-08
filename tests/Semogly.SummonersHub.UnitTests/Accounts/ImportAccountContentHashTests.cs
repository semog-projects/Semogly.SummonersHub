using Semogly.SummonersHub.Application.Accounts.ImportAccount;

namespace Semogly.SummonersHub.UnitTests.Accounts;

public class ImportAccountContentHashTests
{
    private static readonly ImportAccountCommand Command = new(
        1,
        new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.Zero),
        new ImportWizard(12345678, "TestWizard", 50, 4),
        [],
        [],
        [],
        [new ImportCraftItem(1, 1, 150905, 1)]);

    [Fact]
    public void ContentHash_ShouldIgnoreExportDate() =>
        Assert.Equal(
            ImportAccountCommandHandler.ComputeContentHash(Command),
            ImportAccountCommandHandler.ComputeContentHash(Command with { ExportedAt = DateTimeOffset.UtcNow }));

    [Fact]
    public void ContentHash_ShouldChangeWhenContentChanges() =>
        Assert.NotEqual(
            ImportAccountCommandHandler.ComputeContentHash(Command),
            ImportAccountCommandHandler.ComputeContentHash(Command with { Wizard = Command.Wizard with { Level = 51 } }));

    [Fact]
    public void ContentHash_ShouldBeSha256Hex() =>
        Assert.Matches("^[0-9a-f]{64}$", ImportAccountCommandHandler.ComputeContentHash(Command));
}
