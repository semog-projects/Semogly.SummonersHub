using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Settings;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // Configurações do usuário logado
        var api = app.MapGroup("/api/settings").WithTags("Settings").RequireAuthorization();

        api.MapGet("/preferences", GetPreferences)
            .WithName("GetPreferences")
            .WithSummary("Preferências do usuário (tema)");

        api.MapPut("/preferences", UpdatePreferences)
            .WithName("UpdatePreferences")
            .WithSummary("Salva as preferências do usuário");

        api.MapGet("/backup", Export)
            .WithName("ExportSettings")
            .WithSummary("Backup das configurações: preferências, presets do otimizador e marcações (sem dados do jogo)");

        api.MapPost("/backup", Restore)
            .WithName("RestoreSettings")
            .WithSummary("Restaura um backup por cima das configurações atuais (tudo ou nada)");

        return app;
    }

    private static async Task<Ok<PreferencesResponse>> GetPreferences(
        IQueryHandler<GetPreferencesQuery, PreferencesResponse> handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok((await handler.HandleAsync(new GetPreferencesQuery(), cancellationToken)).Value);

    private static async Task<Results<Ok<PreferencesResponse>, ProblemHttpResult>> UpdatePreferences(
        UpdatePreferencesCommand command,
        ICommandHandler<UpdatePreferencesCommand, PreferencesResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<Ok<SettingsBackup>> Export(
        IQueryHandler<ExportSettingsQuery, SettingsBackup> handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok((await handler.HandleAsync(new ExportSettingsQuery(), cancellationToken)).Value);

    private static async Task<Results<Ok<RestoreSettingsResponse>, ProblemHttpResult>> Restore(
        SettingsBackup backup,
        ICommandHandler<RestoreSettingsCommand, RestoreSettingsResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RestoreSettingsCommand(backup), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
