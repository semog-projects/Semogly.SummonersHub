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

    /// <summary>Atributos somáveis, guardados no vetor de contribuições (o HP efetivo é derivado de HP e DEF).</summary>
    private static readonly PveStat[] VectorStats = [.. Enum.GetValues<PveStat>().Where(s => s != PveStat.EffectiveHp)];

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
        var constant = ConstantVector(
            @base,
            artifacts.Where(a => a.EquippedMonsterId == monster.GameId),
            request.RequiredSets,
            request.TeamSets ?? []);
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
                    && MinimumsStillPossible(p, constant, remainingMax, request.Minimums)
                    && MaximumsStillPossible(p, constant, request.Maximums))
                .OrderByDescending(p => Score(Add(constant, p.Vector), @base, request))
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
    /// Pontuação de uma build: soma ponderada dos atributos de foco relativos à base do monstro (HP, ATK, DEF,
    /// VEL e HP efetivo) ou a 100 pontos (atributos percentuais). Ex.: VEL total 200 com base 100 e peso 1 vale 200.
    /// </summary>
    public static decimal Score(StatValues total, BaseStats @base, IReadOnlyList<PveStat> focus, IReadOnlyDictionary<PveStat, decimal>? weights = null) =>
        Math.Round(focus.Sum(stat => Weight(weights, stat) * PveTeamEvaluator.ValueOf(total, stat) / Scale(@base, stat) * 100), 2);

    /// <summary>Peso do atributo no foco; sem peso informado, vale 1.</summary>
    public static decimal Weight(IReadOnlyDictionary<PveStat, decimal>? weights, PveStat stat) =>
        weights is not null && weights.TryGetValue(stat, out var weight) ? weight : 1;

    /// <summary>
    /// Melhor artefato de cada tipo para o foco: o de elemento precisa ser do elemento do monstro e o de
    /// arquétipo, do arquétipo dele. Só o principal (HP/ATK/DEF) entra nos atributos; as subs são efeitos
    /// condicionais. Em empate, fica o artefato que o monstro já usa.
    /// </summary>
    public static IReadOnlyList<Artifact> BestArtifacts(
        Monster monster,
        Archetype? archetype,
        IEnumerable<Artifact> pool,
        IReadOnlyList<PveStat> focus,
        IReadOnlyDictionary<PveStat, decimal>? weights = null)
    {
        var candidates = pool.ToList();
        var chosen = new List<Artifact>();

        foreach (var type in Enum.GetValues<ArtifactType>())
        {
            var best = candidates
                .Where(a => a.Type == type && (type == ArtifactType.Element ? a.Element == monster.Element : a.Archetype == archetype))
                .OrderByDescending(a => ArtifactScore(a, monster.BaseStats, focus, weights))
                .ThenByDescending(a => a.EquippedMonsterId == monster.GameId)
                .ThenByDescending(a => a.Level)
                .ThenBy(a => a.GameId)
                .FirstOrDefault();

            if (best is not null)
            {
                chosen.Add(best);
            }
        }

        return chosen;
    }

    private static decimal ArtifactScore(Artifact artifact, BaseStats @base, IReadOnlyList<PveStat> focus, IReadOnlyDictionary<PveStat, decimal>? weights)
    {
        var stat = artifact.MainStat.Effect switch
        {
            ArtifactEffect.HpFlat => PveStat.Hp,
            ArtifactEffect.AttackFlat => PveStat.Attack,
            ArtifactEffect.DefenseFlat => PveStat.Defense,
            _ => (PveStat?)null,
        };

        if (stat is not { } s)
        {
            return 0;
        }

        // O principal soma em HP, ATK ou DEF; com HP efetivo no foco, HP e DEF também contam
        var direct = focus.Contains(s) ? Weight(weights, s) * artifact.MainStat.Value / Scale(@base, s) : 0;
        var viaEffectiveHp = focus.Contains(PveStat.EffectiveHp) && s is PveStat.Hp or PveStat.Defense
            ? Weight(weights, PveStat.EffectiveHp) * artifact.MainStat.Value / Scale(@base, s)
            : 0;
        return direct + viaEffectiveHp;
    }

    /// <summary>Pontuação aproximada de totais parciais (sem os bônus de sets formados fora dos pedidos).</summary>
    private static decimal Score(decimal[] totals, BaseStats @base, OptimizerRequest request) =>
        request.Focus.Sum(stat => Weight(request.Weights, stat) * ValueOf(totals, stat) / Scale(@base, stat));

    /// <summary>
    /// Quanto uma runa sozinha soma no atributo, relativo à base: serve para escolher os candidatos de cada slot.
    /// No HP efetivo, que não é somável, usa a soma das contribuições relativas de HP e DEF.
    /// </summary>
    private static decimal Marginal(decimal[] vector, BaseStats @base, PveStat stat) => stat == PveStat.EffectiveHp
        ? (vector[(int)PveStat.Hp] / Scale(@base, PveStat.Hp)) + (vector[(int)PveStat.Defense] / Scale(@base, PveStat.Defense))
        : vector[(int)stat] / Scale(@base, stat);

    /// <summary>Valor do atributo em totais aproximados (decimais); o HP efetivo é derivado de HP e DEF.</summary>
    private static decimal ValueOf(decimal[] totals, PveStat stat) => stat == PveStat.EffectiveHp
        ? EffectiveHp(totals[(int)PveStat.Hp], totals[(int)PveStat.Defense])
        : totals[(int)stat];

    private static decimal EffectiveHp(decimal hp, decimal defense) => hp * (1140 + (3.5m * defense)) / 1000;

    private static decimal Scale(BaseStats @base, PveStat stat) => stat switch
    {
        PveStat.Hp => Math.Max(1, @base.Hp),
        PveStat.Attack => Math.Max(1, @base.Attack),
        PveStat.Defense => Math.Max(1, @base.Defense),
        PveStat.Speed => Math.Max(1, @base.Speed),
        PveStat.EffectiveHp => Math.Max(1, EffectiveHp(@base.Hp, @base.Defense)),
        _ => 100,
    };

    private static OptimizedBuild? Evaluate(Monster monster, IReadOnlyCollection<Artifact> artifacts, Partial partial, OptimizerRequest request)
    {
        var calculated = MonsterStatsCalculator.Calculate(monster, partial.Runes.Select(c => c.Effective), artifacts);

        // Sets de equipe do time (em batalha): somados por cima do cálculo da tela do monstro
        var stats = request.TeamSets is { Count: > 0 } teamSets
            ? calculated with { Total = TeamSetBuffs.Apply(calculated.Total, monster.BaseStats, teamSets) }
            : calculated;
        if (request.Minimums.Any(m => PveTeamEvaluator.ValueOf(stats.Total, m.Stat) < m.Min)
            || (request.Maximums ?? []).Any(m => PveTeamEvaluator.ValueOf(stats.Total, m.Stat) > m.Max))
        {
            return null;
        }

        return new OptimizedBuild(
            [.. partial.Runes.Select(c => c.Original)],
            stats,
            Score(stats.Total, monster.BaseStats, request.Focus, request.Weights));
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
            .Select(r => Candidate.From(r, monster, request.AssumeMaxLevel, request.SimulateGrinds))
            .ToList();

        var criteria = new List<Func<Candidate, decimal>>
        {
            c => request.Focus.Sum(stat => Weight(request.Weights, stat) * Marginal(c.Vector, monster.BaseStats, stat)),
        };
        criteria.AddRange(request.Minimums.Select(m => (Func<Candidate, decimal>)(c => Marginal(c.Vector, monster.BaseStats, m.Stat))));

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
        var max = new decimal[VectorStats.Length];
        for (var next = slot + 1; next <= Slots; next++)
        {
            foreach (var stat in VectorStats)
            {
                max[(int)stat] += candidates[next].Count == 0 ? 0 : candidates[next].Max(c => c.Vector[(int)stat]);
            }
        }

        return max;
    }

    private static bool SetsStillPossible(Partial partial, Dictionary<RuneSet, int> required, int remainingSlots) =>
        required.Sum(r => Math.Max(0, r.Value - partial.SetCounts.GetValueOrDefault(r.Key))) <= remainingSlots;

    /// <summary>Com o máximo possível nos slots restantes ainda dá para atingir os mínimos? (O HP efetivo só cresce com HP e DEF.)</summary>
    private static bool MinimumsStillPossible(Partial partial, decimal[] constant, decimal[] remainingMax, IReadOnlyList<StatRequirement> minimums)
    {
        if (minimums.Count == 0)
        {
            return true;
        }

        var optimistic = Add(Add(constant, partial.Vector), remainingMax);
        return minimums.All(m => ValueOf(optimistic, m.Stat) >= m.Min);
    }

    /// <summary>Nenhuma runa reduz atributo: se a parcial já passou do máximo, a build completa também passa.</summary>
    private static bool MaximumsStillPossible(Partial partial, decimal[] constant, IReadOnlyList<StatCap>? maximums)
    {
        if (maximums is null || maximums.Count == 0)
        {
            return true;
        }

        var current = Add(constant, partial.Vector);
        return maximums.All(m => ValueOf(current, m.Stat) <= m.Max);
    }

    /// <summary>Base + artefatos + bônus dos sets obrigatórios (que toda build completa terá) + sets de equipe do time.</summary>
    private static decimal[] ConstantVector(
        BaseStats b,
        IEnumerable<Artifact> artifacts,
        IEnumerable<RuneSet> requiredSets,
        IEnumerable<RuneSet> teamSets)
    {
        var vector = new decimal[VectorStats.Length];
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

        foreach (var bonus in teamSets.Select(s => s.AllyBonus()).OfType<RuneStatValue>())
        {
            AddStat(vector, b, bonus.Stat, bonus.Value);
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
    /// <param name="Effective">
    /// Cópia equipada no monstro alvo e, se pedido, projetada em +15 e com grinds simulados (vai no cálculo).
    /// </param>
    private sealed record Candidate(Rune Original, Rune Effective, decimal[] Vector)
    {
        public static Candidate From(Rune rune, Monster monster, bool assumeMaxLevel, bool simulateGrinds)
        {
            var effective = assumeMaxLevel
                ? RuneProjection.ToMaxLevel(rune, monster.GameId)
                : new Rune(
                    rune.GameId, rune.Slot, rune.Stars, rune.IsAncient, rune.Set, rune.Level,
                    rune.Rarity, rune.OriginalRarity, rune.MainStat, rune.InnateStat, rune.Substats, monster.GameId);

            if (simulateGrinds)
            {
                effective = RuneProjection.WithMaxGrinds(effective);
            }

            var vector = new decimal[VectorStats.Length];
            AddStat(vector, monster.BaseStats, effective.MainStat.Stat, effective.MainStat.Value);
            if (effective.InnateStat is { } innate)
            {
                AddStat(vector, monster.BaseStats, innate.Stat, innate.Value);
            }

            foreach (var sub in effective.Substats)
            {
                AddStat(vector, monster.BaseStats, sub.Stat, sub.Value + sub.GrindValue);
            }

            return new Candidate(rune, effective, vector);
        }
    }

    private sealed record Partial(IReadOnlyList<Candidate> Runes, decimal[] Vector, IReadOnlyDictionary<RuneSet, int> SetCounts)
    {
        public static readonly Partial Empty = new([], new decimal[VectorStats.Length], new Dictionary<RuneSet, int>());

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
/// <param name="Focus">Até <see cref="RuneOptimizer.MaxFocusStats"/> atributos a maximizar.</param>
/// <param name="Minimums">Atributos totais mínimos.</param>
/// <param name="AssumeMaxLevel">Calcula cada runa como se estivesse em +15 (ver <see cref="RuneProjection"/>).</param>
/// <param name="Maximums">Atributos totais máximos (ex.: VEL até 200 para não passar outro monstro do time).</param>
/// <param name="Weights">Peso de cada atributo de foco; os ausentes valem 1.</param>
/// <param name="SimulateGrinds">Considera o grind lendário máximo em cada sub grindável.</param>
/// <param name="TeamSets">Sets de equipe do time (Fight, Determination...), um por set completo; somam em batalha.</param>
public sealed record OptimizerRequest(
    IReadOnlyList<RuneSet> RequiredSets,
    IReadOnlyDictionary<int, IReadOnlyList<RuneStat>> MainStatsBySlot,
    IReadOnlyList<PveStat> Focus,
    IReadOnlyList<StatRequirement> Minimums,
    bool AssumeMaxLevel,
    int MaxResults = 10,
    IReadOnlyList<StatCap>? Maximums = null,
    IReadOnlyDictionary<PveStat, decimal>? Weights = null,
    bool SimulateGrinds = false,
    IReadOnlyList<RuneSet>? TeamSets = null);

/// <summary>Atributo total máximo.</summary>
public sealed record StatCap(PveStat Stat, int Max);

/// <param name="Runes">Uma runa por slot, na ordem dos slots.</param>
public sealed record OptimizedBuild(IReadOnlyList<Rune> Runes, MonsterStats Stats, decimal Score);
