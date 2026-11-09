using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.ManualItems;

/// <param name="GameId">Id negativo do monstro manual (usado nas rotas de monstro).</param>
public sealed record ManualMonsterResponse(long GameId, int MasterId, string? Name, Element? Element, int AwakenLevel, DateTimeOffset CreatedAt);

/// <param name="Stat">Código do atributo no jogo.</param>
public sealed record ManualCraftItemResponse(long GameId, CraftItemType Type, RuneSet Set, int Stat, int Rarity, int Amount, DateTimeOffset CreatedAt);

public sealed record ManualItemsResponse(IReadOnlyList<ManualMonsterResponse> Monsters, IReadOnlyList<ManualCraftItemResponse> CraftItems);

public sealed record ListManualItemsQuery(long WizardId) : IQuery<ManualItemsResponse>;

public sealed record CreateManualMonsterCommand(long WizardId, int MasterId) : ICommand<ManualMonsterResponse>;

/// <param name="Stat">Código do atributo no jogo (ex.: 8 = VEL).</param>
public sealed record CreateManualCraftItemCommand(long WizardId, CraftItemType Type, RuneSet Set, int Stat, int Rarity, int Amount)
    : ICommand<ManualCraftItemResponse>;

public sealed record DeleteManualItemCommand(long WizardId, long GameId) : ICommand;

public static class ManualItemErrors
{
    public static Error Invalid(string message) => Error.Validation("ManualItems.Invalid", message);

    public static readonly Error TooMany = Error.Validation(
        "ManualItems.TooMany",
        $"Limite de {ManualItem.MaxPerWizard} itens manuais por invocador atingido.");

    public static Error NotFound(long gameId) => Error.NotFound("ManualItems.NotFound", $"Item manual {gameId} não encontrado.");
}

/// <summary>Monta a leitura da conta com os itens manuais (ver <see cref="AccountSnapshot.WithManualItems"/>).</summary>
public static class ManualItemsMerge
{
    /// <summary>Monstros sem atributos no bestiário (sincronização antiga) ficam de fora.</summary>
    public static AccountSnapshot Merge(
        AccountSnapshot snapshot,
        IEnumerable<ManualMonster> monsters,
        IEnumerable<ManualCraftItem> craftItems,
        IMonsterCatalog monsterCatalog,
        ISkillCatalog skillCatalog)
    {
        var manualMonsters = monsters
            .Select(manual => (manual, definition: monsterCatalog.Find(manual.MasterId)))
            .Where(m => m.definition?.MaxLevelStats is not null)
            .Select(m => m.manual.ToMonster(
                m.definition!.Element,
                m.definition.MaxLevelStats!,
                [.. (m.definition.SkillIds ?? []).Select(id => new MonsterSkill(id, skillCatalog.Find(id)?.MaxLevel ?? 1))],
                m.definition.IsHomunculus))
            .ToList();

        var manualCraft = craftItems.Select(c => c.ToCraftItem()).ToList();
        return manualMonsters.Count == 0 && manualCraft.Count == 0 ? snapshot : snapshot.WithManualItems(manualMonsters, manualCraft);
    }
}

internal sealed class ListManualItemsQueryHandler(
    IManualItemRepository manualItems,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<ListManualItemsQuery, ManualItemsResponse>
{
    public async Task<Result<ManualItemsResponse>> HandleAsync(ListManualItemsQuery query, CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(query.WizardId, userContext.UserId, cancellationToken))
        {
            return AccountErrors.NotFound(query.WizardId);
        }

        var monsters = await manualItems.ListMonstersAsync(query.WizardId, cancellationToken);
        var craftItems = await manualItems.ListCraftItemsAsync(query.WizardId, cancellationToken);

        return new ManualItemsResponse(
            [.. monsters.OrderBy(m => m.CreatedAt).Select(m => m.ToResponse(catalog))],
            [.. craftItems.OrderBy(c => c.CreatedAt).Select(c => c.ToResponse())]);
    }
}

internal sealed class CreateManualMonsterCommandHandler(
    IManualItemRepository manualItems,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateManualMonsterCommand, ManualMonsterResponse>
{
    public async Task<Result<ManualMonsterResponse>> HandleAsync(CreateManualMonsterCommand command, CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(command.WizardId, userContext.UserId, cancellationToken))
        {
            return AccountErrors.NotFound(command.WizardId);
        }

        if (catalog.Find(command.MasterId) is not { MaxLevelStats: not null, IsHomunculus: false })
        {
            return ManualItemErrors.Invalid("Monstro desconhecido ou sem atributos no bestiário.");
        }

        if (await manualItems.CountAsync(command.WizardId, cancellationToken) >= ManualItem.MaxPerWizard)
        {
            return ManualItemErrors.TooMany;
        }

        var gameId = await manualItems.NextGameIdAsync(command.WizardId, cancellationToken);
        var monster = ManualMonster.Create(command.WizardId, gameId, command.MasterId, timeProvider.GetUtcNow());
        manualItems.Add(monster);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return monster.ToResponse(catalog);
    }
}

internal sealed class CreateManualCraftItemCommandHandler(
    IManualItemRepository manualItems,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateManualCraftItemCommand, ManualCraftItemResponse>
{
    public async Task<Result<ManualCraftItemResponse>> HandleAsync(CreateManualCraftItemCommand command, CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(command.WizardId, userContext.UserId, cancellationToken))
        {
            return AccountErrors.NotFound(command.WizardId);
        }

        if (await manualItems.CountAsync(command.WizardId, cancellationToken) >= ManualItem.MaxPerWizard)
        {
            return ManualItemErrors.TooMany;
        }

        ManualCraftItem item;
        try
        {
            var gameId = await manualItems.NextGameIdAsync(command.WizardId, cancellationToken);
            item = ManualCraftItem.Create(
                command.WizardId, gameId, command.Type, command.Set, (RuneStat)command.Stat, command.Rarity, command.Amount, timeProvider.GetUtcNow());
        }
        catch (DomainException ex)
        {
            return ManualItemErrors.Invalid(ex.Message);
        }

        manualItems.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return item.ToResponse();
    }
}

internal sealed class DeleteManualItemCommandHandler(
    IManualItemRepository manualItems,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteManualItemCommand>
{
    public async Task<Result> HandleAsync(DeleteManualItemCommand command, CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(command.WizardId, userContext.UserId, cancellationToken))
        {
            return Result.Failure(AccountErrors.NotFound(command.WizardId));
        }

        // O id negativo é único entre monstros e itens do invocador
        if ((await manualItems.ListMonstersAsync(command.WizardId, cancellationToken)).FirstOrDefault(m => m.GameId == command.GameId) is { } monster)
        {
            manualItems.Remove(monster);
        }
        else if ((await manualItems.ListCraftItemsAsync(command.WizardId, cancellationToken)).FirstOrDefault(c => c.GameId == command.GameId) is { } item)
        {
            manualItems.Remove(item);
        }
        else
        {
            return Result.Failure(ManualItemErrors.NotFound(command.GameId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class ManualItemMapping
{
    public static ManualMonsterResponse ToResponse(this ManualMonster monster, IMonsterCatalog catalog)
    {
        var definition = catalog.Find(monster.MasterId);
        return new ManualMonsterResponse(monster.GameId, monster.MasterId, definition?.Name, definition?.Element, definition?.AwakenLevel ?? 0, monster.CreatedAt);
    }

    public static ManualCraftItemResponse ToResponse(this ManualCraftItem item)
    {
        var code = CraftItemCode.Parse(item.Code);
        return new ManualCraftItemResponse(item.GameId, item.Type, code.Set, (int)code.Stat, code.Rarity, item.Amount, item.CreatedAt);
    }
}
