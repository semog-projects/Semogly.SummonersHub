using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Infrastructure.Identity;

namespace Semogly.SummonersHub.Infrastructure.Persistence.Configurations;

internal sealed class WizardAccountConfiguration : IEntityTypeConfiguration<WizardAccount>
{
    public void Configure(EntityTypeBuilder<WizardAccount> builder)
    {
        builder.ToTable("WizardAccounts");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        // Um invocador tem um único dono (também resolve dois usuários reivindicando ao mesmo tempo)
        builder.HasIndex(w => w.WizardId).IsUnique();
        builder.HasIndex(w => w.OwnerId);

        // FK para o usuário do Identity sem expor o tipo de infraestrutura no domínio
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(w => w.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}
