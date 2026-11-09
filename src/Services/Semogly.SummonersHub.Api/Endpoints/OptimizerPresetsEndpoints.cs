using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.OptimizerPresets;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class OptimizerPresetsEndpoints
{
    public static IEndpointRouteBuilder MapOptimizerPresetsEndpoints(this IEndpointRouteBuilder app)
    {
        // Presets são do usuário logado (valem para qualquer invocador dele)
        var api = app.MapGroup("/api/optimizer-presets").WithTags("Optimizer presets").RequireAuthorization();

        api.MapGet("/", List)
            .WithName("ListOptimizerPresets")
            .WithSummary("Presets do otimizador do usuário, em ordem alfabética");

        api.MapPost("/", Save)
            .WithName("SaveOptimizerPreset")
            .WithSummary("Salva os critérios com um nome (substitui o preset de mesmo nome)");

        api.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteOptimizerPreset")
            .WithSummary("Exclui um preset do usuário");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<OptimizerPresetResponse>>> List(
        IQueryHandler<ListOptimizerPresetsQuery, IReadOnlyList<OptimizerPresetResponse>> handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok((await handler.HandleAsync(new ListOptimizerPresetsQuery(), cancellationToken)).Value);

    private static async Task<Results<Ok<OptimizerPresetResponse>, ProblemHttpResult>> Save(
        SaveOptimizerPresetCommand command,
        ICommandHandler<SaveOptimizerPresetCommand, OptimizerPresetResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        Guid id,
        ICommandHandler<DeleteOptimizerPresetCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteOptimizerPresetCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
