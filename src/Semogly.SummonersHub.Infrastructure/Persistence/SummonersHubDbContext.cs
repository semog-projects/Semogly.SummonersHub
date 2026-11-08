using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

public sealed class SummonersHubDbContext(DbContextOptions<SummonersHubDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<AccountSnapshot> AccountSnapshots => Set<AccountSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SummonersHubDbContext).Assembly);
}
