using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

/// <summary>
/// Usada apenas pelo dotnet-ef para gerar migrations, sem depender
/// da connection string que o AppHost injeta em tempo de execução.
/// </summary>
internal sealed class SummonersHubDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SummonersHubDbContext>
{
    public SummonersHubDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SummonersHubDbContext>()
            .UseNpgsql("Host=localhost;Database=summonershub-db")
            .Options;

        return new SummonersHubDbContext(options);
    }
}
