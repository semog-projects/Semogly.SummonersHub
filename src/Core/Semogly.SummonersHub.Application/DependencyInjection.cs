using Microsoft.Extensions.DependencyInjection;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.CheckPveTeams;
using Semogly.SummonersHub.Application.Accounts.CompareSnapshots;
using Semogly.SummonersHub.Application.Accounts.GetAccountOverview;
using Semogly.SummonersHub.Application.Accounts.GetAccountSummary;
using Semogly.SummonersHub.Application.Accounts.GetCraftSuggestions;
using Semogly.SummonersHub.Application.Accounts.GetInventoryStats;
using Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;
using Semogly.SummonersHub.Application.Accounts.GetMonsterSkills;
using Semogly.SummonersHub.Application.Accounts.ImportAccount;
using Semogly.SummonersHub.Application.Accounts.ListAccounts;
using Semogly.SummonersHub.Application.Accounts.ListArtifacts;
using Semogly.SummonersHub.Application.Accounts.ListMonsters;
using Semogly.SummonersHub.Application.Accounts.ListRunes;
using Semogly.SummonersHub.Application.Accounts.OptimizeRunes;
using Semogly.SummonersHub.Application.Accounts.OptimizerPresets;
using Semogly.SummonersHub.Application.Accounts.SharedBuilds;
using Semogly.SummonersHub.Application.Accounts.UpdateInventoryMarks;
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
        services.AddScoped<ICommandHandler<UpdateInventoryMarksCommand>, UpdateInventoryMarksCommandHandler>();
        services.AddScoped<IQueryHandler<ListOptimizerPresetsQuery, IReadOnlyList<OptimizerPresetResponse>>, ListOptimizerPresetsQueryHandler>();
        services.AddScoped<ICommandHandler<SaveOptimizerPresetCommand, OptimizerPresetResponse>, SaveOptimizerPresetCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteOptimizerPresetCommand>, DeleteOptimizerPresetCommandHandler>();
        services.AddScoped<ICommandHandler<CreateShareLinkCommand, ShareLinkResponse>, CreateShareLinkCommandHandler>();
        services.AddScoped<IQueryHandler<ListShareLinksQuery, IReadOnlyList<ShareLinkResponse>>, ListShareLinksQueryHandler>();
        services.AddScoped<ICommandHandler<RevokeShareLinkCommand>, RevokeShareLinkCommandHandler>();
        services.AddScoped<IQueryHandler<GetSharedBuildQuery, SharedBuildResponse>, GetSharedBuildQueryHandler>();
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
        services.AddScoped<IQueryHandler<GetCraftSuggestionsQuery, IReadOnlyList<CraftSuggestionGroup>>, GetCraftSuggestionsQueryHandler>();
        services.AddScoped<IQueryHandler<OptimizeRunesQuery, OptimizeRunesResponse>, OptimizeRunesQueryHandler>();
        services.AddScoped<IQueryHandler<GetMonsterSkillsQuery, MonsterSkillsResponse>, GetMonsterSkillsQueryHandler>();
        services.AddScoped<IQueryHandler<GetMonsterDetailQuery, MonsterDetailResponse>, GetMonsterDetailQueryHandler>();
        services.AddScoped<IQueryHandler<GetMonstersQuery, IReadOnlyList<MonsterDefinition>>, GetMonstersQueryHandler>();

        return services;
    }
}
