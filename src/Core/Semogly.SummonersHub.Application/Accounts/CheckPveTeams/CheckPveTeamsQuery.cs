using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.CheckPveTeams;

/// <summary>Times de referência de cada dungeon conferidos contra os monstros do último import.</summary>
public sealed record CheckPveTeamsQuery(long WizardId) : IQuery<IReadOnlyList<PveTeamCheck>>;

/// <param name="DungeonStats">Andar mais alto da dungeon (SWARFARM), de onde vem a PRE mínima dos debuffers.</param>
public sealed record PveTeamCheck(
    string Id,
    Dungeon Dungeon,
    DungeonStats? DungeonStats,
    string Name,
    string? Notes,
    int ReadySlots,
    bool IsReady,
    IReadOnlyList<PveSlotCheck> Slots);

/// <param name="Monster">Melhor candidato da conta para a vaga, ou nulo se a conta não tem o monstro.</param>
/// <param name="Unmet">Mínimos que o candidato ainda não atinge.</param>
/// <param name="Debuffer">A PRE mínima da vaga vem da RES do boss.</param>
public sealed record PveSlotCheck(
    string MonsterName,
    Element Element,
    string Role,
    bool Debuffer,
    IReadOnlyList<StatRequirement> Requirements,
    PveSlotStatus Status,
    PveCandidate? Monster,
    IReadOnlyList<UnmetRequirement> Unmet);

internal sealed class CheckPveTeamsQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog monsterCatalog,
    IPveTeamCatalog teamCatalog,
    IDungeonCatalog dungeonCatalog) : IQueryHandler<CheckPveTeamsQuery, IReadOnlyList<PveTeamCheck>>
{
    public async Task<Result<IReadOnlyList<PveTeamCheck>>> HandleAsync(CheckPveTeamsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var equipment = new EquipmentIndex(result.Value);
        var candidates = result.Value.Monsters
            .Select(m => new PveCandidate(
                m.GameId,
                m.MasterId,
                monsterCatalog.Find(m.MasterId)?.Name,
                m.Element,
                m.Stars,
                m.Level,
                equipment.StatsOf(m).Total))
            .Where(c => c.Name is not null)
            .ToList();

        return teamCatalog.All()
            .OrderBy(team => team.Dungeon)
            .Select(team =>
            {
                var dungeon = dungeonCatalog.Find(team.Dungeon);
                var evaluation = PveTeamEvaluator.Evaluate(PveTeamEvaluator.WithDungeonAccuracy(team, dungeon), candidates);
                return ToCheck(evaluation, dungeon);
            })
            .ToList();
    }

    private static PveTeamCheck ToCheck(PveTeamEvaluation evaluation, DungeonStats? dungeon) => new(
        evaluation.Team.Id,
        evaluation.Team.Dungeon,
        dungeon,
        evaluation.Team.Name,
        evaluation.Team.Notes,
        evaluation.ReadySlots,
        evaluation.IsReady,
        [.. evaluation.Slots.Select(s => new PveSlotCheck(
            s.Slot.MonsterName,
            s.Slot.Element,
            s.Slot.Role,
            s.Slot.Debuffer,
            s.Slot.Requirements,
            s.Status,
            s.Candidate,
            s.Unmet))]);
}
