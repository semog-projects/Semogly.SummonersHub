using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.GetAccountOverview;

/// <summary>Visão geral do último import do invocador: perfil, distribuição dos monstros e ranking de velocidade.</summary>
public sealed record GetAccountOverviewQuery(long WizardId) : IQuery<AccountOverviewResponse>
{
    public const int SpeedRankingSize = 20;
}

public sealed record AccountOverviewResponse(
    WizardOverview Wizard,
    int Monsters,
    int Runes,
    int Artifacts,
    IReadOnlyList<CountBy<int>> MonstersByStars,
    IReadOnlyList<CountBy<Element>> MonstersByElement,
    IReadOnlyList<CountBy<int>> MonstersByAwakening,
    IReadOnlyList<SpeedRankingEntry> SpeedRanking);

public sealed record WizardOverview(
    long WizardId,
    string Name,
    int Level,
    int? ServerId,
    int Version,
    DateTimeOffset ImportedAt,
    DateTimeOffset? ExportedAt);

public sealed record SpeedRankingEntry(
    long MonsterId,
    int MasterId,
    string? Name,
    Element Element,
    int Stars,
    int Level,
    int BaseSpeed,
    int TotalSpeed,
    IReadOnlyList<RuneSet> ActiveSets);

internal sealed class GetAccountOverviewQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<GetAccountOverviewQuery, AccountOverviewResponse>
{
    private static readonly int[] StarGrades = [1, 2, 3, 4, 5, 6];
    private static readonly int[] AwakenLevels = [0, 1, 2];

    public async Task<Result<AccountOverviewResponse>> HandleAsync(GetAccountOverviewQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var snapshot = result.Value;

        var monsters = snapshot.Monsters
            .Select(m => (Monster: m, Definition: catalog.Find(m.MasterId)))
            .ToList();

        return new AccountOverviewResponse(
            new WizardOverview(
                snapshot.WizardId,
                snapshot.WizardName,
                snapshot.WizardLevel,
                snapshot.WizardServerId,
                snapshot.Version,
                snapshot.ImportedAt,
                snapshot.ExportedAt),
            snapshot.Monsters.Count,
            snapshot.Runes.Count,
            snapshot.Artifacts.Count,
            // Todas as chaves aparecem, mesmo com zero, para o gráfico manter a mesma escala
            [.. StarGrades.Select(stars => new CountBy<int>(stars, monsters.Count(m => m.Monster.Stars == stars)))],
            [.. Enum.GetValues<Element>().Select(element => new CountBy<Element>(element, monsters.Count(m => m.Monster.Element == element)))],
            // Monstros fora do bestiário (lançados depois do último sync) ficam fora desta contagem
            [.. AwakenLevels.Select(level => new CountBy<int>(level, monsters.Count(m => m.Definition?.AwakenLevel == level)))],
            SpeedRanking(snapshot, monsters));
    }

    /// <summary>Monstros mais rápidos pela velocidade total. Materiais (rainbowmons, devilmons...) ficam de fora.</summary>
    private static List<SpeedRankingEntry> SpeedRanking(AccountSnapshot snapshot, List<(Monster Monster, MonsterDefinition? Definition)> monsters)
    {
        var equipment = new EquipmentIndex(snapshot);

        return monsters
            .Where(m => m.Definition?.Archetype != Archetype.Material)
            .Select(m =>
            {
                var stats = equipment.StatsOf(m.Monster);
                return new SpeedRankingEntry(
                    m.Monster.GameId,
                    m.Monster.MasterId,
                    m.Definition?.Name,
                    m.Monster.Element,
                    m.Monster.Stars,
                    m.Monster.Level,
                    stats.Base.Speed,
                    stats.Total.Speed,
                    stats.ActiveSets);
            })
            .OrderByDescending(e => e.TotalSpeed)
            .ThenBy(e => e.MonsterId)
            .Take(GetAccountOverviewQuery.SpeedRankingSize)
            .ToList();
    }
}
