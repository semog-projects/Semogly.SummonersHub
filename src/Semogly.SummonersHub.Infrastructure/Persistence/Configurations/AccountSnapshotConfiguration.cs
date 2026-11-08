using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// O snapshot é a raiz do agregado: monstros, runas, artefatos e itens de craft ficam cada um na sua tabela,
/// com chave (snapshot, id do jogo) e exclusão em cascata. Valores pequenos e sempre lidos juntos
/// (subs e skills) ficam em colunas jsonb.
/// </summary>
internal sealed class AccountSnapshotConfiguration : IEntityTypeConfiguration<AccountSnapshot>
{
    public void Configure(EntityTypeBuilder<AccountSnapshot> builder)
    {
        builder.ToTable("AccountSnapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.WizardName).HasMaxLength(100);
        builder.Property(s => s.ContentHash).HasMaxLength(64).IsFixedLength();

        // Idempotência e versionamento por invocador (também protegem contra imports concorrentes)
        builder.HasIndex(s => new { s.WizardId, s.ContentHash }).IsUnique();
        builder.HasIndex(s => new { s.WizardId, s.Version }).IsUnique();

        builder.HasMany(s => s.Monsters).WithOne().HasForeignKey(SnapshotChild.ForeignKey).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Runes).WithOne().HasForeignKey(SnapshotChild.ForeignKey).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Artifacts).WithOne().HasForeignKey(SnapshotChild.ForeignKey).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.CraftItems).WithOne().HasForeignKey(SnapshotChild.ForeignKey).OnDelete(DeleteBehavior.Cascade);
    }
}

internal static class SnapshotChild
{
    /// <summary>Chave estrangeira em sombra: o domínio não precisa conhecer o id do snapshot nas entidades filhas.</summary>
    public const string ForeignKey = "SnapshotId";
}

internal sealed class MonsterConfiguration : IEntityTypeConfiguration<Monster>
{
    public void Configure(EntityTypeBuilder<Monster> builder)
    {
        builder.ToTable("Monsters");
        builder.HasKey(SnapshotChild.ForeignKey, nameof(Monster.GameId));
        builder.Property(m => m.GameId).ValueGeneratedNever();

        // Horário do servidor do jogo, sem fuso
        builder.Property(m => m.CreatedAt).HasColumnType("timestamp without time zone");

        builder.OwnsOne(m => m.BaseStats);
        builder.OwnsMany(m => m.Skills, skills => skills.ToJson());
    }
}

internal sealed class RuneConfiguration : IEntityTypeConfiguration<Rune>
{
    public void Configure(EntityTypeBuilder<Rune> builder)
    {
        builder.ToTable("Runes");
        builder.HasKey(SnapshotChild.ForeignKey, nameof(Rune.GameId));
        builder.Property(r => r.GameId).ValueGeneratedNever();
        builder.HasIndex(SnapshotChild.ForeignKey, nameof(Rune.EquippedMonsterId));

        builder.OwnsOne(r => r.MainStat);
        builder.OwnsOne(r => r.InnateStat);
        builder.OwnsMany(r => r.Substats, substats => substats.ToJson());
    }
}

internal sealed class ArtifactConfiguration : IEntityTypeConfiguration<Artifact>
{
    public void Configure(EntityTypeBuilder<Artifact> builder)
    {
        builder.ToTable("Artifacts");
        builder.HasKey(SnapshotChild.ForeignKey, nameof(Artifact.GameId));
        builder.Property(a => a.GameId).ValueGeneratedNever();
        builder.HasIndex(SnapshotChild.ForeignKey, nameof(Artifact.EquippedMonsterId));

        builder.OwnsOne(a => a.MainStat, main => main.Property(m => m.Value).HasPrecision(10, 2));
        builder.OwnsMany(a => a.Substats, substats => substats.ToJson());
    }
}

internal sealed class CraftItemConfiguration : IEntityTypeConfiguration<CraftItem>
{
    public void Configure(EntityTypeBuilder<CraftItem> builder)
    {
        builder.ToTable("CraftItems");
        builder.HasKey(SnapshotChild.ForeignKey, nameof(CraftItem.GameId));
        builder.Property(c => c.GameId).ValueGeneratedNever();
    }
}
