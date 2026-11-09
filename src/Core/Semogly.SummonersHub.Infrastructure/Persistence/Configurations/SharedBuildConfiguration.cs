using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Infrastructure.Identity;

namespace Semogly.SummonersHub.Infrastructure.Persistence.Configurations;

internal sealed class SharedBuildConfiguration : IEntityTypeConfiguration<SharedBuild>
{
    public void Configure(EntityTypeBuilder<SharedBuild> builder)
    {
        builder.ToTable("SharedBuilds");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Token).HasMaxLength(SharedBuild.TokenLength).IsFixedLength();
        builder.HasIndex(b => b.Token).IsUnique();
        builder.HasIndex(b => new { b.OwnerId, b.WizardId, b.MonsterId });

        // O link deixa de existir se o usuário ou a versão do import forem apagados
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.OwnerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AccountSnapshot>().WithMany().HasForeignKey(b => b.SnapshotId).OnDelete(DeleteBehavior.Cascade);
    }
}
