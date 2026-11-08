using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Application.GameData.GetMonsters;
using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.Infrastructure.GameData;

namespace Semogly.SummonersHub.UnitTests.GameData;

public class MonsterCatalogTests
{
    private static readonly EmbeddedMonsterCatalog Catalog = new();

    [Fact]
    public void Catalog_ShouldLoadEmbeddedBestiary() =>
        Assert.True(Catalog.Count > 3000, $"Esperava mais de 3000 monstros, encontrou {Catalog.Count}");

    [Fact]
    public void Find_ShouldReturnAwakenedMonster()
    {
        var lapis = Catalog.Find(19811);

        Assert.NotNull(lapis);
        Assert.Equal("Lapis", lapis.Name);
        Assert.Equal(Element.Water, lapis.Element);
        Assert.Equal(Archetype.Attack, lapis.Archetype);
        Assert.Equal(1, lapis.AwakenLevel);
        Assert.EndsWith(".png", lapis.ImageFilename, StringComparison.Ordinal);
    }

    [Fact]
    public void Find_ShouldReturnNullForUnknownId() =>
        Assert.Null(Catalog.Find(1));

    [Theory]
    [InlineData(19811)]
    [InlineData(21911)]
    public void Find_ShouldResolveMonstersFromExportFixture(int com2usId) =>
        Assert.NotNull(Catalog.Find(com2usId));

    [Fact]
    public async Task GetMonsters_ShouldIgnoreUnknownAndDuplicatedIds()
    {
        var handler = new GetMonstersQueryHandler(Catalog);

        var result = await handler.HandleAsync(new GetMonstersQuery([19811, 19811, 1]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetMonstersQuery.MaxIds + 1)]
    public async Task GetMonsters_ShouldRejectInvalidIdCount(int count)
    {
        var handler = new GetMonstersQueryHandler(Catalog);

        var result = await handler.HandleAsync(new GetMonstersQuery(Enumerable.Range(1, count).ToArray()), CancellationToken.None);

        Assert.Equal(GameDataErrors.InvalidIdCount, result.Error);
    }
}
