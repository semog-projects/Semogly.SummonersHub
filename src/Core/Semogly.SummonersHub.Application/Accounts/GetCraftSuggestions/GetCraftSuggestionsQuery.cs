using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.GetCraftSuggestions;

/// <summary>
/// Para cada grind e gem do inventário (agrupados como no inventário), as runas onde ele mais aumenta a
/// eficiência. Por padrão considera só runas equipadas, que é onde o ganho aparece no jogo.
/// </summary>
public sealed record GetCraftSuggestionsQuery(long WizardId, bool EquippedOnly = true) : IQuery<IReadOnlyList<CraftSuggestionGroup>>
{
    public const int SuggestionsPerItem = 10;
}

/// <param name="Stat">Código numérico do atributo, como nas runas.</param>
/// <param name="Range">Faixa de valores do item; nula nos itens ancient (sem fonte para as faixas).</param>
public sealed record CraftSuggestionGroup(
    CraftItemType Type,
    RuneSet Set,
    int Stat,
    int Rarity,
    int Amount,
    ValueRange? Range,
    IReadOnlyList<CraftSuggestionItem> Suggestions);

/// <param name="TargetStat">Sub que recebe o grind ou é trocada pela gem (código numérico).</param>
public sealed record CraftSuggestionItem(
    RuneDetail Rune,
    EquippedMonster? EquippedOn,
    int TargetStat,
    int CurrentValue,
    int MinValue,
    int MaxValue,
    decimal MinEfficiencyGain,
    decimal MaxEfficiencyGain);

internal sealed class GetCraftSuggestionsQueryHandler(
    IAccountSnapshotRepository repository,
    IWizardAccountRepository wizardAccounts,
    IUserContext userContext,
    IMonsterCatalog catalog) : IQueryHandler<GetCraftSuggestionsQuery, IReadOnlyList<CraftSuggestionGroup>>
{
    public async Task<Result<IReadOnlyList<CraftSuggestionGroup>>> HandleAsync(GetCraftSuggestionsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetOwnedLatestAsync(wizardAccounts, query.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var snapshot = result.Value;
        var monsters = EquipmentDetails.MonstersById(snapshot, catalog);
        var runes = query.EquippedOnly ? snapshot.Runes.Where(r => r.EquippedMonsterId is not null).ToList() : snapshot.Runes;

        return snapshot.CraftItems
            .GroupBy(item => (item.Type, Code: item.Decode()))
            .Select(group =>
            {
                var (type, code) = group.Key;
                var suggestions = CraftSuggestions.For(type, code, runes)
                    .OrderByDescending(s => s.MaxEfficiencyGain)
                    .ThenByDescending(s => s.MinEfficiencyGain)
                    .ThenBy(s => s.Rune.GameId)
                    .Take(GetCraftSuggestionsQuery.SuggestionsPerItem)
                    .Select(s => new CraftSuggestionItem(
                        EquipmentDetails.ToDetail(s.Rune),
                        s.Rune.EquippedMonsterId is { } id ? monsters[id] : null,
                        (int)s.TargetStat,
                        s.CurrentValue,
                        s.MinValue,
                        s.MaxValue,
                        Math.Round(s.MinEfficiencyGain, 2),
                        Math.Round(s.MaxEfficiencyGain, 2)))
                    .ToList();

                return new CraftSuggestionGroup(
                    type,
                    code.Set,
                    (int)code.Stat,
                    code.Rarity,
                    group.Sum(item => item.Amount),
                    CraftItemRanges.For(type, code.Stat, code.Rarity),
                    suggestions);
            })
            // Itens com o maior ganho possível primeiro; os sem sugestão no fim
            .OrderByDescending(g => g.Suggestions.Count > 0 ? g.Suggestions[0].MaxEfficiencyGain : -1)
            .ThenBy(g => g.Type.IsGem() ? 0 : 1)
            .ThenBy(g => g.Set)
            .ThenBy(g => g.Stat)
            .ToList();
    }
}
