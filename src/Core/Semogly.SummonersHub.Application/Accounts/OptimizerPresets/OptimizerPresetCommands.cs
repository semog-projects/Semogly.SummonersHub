using System.Text.Json;
using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts.OptimizerPresets;

/// <param name="Criteria">Critérios em JSON (objeto), no formato do formulário do otimizador.</param>
public sealed record OptimizerPresetResponse(Guid Id, string Name, JsonElement Criteria, DateTimeOffset UpdatedAt);

/// <summary>Presets do otimizador do usuário, em ordem alfabética.</summary>
public sealed record ListOptimizerPresetsQuery : IQuery<IReadOnlyList<OptimizerPresetResponse>>;

/// <summary>Salva os critérios com um nome; se o usuário já tem um preset com esse nome, substitui.</summary>
public sealed record SaveOptimizerPresetCommand(string Name, JsonElement Criteria) : ICommand<OptimizerPresetResponse>;

public sealed record DeleteOptimizerPresetCommand(Guid Id) : ICommand;

public static class OptimizerPresetErrors
{
    public static readonly Error NotFound = Error.NotFound("OptimizerPresets.NotFound", "Preset não encontrado.");

    public static readonly Error TooMany = Error.Validation(
        "OptimizerPresets.TooMany",
        $"Limite de {OptimizerPreset.MaxPerUser} presets atingido. Exclua algum para salvar outro.");

    public static Error Invalid(string message) => Error.Validation("OptimizerPresets.Invalid", message);
}

internal static class OptimizerPresetMapping
{
    public static OptimizerPresetResponse ToResponse(this OptimizerPreset preset)
    {
        using var document = JsonDocument.Parse(preset.Criteria);
        return new OptimizerPresetResponse(preset.Id, preset.Name, document.RootElement.Clone(), preset.UpdatedAt);
    }
}

internal sealed class ListOptimizerPresetsQueryHandler(IOptimizerPresetRepository presets, IUserContext userContext)
    : IQueryHandler<ListOptimizerPresetsQuery, IReadOnlyList<OptimizerPresetResponse>>
{
    public async Task<Result<IReadOnlyList<OptimizerPresetResponse>>> HandleAsync(ListOptimizerPresetsQuery query, CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<OptimizerPresetResponse>>(
            [.. (await presets.ListAsync(userContext.UserId, cancellationToken)).Select(p => p.ToResponse())]);
}

internal sealed class SaveOptimizerPresetCommandHandler(
    IOptimizerPresetRepository presets,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<SaveOptimizerPresetCommand, OptimizerPresetResponse>
{
    public async Task<Result<OptimizerPresetResponse>> HandleAsync(SaveOptimizerPresetCommand command, CancellationToken cancellationToken)
    {
        if (command.Criteria.ValueKind != JsonValueKind.Object)
        {
            return OptimizerPresetErrors.Invalid("Os critérios do preset devem ser um objeto JSON.");
        }

        var criteria = command.Criteria.GetRawText();
        if (criteria.Length > OptimizerPreset.MaxCriteriaLength)
        {
            return OptimizerPresetErrors.Invalid($"Os critérios do preset devem ter até {OptimizerPreset.MaxCriteriaLength} caracteres.");
        }

        string name;
        try
        {
            name = OptimizerPreset.NormalizeName(command.Name);
        }
        catch (DomainException ex)
        {
            return OptimizerPresetErrors.Invalid(ex.Message);
        }

        var userId = userContext.UserId;
        var now = timeProvider.GetUtcNow();
        var preset = await presets.FindByNameAsync(userId, name, cancellationToken);

        if (preset is null)
        {
            if (await presets.CountAsync(userId, cancellationToken) >= OptimizerPreset.MaxPerUser)
            {
                return OptimizerPresetErrors.TooMany;
            }

            preset = OptimizerPreset.Create(userId, name, criteria, now);
            presets.Add(preset);
        }
        else
        {
            preset.Update(criteria, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return preset.ToResponse();
    }
}

internal sealed class DeleteOptimizerPresetCommandHandler(
    IOptimizerPresetRepository presets,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteOptimizerPresetCommand>
{
    public async Task<Result> HandleAsync(DeleteOptimizerPresetCommand command, CancellationToken cancellationToken)
    {
        var preset = await presets.FindAsync(userContext.UserId, command.Id, cancellationToken);
        if (preset is null)
        {
            return Result.Failure(OptimizerPresetErrors.NotFound);
        }

        presets.Remove(preset);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
