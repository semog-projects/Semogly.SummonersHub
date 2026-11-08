using Microsoft.Extensions.DependencyInjection;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.GetAccountOverview;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.GetMonsterStats;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;
using Semogly.SummonersHub.Application.Accounts.ListAccounts;
using Semogly.SummonersHub.Application.GameData.GetMonsters;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application;

public static class DependencyInjection
{
    /// <summary>Registra os handlers de casos de uso.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ICommandHandler<ImportAccountCommand, ImportAccountResponse>, ImportAccountCommandHandler>();
        services.AddScoped<IQueryHandler<GetAccountSummaryQuery, AccountSummaryResponse>, GetAccountSummaryQueryHandler>();
        services.AddScoped<IQueryHandler<GetAccountOverviewQuery, AccountOverviewResponse>, GetAccountOverviewQueryHandler>();
        services.AddScoped<IQueryHandler<ListAccountsQuery, IReadOnlyList<AccountSummaryResponse>>, ListAccountsQueryHandler>();
        services.AddScoped<IQueryHandler<GetMonsterStatsQuery, MonsterStatsResponse>, GetMonsterStatsQueryHandler>();
        services.AddScoped<IQueryHandler<GetMonstersQuery, IReadOnlyList<MonsterDefinition>>, GetMonstersQueryHandler>();

        return services;
    }
}
