using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Confere um time de referência contra os monstros da conta: para cada vaga, escolhe o melhor candidato
/// (mesmo nome e elemento) e diz quais mínimos ele ainda não atinge.
/// </summary>
public static class PveTeamEvaluator
{
    public static PveTeamEvaluation Evaluate(PveTeam team, IReadOnlyCollection<PveCandidate> monsters)
    {
        var used = new HashSet<long>();
        var slots = new List<PveSlotEvaluation>(team.Slots.Count);

        foreach (var slot in team.Slots)
        {
            // Um monstro ocupa uma vaga só (relevante quando o time repete o mesmo monstro)
            // Melhor candidato: menos mínimos não atingidos, depois o que está mais perto deles
            var ranked = monsters
                .Where(m => !used.Contains(m.MonsterId)
                    && m.Element == slot.Element
                    && string.Equals(m.Name, slot.MonsterName, StringComparison.OrdinalIgnoreCase))
                .Select(m => new PveSlotEvaluation(slot, PveSlotStatus.BelowRequirements, m, Unmet(slot, m.Total)))
                .OrderBy(e => e.Unmet.Count)
                .ThenBy(e => e.Unmet.Sum(u => (decimal)(u.Required - u.Current) / u.Required))
                .ThenByDescending(e => e.Candidate!.Stars)
                .ThenByDescending(e => e.Candidate!.Level)
                .ThenBy(e => e.Candidate!.MonsterId)
                .ToList();

            if (ranked.Count == 0)
            {
                slots.Add(new PveSlotEvaluation(slot, PveSlotStatus.Missing, null, []));
                continue;
            }

            var best = ranked[0];
            used.Add(best.Candidate!.MonsterId);
            slots.Add(best.Unmet.Count == 0 ? best with { Status = PveSlotStatus.Ready } : best);
        }

        return new PveTeamEvaluation(team, slots);
    }

    /// <summary>
    /// Troca a PRE mínima das vagas de debuffer pela PRE que a RES do boss pede. Sem dados da dungeon,
    /// ou com o boss já no piso de resistência, o time fica como está.
    /// </summary>
    public static PveTeam WithDungeonAccuracy(PveTeam team, DungeonStats? dungeon)
    {
        if (dungeon is null)
        {
            return team;
        }

        return team with
        {
            Slots = [.. team.Slots.Select(slot => !slot.Debuffer ? slot : slot with
            {
                Requirements =
                [
                    .. slot.Requirements.Where(r => r.Stat != PveStat.Accuracy),
                    .. dungeon.AccuracyTarget > 0 ? [new StatRequirement(PveStat.Accuracy, dungeon.AccuracyTarget)] : Array.Empty<StatRequirement>(),
                ],
            })],
        };
    }

    private static List<UnmetRequirement> Unmet(PveTeamSlot slot, StatValues total) =>
        [.. slot.Requirements
            .Select(r => new UnmetRequirement(r.Stat, r.Min, ValueOf(total, r.Stat)))
            .Where(u => u.Current < u.Required)];

    public static int ValueOf(StatValues stats, PveStat stat) => stat switch
    {
        PveStat.Hp => stats.Hp,
        PveStat.Attack => stats.Attack,
        PveStat.Defense => stats.Defense,
        PveStat.Speed => stats.Speed,
        PveStat.CriticalRate => stats.CriticalRate,
        PveStat.CriticalDamage => stats.CriticalDamage,
        PveStat.Resistance => stats.Resistance,
        PveStat.Accuracy => stats.Accuracy,
        PveStat.EffectiveHp => stats.EffectiveHp,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };
}

/// <summary>Monstro da conta já com nome do bestiário e atributos totais.</summary>
public sealed record PveCandidate(long MonsterId, int MasterId, string? Name, Element Element, int Stars, int Level, StatValues Total);

public enum PveSlotStatus
{
    /// <summary>A conta não tem o monstro.</summary>
    Missing,

    /// <summary>Tem o monstro, mas algum atributo está abaixo do mínimo.</summary>
    BelowRequirements,

    Ready,
}

public sealed record UnmetRequirement(PveStat Stat, int Required, int Current);

public sealed record PveSlotEvaluation(
    PveTeamSlot Slot,
    PveSlotStatus Status,
    PveCandidate? Candidate,
    IReadOnlyList<UnmetRequirement> Unmet);

public sealed record PveTeamEvaluation(PveTeam Team, IReadOnlyList<PveSlotEvaluation> Slots)
{
    public int ReadySlots => Slots.Count(s => s.Status == PveSlotStatus.Ready);

    public bool IsReady => ReadySlots == Slots.Count;
}
