using Semogly.SummonersHub.Domain.GameData;
using Semogly.SummonersHub.Infrastructure.GameData;

namespace Semogly.SummonersHub.UnitTests.GameData;

public class SkillCatalogTests
{
    private static readonly EmbeddedSkillCatalog Catalog = new();

    [Fact]
    public void Catalog_ShouldLoadEmbeddedSkills() =>
        Assert.True(Catalog.Count > 5000, $"Esperava mais de 5000 habilidades, encontrou {Catalog.Count}");

    [Fact]
    public void Find_ShouldReturnFormulaHitsAndUpgrades()
    {
        // Magic Shot (Lapis): 4 golpes de 1,9 × ATK
        var skill = Catalog.Find(10606);

        Assert.NotNull(skill);
        Assert.Equal("Magic Shot", skill.Name);
        Assert.Equal(4, skill.Hits);
        Assert.Equal("1.9*{ATK}", skill.FormulaText);
        Assert.Contains("\"ATK\"", skill.Formula, StringComparison.Ordinal);
        Assert.Equal([10, 0, 10, 15], skill.DamageUpgrades);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 10)]
    [InlineData(3, 10)]
    [InlineData(5, 35)]
    [InlineData(9, 35)]
    public void DamageBonusAt_ShouldSumTheUpgradesUpToTheLevel(int level, int bonus)
    {
        var skill = new SkillDefinition(1, "Teste", 1, 1, false, false, 5, "1.0*{ATK}", "[[\"ATK\",\"*\",1.0]]", [10, 0, 10, 15]);

        Assert.Equal(bonus, skill.DamageBonusAt(level));
    }
}
