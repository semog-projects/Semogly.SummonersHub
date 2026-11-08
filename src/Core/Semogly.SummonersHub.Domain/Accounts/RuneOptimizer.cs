using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>
/// Monta builds de runas para um monstro: escolhe uma runa por slot que complete os sets pedidos, respeite
/// o principal dos slots 2/4/6 e os mínimos, e maximize os atributos de foco.
/// </summary>
/// <remarks>
/// Busca em feixe (beam search) slot a slot, com poda: parciais que não conseguem mais fechar os sets ou
/// atingir os mínimos são descartados, e só os <see cref="BeamWidth"/> melhores seguem. Antes da busca, cada
/// slot fica com os melhores candidatos de cada set (pelo foco e por cada atributo com mínimo). Não garante o
/// ótimo absoluto, mas resolve contas com milhares de runas em fração de segundo. O resultado final é
/// recalculado com <see cref="MonsterStatsCalculator"/> (bônus de sets e arredondamentos exatos).
/// </remarks>
public static class RuneOptimizer
{
    public const int BeamWidth = 3000;

    /// <summary>Candidatos guardados por slot, por set e por critério (foco ou cada mínimo).</summary>
    public const int CandidatesPerGroup = 10;

    public const int MaxFocusStats = 5;

    private const int Slots = 6;

    private static readonly PveStat[] AllStats = Enum.GetValues<PveStat>();

    public static IReadOnlyList<OptimizedBuild> Optimize(
        Monster monster,
        IReadOnlyCollection<Artifact> artifacts,
        IEnumerable<Rune> pool,
        OptimizerRequest request)
    {
        var required = request.RequiredSets
            .GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count() * g.Key.RequiredPieces());

        if (required.Values.Sum() > Slots || request.Focus.Count is 0 or > MaxFocusStats)
        {
            return [];
        }

        var @base = monster.BaseStats;
        var constant = ConstantVector(@base, artifacts.Where(a => a.EquippedMonsterId == monster.GameId), request.RequiredSets);
        var candidates = Candidates(monster, pool, request, required);

        var beam = new List<Partial> { Partial.Empty };
        for (var slot = 1; slot <= Slots; slot++)
        {
            var slotCandidates = candidates[slot];
            var remainingMax = RemainingMax(candidates, slot);
            var remainingSlots = Slots - slot;

            beam = [.. beam
                .SelectMany(partial => slotCandidates.Select(candidate => partial.With(candidate)))
                .Where(p => SetsStillPossible(p, required, remainingSlots)
                    && MinimumsStillPossible(p, constant, remainingMax, request.Minimums))
                .OrderByDescending(p => Score(Add(constant, p.Vector), @base, request.Focus))
                .Take(BeamWidth)];

            if (beam.Count == 0)
            {
                return [];
            }
        }

        return [.. beam
            .Select(p => Evaluate(monster, artifacts, p, request))
            .OfType<OptimizedBuild>()
            .OrderByDescending(b => b.Score)
            .Take(request.MaxResults)];
    }

    /// <summary>
    /// Pontuação de uma build: soma dos atributos de foco relativos à base do monstro (HP, ATK, DEF e VEL) ou a
    /// 100 pontos (atributos percentuais). Ex.: VEL total 200 com base 100 vale 200.
    /// </summary>
    public static decimal Score(StatValues total, BaseStats @base, IEnumerable<PveStat> focus) =>
        Math.Round(focus.Sum(stat => (decimal)PveTeamEvaluator.ValueOf(total, stat) / Scale(@base, stat) * 100), 2);

    private static decimal Score(decimal[] vector, BaseStats @base, IEnumerable<PveStat> focus) =>
        focus.Sum(stat => vector[(int)stat] / Scale(@base, stat));

    private static decimal Scale(BaseStats @base, PveStat stat) => stat switch
    {
        PveStat.Hp => Math.Max(1, @base.Hp),
        PveStat.Attack => Math.Max(1, @base.Attack),
        PveStat.Defense => Math.Max(1, @base.Defense),
        PveStat.Speed => Math.Max(1, @base.Speed),
        _ => 100,
    };

    private static OptimizedBuild? Evaluate(Monster monster, IReadOnlyCollection<Artifact> artifacts, Partial partial, OptimizerRequest request)
    {
        var stats = MonsterStatsCalculator.Calculate(monster, partial.Runes.Select(c => c.Effective), artifacts);
        if (request.Minimums.Any(m => PveTeamEvaluator.ValueOf(stats.Total, m.Stat) < m.Min))
        {
            return null;
        }

        return new OptimizedBuild([.. partial.Runes.Select(c => c.Original)], stats, Score(stats.Total, monster.BaseStats, request.Focus));
    }

    private static Dictionary<int, List<Candidate>> Candidates(
        Monster monster,
        IEnumerable<Rune> pool,
        OptimizerRequest request,
        Dictionary<RuneSet, int> required)
    {
        // Com os sets ocupando os 6 slots, runas de outros sets nunca entram
        var onlyRequiredSets = required.Values.Sum() == Slots;

        var ranked = pool
            .Where(r => !onlyRequiredSets || required.ContainsKey(r.Set))
            .Where(r => !request.MainStatsBySlot.TryGetValue(r.Slot, out var allowed) || allowed.Count == 0 || allowed.Contains(r.MainStat.Stat))
            .Select(r => Candidate.From(r, monster, request.AssumeMaxLevel))
            .ToList();

        var criteria = new List<Func<Candidate, decimal>> { c => Score(c.Vector, monster.BaseStats, request.Focus) };
        criteria.AddRange(request.Minimums.Select(m => (Func<Candidate, decimal>)(c => c.Vector[(int)m.Stat])));

        return Enumerable.Range(1, Slots).ToDictionary(
            slot => slot,
            slot => ranked
                .Where(c => c.Original.Slot == slot)
                // Sets fora dos pedidos competem juntos: só servem para preencher slots livres
                .GroupBy(c => required.ContainsKey(c.Original.Set) ? c.Original.Set : (RuneSet?)null)
                .SelectMany(group => criteria.SelectMany(criterion => group.OrderByDescending(criterion).Take(CandidatesPerGroup)))
                .DistinctBy(c => c.Original.GameId)
                .ToList());
    }

    /// <summary>Maior contribuição possível de cada atributo nos slots depois de <paramref name="slot"/>.</summary>
    private static decimal[] RemainingMax(Dictionary<int, List<Candidate>> candidates, int slot)
    {
        var max = new decimal[AllStats.Length];
        for (var next = slot + 1; next <= Slots; next++)
        {
            foreach (var stat in AllStats)
            {
                max[(int)stat] += candidates[next].Count == 0 ? 0 : candidates[next].Max(c => c.Vector[(int)stat]);
            }
        }

        return max;
    }

    private static bool SetsStillPossible(Partial partial, Dictionary<RuneSet, int> required, int remainingSlots) =>
        required.Sum(r => Math.Max(0, r.Value - partial.SetCounts.GetValueOrDefault(r.Key))) <= remainingSlots;

    private static bool MinimumsStillPossible(Partial partial, decimal[] constant, decimal[] remainingMax, IReadOnlyList<StatRequirement> minimums) =>
        minimums.All(m => constant[(int)m.Stat] + partial.Vector[(int)m.Stat] + remainingMax[(int)m.Stat] >= m.Min);

    /// <summary>Base + artefatos + bônus dos sets obrigatórios (que toda build completa terá).</summary>
    private static decimal[] ConstantVector(BaseStats b, IEnumerable<Artifact> artifacts, IEnumerable<RuneSet> requiredSets)
    {
        var vector = new decimal[AllStats.Length];
        vector[(int)PveStat.Hp] = b.Hp;
        vector[(int)PveStat.Attack] = b.Attack;
        vector[(int)PveStat.Defense] = b.Defense;
        vector[(int)PveStat.Speed] = b.Speed;
        vector[(int)PveStat.CriticalRate] = b.CriticalRate;
        vector[(int)PveStat.CriticalDamage] = b.CriticalDamage;
        vector[(int)PveStat.Resistance] = b.Resistance;
        vector[(int)PveStat.Accuracy] = b.Accuracy;

        foreach (var artifact in artifacts)
        {
            switch (artifact.MainStat.Effect)
            {
                case ArtifactEffect.HpFlat: vector[(int)PveStat.Hp] += artifact.MainStat.Value; break;
                case ArtifactEffect.AttackFlat: vector[(int)PveStat.Attack] += artifact.MainStat.Value; break;
                case ArtifactEffect.DefenseFlat: vector[(int)PveStat.Defense] += artifact.MainStat.Value; break;
            }
        }

        foreach (var set in requiredSets)
        {
            if (set == RuneSet.Swift)
            {
                vector[(int)PveStat.Speed] += b.Speed * RuneSetExtensions.SwiftSpeedPercent / 100m;
            }
            else if (set.StatBonus() is { } bonus)
            {
                AddStat(vector, b, bonus.Stat, bonus.Value);
            }
        }

        return vector;
    }

    /// <summary>Soma um atributo de runa no vetor; percentuais de HP/ATK/DEF viram pontos sobre a base.</summary>
    private static void AddStat(decimal[] vector, BaseStats b, RuneStat stat, int value)
    {
        switch (stat)
        {
            case RuneStat.HpFlat: vector[(int)PveStat.Hp] += value; break;
            case RuneStat.HpPercent: vector[(int)PveStat.Hp] += b.Hp * value / 100m; break;
            case RuneStat.AttackFlat: vector[(int)PveStat.Attack] += value; break;
            case RuneStat.AttackPercent: vector[(int)PveStat.Attack] += b.Attack * value / 100m; break;
            case RuneStat.DefenseFlat: vector[(int)PveStat.Defense] += value; break;
            case RuneStat.DefensePercent: vector[(int)PveStat.Defense] += b.Defense * value / 100m; break;
            case RuneStat.Speed: vector[(int)PveStat.Speed] += value; break;
            case RuneStat.CriticalRate: vector[(int)PveStat.CriticalRate] += value; break;
            case RuneStat.CriticalDamage: vector[(int)PveStat.CriticalDamage] += value; break;
            case RuneStat.Resistance: vector[(int)PveStat.Resistance] += value; break;
            case RuneStat.Accuracy: vector[(int)PveStat.Accuracy] += value; break;
        }
    }

    private static decimal[] Add(decimal[] a, decimal[] b)
    {
        var sum = new decimal[a.Length];
        for (var i = 0; i < a.Length; i++)
        {
            sum[i] = a[i] + b[i];
        }

        return sum;
    }

    /// <param name="Original">Runa como está na conta (vai na resposta).</param>
    /// <param name="Effective">Cópia equipada no monstro alvo e, se pedido, com o principal em +15 (vai no cálculo).</param>
    private sealed record Candidate(Rune Original, Rune Effective, decimal[] Vector)
    {
        public static Candidate From(Rune rune, Monster monster, bool assumeMaxLevel)
        {
            var main = assumeMaxLevel
                ? rune.MainStat with { Value = RuneEfficiency.MainStatAtMaxLevel(rune.MainStat.Stat, rune.Stars) }
                : rune.MainStat;

            var effective = new Rune(
                rune.GameId, rune.Slot, rune.Stars, rune.IsAncient, rune.Set, assumeMaxLevel ? Rune.MaxLevel : rune.Level,
                rune.Rarity, rune.OriginalRarity, main, rune.InnateStat, rune.Substats, monster.GameId);

            var vector = new decimal[AllStats.Length];
            AddStat(vector, monster.BaseStats, main.Stat, main.Value);
            if (rune.InnateStat is { } innate)
            {
                AddStat(vector, monster.BaseStats, innate.Stat, innate.Value);
            }

            foreach (var sub in rune.Substats)
            {
                AddStat(vector, monster.BaseStats, sub.Stat, sub.Value + sub.GrindValue);
            }

            return new Candidate(rune, effective, vector);
        }
    }

    private sealed record Partial(IReadOnlyList<Candidate> Runes, decimal[] Vector, IReadOnlyDictionary<RuneSet, int> SetCounts)
    {
        public static readonly Partial Empty = new([], new decimal[AllStats.Length], new Dictionary<RuneSet, int>());

        public Partial With(Candidate candidate)
        {
            var counts = new Dictionary<RuneSet, int>(SetCounts);
            counts[candidate.Original.Set] = counts.GetValueOrDefault(candidate.Original.Set) + 1;
            return new Partial([.. Runes, candidate], Add(Vector, candidate.Vector), counts);
        }
    }
}

/// <param name="RequiredSets">Sets que a build precisa completar; repetir o set pede mais de um (ex.: Energy 3x).</param>
/// <param name="MainStatsBySlot">Principais aceitos por slot (normalmente 2, 4 e 6); slot ausente ou vazio aceita qualquer um.</param>
/// <param name="Focus">Até <see cref="RuneOptimizer.MaxFocusStats"/> atributos a maximizar, com o mesmo peso.</param>
/// <param name="Minimums">Atributos totais mínimos.</param>
/// <param name="AssumeMaxLevel">Calcula o principal como se cada runa estivesse em +15.</param>
public sealed record OptimizerRequest(
    IReadOnlyList<RuneSet> RequiredSets,
    IReadOnlyDictionary<int, IReadOnlyList<RuneStat>> MainStatsBySlot,
    IReadOnlyList<PveStat> Focus,
    IReadOnlyList<StatRequirement> Minimums,
    bool AssumeMaxLevel,
    int MaxResults = 10);

/// <param name="Runes">Uma runa por slot, na ordem dos slots.</param>
public sealed record OptimizedBuild(IReadOnlyList<Rune> Runes, MonsterStats Stats, decimal Score);
