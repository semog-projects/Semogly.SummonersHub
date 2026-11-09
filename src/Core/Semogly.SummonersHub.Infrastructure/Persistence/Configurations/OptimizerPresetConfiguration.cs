using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Infrastructure.Identity;

namespace Semogly.SummonersHub.Infrastructure.Persistence.Configurations;

internal sealed class OptimizerPresetConfiguration : IEntityTypeConfiguration<OptimizerPreset>
{
    public void Configure(EntityTypeBuilder<OptimizerPreset> builder)
    {
        builder.ToTable("OptimizerPresets");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(OptimizerPreset.MaxNameLength);
        builder.Property(p => p.Criteria).HasColumnType("jsonb");

        builder.HasIndex(p => new { p.OwnerId, p.Name }).IsUnique();

        // Apagado junto com o usuário
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}
