using System.Security.Cryptography;
using System.Text.Json;
using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.ImportAccount;

internal sealed class ImportAccountCommandHandler(
    IAccountSnapshotRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<ImportAccountCommand, ImportAccountResponse>
{
    public async Task<Result<ImportAccountResponse>> HandleAsync(ImportAccountCommand command, CancellationToken cancellationToken)
    {
        if (ImportAccountValidator.Validate(command) is { } error)
        {
            return error;
        }

        var contentHash = ComputeContentHash(command);

        // Reenvio do mesmo conteúdo (mesmo export ou novo export sem mudanças): devolve a versão existente
        var existing = await repository.FindByContentHashAsync(command.Wizard.Id, contentHash, cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing, created: false);
        }

        var version = await repository.GetLatestVersionAsync(command.Wizard.Id, cancellationToken) + 1;

        var snapshot = AccountSnapshot.Create(
            new WizardProfile(command.Wizard.Id, command.Wizard.Name.Trim(), command.Wizard.Level, command.Wizard.ServerId),
            version,
            contentHash,
            command.ExportedAt,
            timeProvider.GetUtcNow(),
            command.Monsters.Select(ToMonster),
            command.Runes.Select(ToRune),
            command.Artifacts.Select(ToArtifact),
            command.CraftItems.Select(c => new CraftItem(c.Id, (CraftItemType)c.CraftType, c.CraftTypeId, c.Amount)));

        repository.Add(snapshot);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(snapshot, created: true);
    }

    /// <summary>
    /// SHA-256 do conteúdo sem a data do export: dois exports sem mudanças na conta geram o mesmo hash.
    /// </summary>
    internal static string ComputeContentHash(ImportAccountCommand command)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(command with { ExportedAt = null });
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static Monster ToMonster(ImportMonster m) => new(
        m.Id,
        m.MasterId,
        m.Level,
        m.Stars,
        (Element)m.Attribute,
        new BaseStats(
            m.BaseStats.Hp,
            m.BaseStats.Attack,
            m.BaseStats.Defense,
            m.BaseStats.Speed,
            m.BaseStats.Resistance,
            m.BaseStats.Accuracy,
            m.BaseStats.CriticalRate,
            m.BaseStats.CriticalDamage),
        [.. m.Skills.Select(s => new MonsterSkill(s.Id, s.Level))],
        m.IsHomunculus,
        m.CreatedAt);

    private static Rune ToRune(ImportRune r) => new(
        r.Id,
        r.Slot,
        r.Stars,
        r.IsAncient,
        (RuneSet)r.SetId,
        r.Level,
        r.Rarity,
        r.OriginalRarity,
        new RuneStatValue((RuneStat)r.MainStat.Type, (int)r.MainStat.Value),
        r.InnateStat is { } innate ? new RuneStatValue((RuneStat)innate.Type, (int)innate.Value) : null,
        [.. r.Substats.Select(s => new RuneSubstat((RuneStat)s.Type, s.Value, s.IsGemmed, s.GrindValue))],
        r.EquippedMonsterId);

    private static Artifact ToArtifact(ImportArtifact a)
    {
        var type = (ArtifactType)a.Type;

        // O jogo preenche só um dos dois conforme o tipo (o outro vem 0)
        return new Artifact(
            a.Id,
            type,
            type == ArtifactType.Element ? (Element)a.Attribute : null,
            type == ArtifactType.Archetype ? (Archetype)a.Archetype : null,
            a.Rarity,
            a.OriginalRarity,
            a.Level,
            new ArtifactStatValue((ArtifactEffect)a.MainStat.Type, a.MainStat.Value),
            [.. a.Substats.Select(s => new ArtifactSubstat((ArtifactEffect)s.Type, s.Value, s.Upgrades))],
            a.EquippedMonsterId);
    }

    private static ImportAccountResponse ToResponse(AccountSnapshot snapshot, bool created) => new(
        snapshot.Id,
        snapshot.WizardId,
        snapshot.Version,
        created,
        snapshot.Monsters.Count,
        snapshot.Runes.Count,
        snapshot.Artifacts.Count,
        snapshot.CraftItems.Count);
}
