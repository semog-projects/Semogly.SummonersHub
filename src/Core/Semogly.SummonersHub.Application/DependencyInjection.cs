using Microsoft.Extensions.DependencyInjection;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.CheckPveTeams;
using Semogly.SummonersHub.Application.Accounts.CompareSnapshots;
using Semogly.SummonersHub.Application.Accounts.GetAccountOverview;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.GetInventoryStats;
using Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;
using Semogly.SummonersHub.Application.Accounts.ListAccounts;
using Semogly.SummonersHub.Application.Accounts.ListArtifacts;
using Semogly.SummonersHub.Application.Accounts.ListMonsters;
using Semogly.SummonersHub.Application.Accounts.ListRunes;
using Semogly.SummonersHub.Application.Accounts.ListSnapshots;
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
        services.AddScoped<IQueryHandler<GetInventoryStatsQuery, InventoryStatsResponse>, GetInventoryStatsQueryHandler>();
        services.AddScoped<IQueryHandler<ListMonstersQuery, IReadOnlyList<MonsterListItem>>, ListMonstersQueryHandler>();
        services.AddScoped<IQueryHandler<ListRunesQuery, IReadOnlyList<RuneListItem>>, ListRunesQueryHandler>();
        services.AddScoped<IQueryHandler<ListArtifactsQuery, IReadOnlyList<ArtifactListItem>>, ListArtifactsQueryHandler>();
        services.AddScoped<IQueryHandler<ListSnapshotsQuery, IReadOnlyList<AccountSummaryResponse>>, ListSnapshotsQueryHandler>();
        services.AddScoped<IQueryHandler<CompareSnapshotsQuery, SnapshotComparisonResponse>, CompareSnapshotsQueryHandler>();
        services.AddScoped<IQueryHandler<CheckPveTeamsQuery, IReadOnlyList<PveTeamCheck>>, CheckPveTeamsQueryHandler>();
        services.AddScoped<IQueryHandler<GetMonsterDetailQuery, MonsterDetailResponse>, GetMonsterDetailQueryHandler>();
        services.AddScoped<IQueryHandler<GetMonstersQuery, IReadOnlyList<MonsterDefinition>>, GetMonstersQueryHandler>();

        return services;
    }
}
