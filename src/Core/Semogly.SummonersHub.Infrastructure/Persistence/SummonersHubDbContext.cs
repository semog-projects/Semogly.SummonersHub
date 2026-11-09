using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Infrastructure.Identity;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

/// <summary>
/// Banco único da aplicação: contas importadas, usuários do Identity e as chaves de Data Protection
/// (que assinam o cookie de login e precisam sobreviver a restarts e a múltiplas instâncias).
/// </summary>
public sealed class SummonersHubDbContext(DbContextOptions<SummonersHubDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork, IDataProtectionKeyContext
{
    public DbSet<AccountSnapshot> AccountSnapshots => Set<AccountSnapshot>();

    public DbSet<WizardAccount> WizardAccounts => Set<WizardAccount>();

    public DbSet<InventoryMark> InventoryMarks => Set<InventoryMark>();

    public DbSet<OptimizerPreset> OptimizerPresets => Set<OptimizerPreset>();

    public DbSet<SharedBuild> SharedBuilds => Set<SharedBuild>();

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Tabelas do Identity (AspNetUsers, AspNetRoles...)
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(SummonersHubDbContext).Assembly);
    }
}
