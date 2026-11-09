using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.GetMonsterSkills;

/// <summary>
/// Habilidades de um monstro do último import com o que a calculadora de dano precisa: atributos totais,
/// fórmula de cada habilidade e o bônus de dano dos skill-ups no nível atual.
/// </summary>
public sealed record GetMonsterSkillsQuery(long WizardId, long MonsterId) : IQuery<MonsterSkillsResponse>;

public sealed record MonsterSkillsResponse(
    long MonsterId,
    int MasterId,
    string? Name,
    Element Element,
    int Level,
    StatValues Total,
    IReadOnlyList<MonsterSkillDetail> Skills);

/// <param name="Name">Nulo quando a habilidade não está no catálogo.</param>
/// <param name="DamageBonus">Bônus de dano (%) dos skill-ups até o nível atual.</param>
public sealed record MonsterSkillDetail(
    int SkillId,
    string? Name,
    int Slot,
    int Level,
    int MaxLevel,
    int Hits,
    bool Passive,
    bool Aoe,
    string? FormulaText,
    string? Formula,
    int DamageBonus);

internal sealed class GetMonsterSkillsQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog monsterCatalog,
    ISkillCatalog skillCatalog) : IQueryHandler<GetMonsterSkillsQuery, MonsterSkillsResponse>
{
    public async Task<Result<MonsterSkillsResponse>> HandleAsync(GetMonsterSkillsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var monster = result.Value.Monsters.FirstOrDefault(m => m.GameId == query.MonsterId);
        if (monster is null)
        {
            return AccountErrors.MonsterNotFound(query.MonsterId);
        }

        var stats = new EquipmentIndex(result.Value).StatsOf(monster);

        return new MonsterSkillsResponse(
            monster.GameId,
            monster.MasterId,
            monsterCatalog.Find(monster.MasterId)?.Name,
            monster.Element,
            monster.Level,
            stats.Total,
            [.. monster.Skills
                .Select(skill => (Skill: skill, Definition: skillCatalog.Find(skill.SkillId)))
                .OrderBy(s => s.Definition?.Slot ?? int.MaxValue)
                .Select(s => s.Definition is { } d
                    ? new MonsterSkillDetail(
                        s.Skill.SkillId, d.Name, d.Slot, s.Skill.Level, d.MaxLevel, d.Hits, d.Passive, d.Aoe,
                        d.FormulaText, d.Formula, d.DamageBonusAt(s.Skill.Level))
                    : new MonsterSkillDetail(s.Skill.SkillId, null, 0, s.Skill.Level, 0, 0, false, false, null, null, 0))]);
    }
}
