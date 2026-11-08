using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/imports", ImportAccount)
            .WithName("ImportAccount")
            .WithTags("Accounts")
            .WithSummary("Importa a conta exportada pelo SW Exporter (payload já sanitizado pelo front)")
            .WithDescription("201 quando gera uma nova versão; 200 quando o mesmo conteúdo já tinha sido importado.");

        app.MapGet("/api/accounts/{wizardId:long}", GetAccountSummary)
            .WithName("GetAccountSummary")
            .WithTags("Accounts")
            .WithSummary("Resumo do último import do invocador");

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

    private static async Task<Results<Ok<AccountSummaryResponse>, ProblemHttpResult>> GetAccountSummary(
        long wizardId,
        IQueryHandler<GetAccountSummaryQuery, AccountSummaryResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAccountSummaryQuery(wizardId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
