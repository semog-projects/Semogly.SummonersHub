using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.SharedBuilds;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Infrastructure.GameData;
using Semogly.SummonersHub.Infrastructure.Identity;
using Semogly.SummonersHub.Infrastructure.Persistence;

namespace Semogly.SummonersHub.Infrastructure;

public static class DependencyInjection
{
    // Nome da connection string injetada pelo AppHost do Aspire
    private const string DatabaseConnectionName = "summonershub-db";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        UseDatabaseUrlWhenNoConnectionString(builder.Configuration);

        // Registra o DbContext com pooling, retry, health check e telemetria
        builder.AddNpgsqlDbContext<SummonersHubDbContext>(DatabaseConnectionName);

        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SummonersHubDbContext>());
        builder.Services.AddIdentityServices(builder.Configuration);

        builder.Services.AddScoped<IAccountSnapshotRepository, AccountSnapshotRepository>();
        builder.Services.AddScoped<IWizardAccountRepository, WizardAccountRepository>();
        builder.Services.AddScoped<IInventoryMarkRepository, InventoryMarkRepository>();
        builder.Services.AddScoped<IOptimizerPresetRepository, OptimizerPresetRepository>();
        builder.Services.AddScoped<ISharedBuildRepository, SharedBuildRepository>();
        builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        builder.Services.AddScoped<IManualItemRepository, ManualItemRepository>();
        builder.Services.AddScoped<IAccountReadService, AccountReadService>();
        builder.Services.AddScoped<IAccountReadServiceForShares, AccountReadService>();
        builder.Services.AddSingleton<IMonsterCatalog, EmbeddedMonsterCatalog>();
        builder.Services.AddSingleton<IPveTeamCatalog, EmbeddedPveTeamCatalog>();
        builder.Services.AddSingleton<IDungeonCatalog, EmbeddedDungeonCatalog>();
        builder.Services.AddSingleton<ISkillCatalog, EmbeddedSkillCatalog>();

        return builder;
    }

    /// <summary>
    /// Fora do Aspire (ex.: Vercel + Neon) o banco chega como URL em <c>DATABASE_URL</c>: vira a connection string
    /// esperada pelo Npgsql. Uma connection string configurada explicitamente sempre tem prioridade.
    /// </summary>
    private static void UseDatabaseUrlWhenNoConnectionString(IConfigurationManager configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString(DatabaseConnectionName)))
        {
            return;
        }

        var url = DatabaseUrl.EnvironmentVariables
            .Select(name => configuration[name])
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (url is not null)
        {
            configuration[$"ConnectionStrings:{DatabaseConnectionName}"] = DatabaseUrl.ToConnectionString(url);
        }
    }
}
