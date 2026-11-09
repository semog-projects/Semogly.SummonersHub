using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence.Configurations;

internal sealed class InventoryMarkConfiguration : IEntityTypeConfiguration<InventoryMark>
{
    public void Configure(EntityTypeBuilder<InventoryMark> builder)
    {
        builder.ToTable("InventoryMarks");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        // Uma marcação por item do invocador
        builder.HasIndex(m => new { m.WizardId, m.Kind, m.ItemId }).IsUnique();

        // text[] no PostgreSQL; o domínio expõe a lista somente leitura e altera pelo campo
        builder.Property(m => m.Tags)
            .HasField("_tags")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnType("text[]");

        // Removido junto com o vínculo do invocador
        builder.HasOne<WizardAccount>()
            .WithMany()
            .HasForeignKey(m => m.WizardId)
            .HasPrincipalKey(w => w.WizardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
