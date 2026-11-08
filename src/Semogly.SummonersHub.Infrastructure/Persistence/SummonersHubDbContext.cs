using Microsoft.EntityFrameworkCore;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

/// <summary>Contexto do EF Core. As entidades entram com a importação da conta (issue #4).</summary>
public sealed class SummonersHubDbContext(DbContextOptions<SummonersHubDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SummonersHubDbContext).Assembly);
}
