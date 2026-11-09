using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts.UpdateInventoryMarks;

/// <summary>
/// Bloqueia/desbloqueia e adiciona/remove tags de várias runas ou artefatos de uma vez (um item é um lote de 1).
/// Campos nulos ou vazios não mexem naquele aspecto.
/// </summary>
public sealed record UpdateInventoryMarksCommand(
    long WizardId,
    InventoryItemKind Kind,
    IReadOnlyList<long> ItemIds,
    bool? Locked,
    IReadOnlyList<string>? AddTags,
    IReadOnlyList<string>? RemoveTags) : ICommand
{
    /// <summary>Uma conta tem no máximo alguns milhares de runas.</summary>
    public const int MaxItems = 5000;
}

internal sealed class UpdateInventoryMarksCommandHandler(
    IInventoryMarkRepository marks,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateInventoryMarksCommand>
{
    public async Task<Result> HandleAsync(UpdateInventoryMarksCommand command, CancellationToken cancellationToken)
    {
        if (Validate(command) is { } error)
        {
            return Result.Failure(error);
        }

        if (!await wizardAccounts.IsOwnedByAsync(command.WizardId, userContext.UserId, cancellationToken))
        {
            return Result.Failure(AccountErrors.NotFound(command.WizardId));
        }

        var existing = (await marks.ListAsync(command.WizardId, command.Kind, cancellationToken)).ToDictionary(m => m.ItemId);

        foreach (var itemId in command.ItemIds.Distinct())
        {
            var isNew = !existing.TryGetValue(itemId, out var mark);
            mark ??= InventoryMark.Create(command.WizardId, command.Kind, itemId);

            if (command.Locked is { } locked)
            {
                mark.SetLocked(locked);
            }

            foreach (var tag in command.AddTags ?? [])
            {
                mark.AddTag(tag);
            }

            foreach (var tag in command.RemoveTags ?? [])
            {
                mark.RemoveTag(tag);
            }

            // Marcação vazia não precisa existir
            if (isNew && !mark.IsEmpty)
            {
                marks.Add(mark);
            }
            else if (!isNew && mark.IsEmpty)
            {
                marks.Remove(mark);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static Error? Validate(UpdateInventoryMarksCommand command)
    {
        if (command.ItemIds.Count is 0 or > UpdateInventoryMarksCommand.MaxItems || command.ItemIds.Any(id => id <= 0))
        {
            return AccountErrors.InvalidMarks($"Informe de 1 a {UpdateInventoryMarksCommand.MaxItems} itens com ids válidos.");
        }

        if (command.Locked is null && (command.AddTags ?? []).Count == 0 && (command.RemoveTags ?? []).Count == 0)
        {
            return AccountErrors.InvalidMarks("Nada a alterar: informe o bloqueio ou tags.");
        }

        var tags = (command.AddTags ?? []).Concat(command.RemoveTags ?? []);
        if (tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > InventoryMark.MaxTagLength)
            || (command.AddTags ?? []).Count > InventoryMark.MaxTags)
        {
            return AccountErrors.InvalidMarks(
                $"Tags devem ter de 1 a {InventoryMark.MaxTagLength} caracteres, no máximo {InventoryMark.MaxTags} por item.");
        }

        return null;
    }
}
