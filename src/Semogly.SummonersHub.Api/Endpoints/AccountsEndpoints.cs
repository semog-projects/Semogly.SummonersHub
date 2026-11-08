using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.GetMonsterStats;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;
using Semogly.SummonersHub.Application.Accounts.ListAccounts;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        // Dados da conta só para o usuário logado que é dono do invocador
        var api = app.MapGroup("/api").WithTags("Accounts").RequireAuthorization();

        api.MapPost("/imports", ImportAccount)
            .WithName("ImportAccount")
            .WithSummary("Importa a conta exportada pelo SW Exporter (payload já sanitizado pelo front)")
            .WithDescription(
                "201 quando gera uma nova versão; 200 quando o mesmo conteúdo já tinha sido importado. " +
                "O primeiro import vincula o invocador ao usuário; 409 se ele já for de outro usuário.");

        api.MapGet("/accounts", ListAccounts)
            .WithName("ListAccounts")
            .WithSummary("Invocadores do usuário com o resumo do último import de cada um");

        api.MapGet("/accounts/{wizardId:long}", GetAccountSummary)
            .WithName("GetAccountSummary")
            .WithSummary("Resumo do último import do invocador");

        api.MapGet("/accounts/{wizardId:long}/monsters/{monsterId:long}/stats", GetMonsterStats)
            .WithName("GetMonsterStats")
            .WithSummary("Atributos totais do monstro (base + runas + sets + artefatos) no último import");

        return app;
    }

    private static async Task<Results<Created<ImportAccountResponse>, Ok<ImportAccountResponse>, ProblemHttpResult>> ImportAccount(
        ImportAccountCommand command,
        ICommandHandler<ImportAccountCommand, ImportAccountResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        return result.Value.Created
            ? TypedResults.Created($"/api/accounts/{result.Value.WizardId}", result.Value)
            : TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<IReadOnlyList<AccountSummaryResponse>>> ListAccounts(
        IQueryHandler<ListAccountsQuery, IReadOnlyList<AccountSummaryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListAccountsQuery(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<MonsterStatsResponse>, ProblemHttpResult>> GetMonsterStats(
        long wizardId,
        long monsterId,
        IQueryHandler<GetMonsterStatsQuery, MonsterStatsResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMonsterStatsQuery(wizardId, monsterId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<AccountSummaryResponse>, ProblemHttpResult>> GetAccountSummary(
        long wizardId,
        IQueryHandler<GetAccountSummaryQuery, AccountSummaryResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAccountSummaryQuery(wizardId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
