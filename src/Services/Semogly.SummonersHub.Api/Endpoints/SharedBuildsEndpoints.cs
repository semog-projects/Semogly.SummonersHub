using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.SharedBuilds;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class SharedBuildsEndpoints
{
    public static IEndpointRouteBuilder MapSharedBuildsEndpoints(this IEndpointRouteBuilder app)
    {
        var owner = app.MapGroup("/api").WithTags("Shared builds").RequireAuthorization();

        owner.MapPost("/accounts/{wizardId:long}/monsters/{monsterId:long}/shares", Create)
            .WithName("CreateShareLink")
            .WithSummary("Cria (ou devolve) o link público da build do monstro na versão atual do import");

        owner.MapGet("/accounts/{wizardId:long}/monsters/{monsterId:long}/shares", List)
            .WithName("ListShareLinks")
            .WithSummary("Links de build do monstro criados pelo usuário");

        owner.MapDelete("/shares/{token}", Revoke)
            .WithName("RevokeShareLink")
            .WithSummary("Revoga um link de build do usuário");

        // Público: quem tem o link vê a build, sem dados do invocador
        app.MapGet("/api/shared/{token}", Get)
            .WithTags("Shared builds")
            .WithName("GetSharedBuild")
            .WithSummary("Build compartilhada (pública, somente leitura)")
            .AllowAnonymous();

        return app;
    }

    private static async Task<Results<Ok<ShareLinkResponse>, ProblemHttpResult>> Create(
        long wizardId,
        long monsterId,
        ICommandHandler<CreateShareLinkCommand, ShareLinkResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateShareLinkCommand(wizardId, monsterId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyList<ShareLinkResponse>>, ProblemHttpResult>> List(
        long wizardId,
        long monsterId,
        IQueryHandler<ListShareLinksQuery, IReadOnlyList<ShareLinkResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListShareLinksQuery(wizardId, monsterId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Revoke(
        string token,
        ICommandHandler<RevokeShareLinkCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RevokeShareLinkCommand(token), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<SharedBuildResponse>, ProblemHttpResult>> Get(
        string token,
        IQueryHandler<GetSharedBuildQuery, SharedBuildResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSharedBuildQuery(token), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
