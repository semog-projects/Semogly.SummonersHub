using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.ManualItems;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class ManualItemsEndpoints
{
    public static IEndpointRouteBuilder MapManualItemsEndpoints(this IEndpointRouteBuilder app)
    {
        // Monstros e gems/grinds criados à mão, somados à leitura do último import
        var api = app.MapGroup("/api/accounts/{wizardId:long}/manual-items").WithTags("Manual items").RequireAuthorization();

        api.MapGet("/", List)
            .WithName("ListManualItems")
            .WithSummary("Monstros e gems/grinds criados à mão no invocador");

        api.MapPost("/monsters", CreateMonster)
            .WithName("CreateManualMonster")
            .WithSummary("Cria um monstro 6★ nível 40 do bestiário, com skills no máximo");

        api.MapPost("/craft-items", CreateCraftItem)
            .WithName("CreateManualCraftItem")
            .WithSummary("Cria gems ou grinds no inventário");

        api.MapDelete("/{gameId:long}", Delete)
            .WithName("DeleteManualItem")
            .WithSummary("Exclui um monstro ou gem/grind manual (id negativo)");

        return app;
    }

    private static async Task<Results<Ok<ManualItemsResponse>, ProblemHttpResult>> List(
        long wizardId,
        IQueryHandler<ListManualItemsQuery, ManualItemsResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListManualItemsQuery(wizardId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<ManualMonsterResponse>, ProblemHttpResult>> CreateMonster(
        long wizardId,
        CreateManualMonsterRequest request,
        ICommandHandler<CreateManualMonsterCommand, ManualMonsterResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateManualMonsterCommand(wizardId, request.MasterId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<ManualCraftItemResponse>, ProblemHttpResult>> CreateCraftItem(
        long wizardId,
        CreateManualCraftItemRequest request,
        ICommandHandler<CreateManualCraftItemCommand, ManualCraftItemResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateManualCraftItemCommand(wizardId, request.Type, request.Set, request.Stat, request.Rarity, request.Amount);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        long wizardId,
        long gameId,
        ICommandHandler<DeleteManualItemCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteManualItemCommand(wizardId, gameId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}

internal sealed record CreateManualMonsterRequest(int MasterId);

/// <param name="Stat">Código do atributo no jogo (ex.: 8 = VEL).</param>
/// <param name="Rarity">1 comum a 5 lendária.</param>
internal sealed record CreateManualCraftItemRequest(CraftItemType Type, RuneSet Set, int Stat, int Rarity, int Amount);
