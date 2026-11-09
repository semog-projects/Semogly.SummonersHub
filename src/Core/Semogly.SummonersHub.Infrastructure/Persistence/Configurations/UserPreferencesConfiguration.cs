using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Infrastructure.Identity;

namespace Semogly.SummonersHub.Infrastructure.Persistence.Configurations;

internal sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(p => p.OwnerId);
        builder.Property(p => p.OwnerId).ValueGeneratedNever();

        // Texto legível no banco (System, Light, Dark)
        builder.Property(p => p.Theme).HasConversion<string>().HasMaxLength(16);

        // Apagadas junto com o usuário
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserPreferences>(p => p.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}
