using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.GetAccountOverview;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.GetInventoryStats;
using Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;
using Semogly.SummonersHub.Application.Accounts.ListAccounts;
using Semogly.SummonersHub.Application.Accounts.ListMonsters;

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

        api.MapGet("/accounts/{wizardId:long}/overview", GetAccountOverview)
            .WithName("GetAccountOverview")
            .WithSummary("Visão geral do último import: monstros por estrelas, elemento e despertar e ranking de velocidade");

        api.MapGet("/accounts/{wizardId:long}/inventory", GetInventoryStats)
            .WithName("GetInventoryStats")
            .WithSummary("Indicadores de runas, artefatos, gems e grinds do último import");

        api.MapGet("/accounts/{wizardId:long}/monsters", ListMonsters)
            .WithName("ListMonsters")
            .WithSummary("Todos os monstros do último import com atributos totais, sets ativos e equipamentos");

        api.MapGet("/accounts/{wizardId:long}/monsters/{monsterId:long}", GetMonsterDetail)
            .WithName("GetMonsterDetail")
            .WithSummary("Monstro com atributos base e totais (base + runas + sets + artefatos), skills, runas e artefatos");

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

    private static async Task<Results<Ok<InventoryStatsResponse>, ProblemHttpResult>> GetInventoryStats(
        long wizardId,
        IQueryHandler<GetInventoryStatsQuery, InventoryStatsResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetInventoryStatsQuery(wizardId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyList<MonsterListItem>>, ProblemHttpResult>> ListMonsters(
        long wizardId,
        IQueryHandler<ListMonstersQuery, IReadOnlyList<MonsterListItem>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListMonstersQuery(wizardId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<MonsterDetailResponse>, ProblemHttpResult>> GetMonsterDetail(
        long wizardId,
        long monsterId,
        IQueryHandler<GetMonsterDetailQuery, MonsterDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMonsterDetailQuery(wizardId, monsterId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<AccountOverviewResponse>, ProblemHttpResult>> GetAccountOverview(
        long wizardId,
        IQueryHandler<GetAccountOverviewQuery, AccountOverviewResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAccountOverviewQuery(wizardId), cancellationToken);
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
