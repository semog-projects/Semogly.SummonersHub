using Microsoft.Extensions.Hosting;
using Semogly.SummonersHub.Infrastructure.Persistence;

namespace Semogly.SummonersHub.Infrastructure;

public static class DependencyInjection
{
    // Nome da connection string injetada pelo AppHost do Aspire
    private const string DatabaseConnectionName = "summonershub-db";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        // Registra o DbContext com pooling, retry, health check e telemetria
        builder.AddNpgsqlDbContext<SummonersHubDbContext>(DatabaseConnectionName);

        return builder;
    }
}
