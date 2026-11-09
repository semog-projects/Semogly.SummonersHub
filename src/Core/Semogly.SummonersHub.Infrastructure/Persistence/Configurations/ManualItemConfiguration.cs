using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence.Configurations;

internal sealed class ManualMonsterConfiguration : IEntityTypeConfiguration<ManualMonster>
{
    public void Configure(EntityTypeBuilder<ManualMonster> builder)
    {
        builder.ToTable("ManualMonsters");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.HasIndex(m => new { m.WizardId, m.GameId }).IsUnique();
    }
}

internal sealed class ManualCraftItemConfiguration : IEntityTypeConfiguration<ManualCraftItem>
{
    public void Configure(EntityTypeBuilder<ManualCraftItem> builder)
    {
        builder.ToTable("ManualCraftItems");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.HasIndex(c => new { c.WizardId, c.GameId }).IsUnique();
    }
}
