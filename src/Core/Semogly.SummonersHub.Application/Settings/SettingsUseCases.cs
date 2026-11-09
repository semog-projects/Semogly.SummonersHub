using System.Text.Json;
using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Settings;

public sealed record PreferencesResponse(ThemePreference Theme);

/// <summary>Preferências do usuário logado (padrão, se ele nunca salvou).</summary>
public sealed record GetPreferencesQuery : IQuery<PreferencesResponse>;

public sealed record UpdatePreferencesCommand(ThemePreference Theme) : ICommand<PreferencesResponse>;

/// <summary>Configurações do usuário num arquivo: preferências, presets do otimizador e marcações.</summary>
/// <param name="Format">Sempre <see cref="FormatName"/>, para reconhecer o arquivo.</param>
/// <param name="InventoryMarks">Bloqueios e tags de runas, artefatos e monstros, por invocador.</param>
/// <remarks>Não traz nada do export do jogo: os dados da conta voltam reimportando o JSON do jogo.</remarks>
public sealed record SettingsBackup(
    string Format,
    int Version,
    DateTimeOffset ExportedAt,
    PreferencesResponse? Preferences,
    IReadOnlyList<BackupPreset>? OptimizerPresets,
    IReadOnlyList<BackupMark>? InventoryMarks)
{
    public const string FormatName = "summonershub-settings";

    public const int CurrentVersion = 1;

    public const int MaxPresets = 200;

    public const int MaxMarks = 50_000;
}

/// <param name="Criteria">Critérios em JSON (objeto), no formato do formulário do otimizador.</param>
public sealed record BackupPreset(string Name, JsonElement Criteria);

/// <param name="ItemId">Id da runa, do artefato ou do monstro no jogo.</param>
public sealed record BackupMark(long WizardId, InventoryItemKind Kind, long ItemId, bool Locked, IReadOnlyList<string>? Tags);

public sealed record ExportSettingsQuery : IQuery<SettingsBackup>;

/// <summary>
/// Restaura um backup por cima do que existe: presets substituem os de mesmo nome e marcações substituem as do
/// mesmo item. Tudo ou nada: com qualquer item inválido, nada é gravado.
/// </summary>
public sealed record RestoreSettingsCommand(SettingsBackup Backup) : ICommand<RestoreSettingsResponse>;

/// <param name="SkippedPresets">Presets novos que passariam do limite por usuário.</param>
/// <param name="SkippedMarks">Marcações de invocadores que não são do usuário.</param>
public sealed record RestoreSettingsResponse(int Presets, int Marks, int SkippedPresets, int SkippedMarks);

public static class SettingsErrors
{
    public static Error Invalid(string message) => Error.Validation("Settings.Invalid", message);
}

internal sealed class GetPreferencesQueryHandler(IUserPreferencesRepository preferences, IUserContext userContext)
    : IQueryHandler<GetPreferencesQuery, PreferencesResponse>
{
    public async Task<Result<PreferencesResponse>> HandleAsync(GetPreferencesQuery query, CancellationToken cancellationToken) =>
        new PreferencesResponse((await preferences.FindAsync(userContext.UserId, cancellationToken))?.Theme ?? UserPreferences.DefaultTheme);
}

internal sealed class UpdatePreferencesCommandHandler(
    IUserPreferencesRepository preferences,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<UpdatePreferencesCommand, PreferencesResponse>
{
    public async Task<Result<PreferencesResponse>> HandleAsync(UpdatePreferencesCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.Theme))
        {
            return SettingsErrors.Invalid("Tema inválido.");
        }

        var now = timeProvider.GetUtcNow();
        var current = await preferences.FindAsync(userContext.UserId, cancellationToken);
        if (current is null)
        {
            current = UserPreferences.Create(userContext.UserId, now);
            preferences.Add(current);
        }

        current.SetTheme(command.Theme, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PreferencesResponse(current.Theme);
    }
}

internal sealed class ExportSettingsQueryHandler(
    IUserPreferencesRepository preferences,
    IOptimizerPresetRepository presets,
    IWizardAccountRepository wizardAccounts,
    IInventoryMarkRepository marks,
    IUserContext userContext,
    TimeProvider timeProvider) : IQueryHandler<ExportSettingsQuery, SettingsBackup>
{
    public async Task<Result<SettingsBackup>> HandleAsync(ExportSettingsQuery query, CancellationToken cancellationToken)
    {
        // Em sequência: o DbContext não aceita consultas concorrentes
        var userId = userContext.UserId;
        var theme = (await preferences.FindAsync(userId, cancellationToken))?.Theme ?? UserPreferences.DefaultTheme;
        var savedPresets = await presets.ListAsync(userId, cancellationToken);
        var wizardIds = await wizardAccounts.ListWizardIdsAsync(userId, cancellationToken);
        var savedMarks = wizardIds.Count == 0 ? [] : await marks.ListByWizardsAsync(wizardIds, cancellationToken);

        return new SettingsBackup(
            SettingsBackup.FormatName,
            SettingsBackup.CurrentVersion,
            timeProvider.GetUtcNow(),
            new PreferencesResponse(theme),
            [.. savedPresets.Select(p => new BackupPreset(p.Name, ParseCriteria(p.Criteria)))],
            [.. savedMarks
                .OrderBy(m => m.WizardId).ThenBy(m => m.Kind).ThenBy(m => m.ItemId)
                .Select(m => new BackupMark(m.WizardId, m.Kind, m.ItemId, m.IsLocked, m.Tags))]);
    }

    private static JsonElement ParseCriteria(string criteria)
    {
        using var document = JsonDocument.Parse(criteria);
        return document.RootElement.Clone();
    }
}

internal sealed class RestoreSettingsCommandHandler(
    IUserPreferencesRepository preferences,
    IOptimizerPresetRepository presets,
    IWizardAccountRepository wizardAccounts,
    IInventoryMarkRepository marks,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<RestoreSettingsCommand, RestoreSettingsResponse>
{
    public async Task<Result<RestoreSettingsResponse>> HandleAsync(RestoreSettingsCommand command, CancellationToken cancellationToken)
    {
        var backup = command.Backup;
        if (backup.Format != SettingsBackup.FormatName || backup.Version != SettingsBackup.CurrentVersion)
        {
            return SettingsErrors.Invalid("O arquivo não é um backup de configurações do SummonersHub (ou é de uma versão desconhecida).");
        }

        var backupPresets = backup.OptimizerPresets ?? [];
        var backupMarks = backup.InventoryMarks ?? [];
        if (backupPresets.Count > SettingsBackup.MaxPresets || backupMarks.Count > SettingsBackup.MaxMarks)
        {
            return SettingsErrors.Invalid(
                $"O backup passa do limite de {SettingsBackup.MaxPresets} presets ou {SettingsBackup.MaxMarks} marcações.");
        }

        var userId = userContext.UserId;
        var now = timeProvider.GetUtcNow();

        try
        {
            if (backup.Preferences is { } prefs)
            {
                if (!Enum.IsDefined(prefs.Theme))
                {
                    return SettingsErrors.Invalid("Tema inválido.");
                }

                var current = await preferences.FindAsync(userId, cancellationToken);
                if (current is null)
                {
                    current = UserPreferences.Create(userId, now);
                    preferences.Add(current);
                }

                current.SetTheme(prefs.Theme, now);
            }

            var presetResult = await RestorePresetsAsync(userId, backupPresets, now, cancellationToken);
            if (!presetResult.IsSuccess)
            {
                return presetResult.Error;
            }

            var markResult = await RestoreMarksAsync(userId, backupMarks, cancellationToken);
            if (!markResult.IsSuccess)
            {
                return markResult.Error;
            }

            // Nada foi gravado até aqui: um erro acima descarta tudo junto com o contexto da requisição
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var (restoredPresets, skippedPresets) = presetResult.Value;
            var (restoredMarks, skippedMarks) = markResult.Value;
            return new RestoreSettingsResponse(restoredPresets, restoredMarks, skippedPresets, skippedMarks);
        }
        catch (DomainException ex)
        {
            return SettingsErrors.Invalid(ex.Message);
        }
    }

    private async Task<Result<(int Restored, int Skipped)>> RestorePresetsAsync(
        Guid userId,
        IReadOnlyList<BackupPreset> backupPresets,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = (await presets.ListAsync(userId, cancellationToken))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        int restored = 0, skipped = 0;

        foreach (var item in backupPresets)
        {
            if (item.Criteria.ValueKind != JsonValueKind.Object)
            {
                return SettingsErrors.Invalid($"Os critérios do preset \"{item.Name}\" devem ser um objeto JSON.");
            }

            var name = OptimizerPreset.NormalizeName(item.Name);
            var criteria = item.Criteria.GetRawText();

            if (existing.TryGetValue(name, out var preset))
            {
                preset.Update(criteria, now);
            }
            else if (existing.Count >= OptimizerPreset.MaxPerUser)
            {
                skipped++;
                continue;
            }
            else
            {
                preset = OptimizerPreset.Create(userId, name, criteria, now);
                presets.Add(preset);
                existing[name] = preset;
            }

            restored++;
        }

        return (restored, skipped);
    }

    private async Task<Result<(int Restored, int Skipped)>> RestoreMarksAsync(
        Guid userId,
        IReadOnlyList<BackupMark> backupMarks,
        CancellationToken cancellationToken)
    {
        if (backupMarks.Count == 0)
        {
            return (0, 0);
        }

        if (backupMarks.Any(m => !Enum.IsDefined(m.Kind)))
        {
            return SettingsErrors.Invalid("Tipo de item inválido numa marcação.");
        }

        // Só invocadores do usuário: marcações de outras contas são ignoradas
        var owned = (await wizardAccounts.ListWizardIdsAsync(userId, cancellationToken)).ToHashSet();
        var toRestore = backupMarks.Where(m => owned.Contains(m.WizardId)).ToList();
        var skipped = backupMarks.Count - toRestore.Count;

        var wizardIds = toRestore.Select(m => m.WizardId).Distinct().ToList();
        var existing = wizardIds.Count == 0
            ? []
            : (await marks.ListByWizardsAsync(wizardIds, cancellationToken)).ToDictionary(m => (m.WizardId, m.Kind, m.ItemId));

        foreach (var item in toRestore)
        {
            var key = (item.WizardId, item.Kind, item.ItemId);
            var isNew = !existing.TryGetValue(key, out var mark);
            mark ??= InventoryMark.Create(item.WizardId, item.Kind, item.ItemId);

            // O backup substitui a marcação: limpa as tags atuais e aplica as do arquivo
            foreach (var tag in mark.Tags.ToList())
            {
                mark.RemoveTag(tag);
            }

            foreach (var tag in item.Tags ?? [])
            {
                mark.AddTag(tag);
            }

            mark.SetLocked(item.Locked);

            if (isNew && !mark.IsEmpty)
            {
                marks.Add(mark);
                existing[key] = mark;
            }
            else if (!isNew && mark.IsEmpty)
            {
                marks.Remove(mark);
                existing.Remove(key);
            }
        }

        return (toRestore.Count, skipped);
    }
}
