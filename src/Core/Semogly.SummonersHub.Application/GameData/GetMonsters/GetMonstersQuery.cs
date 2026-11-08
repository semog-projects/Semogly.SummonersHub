using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.GameData.GetMonsters;

/// <summary>Busca vários monstros do bestiário de uma vez (ex.: todos os de uma conta importada).</summary>
public sealed record GetMonstersQuery(IReadOnlyCollection<int> Com2usIds) : IQuery<IReadOnlyList<MonsterDefinition>>
{
    public const int MaxIds = 500;
}

internal sealed class GetMonstersQueryHandler(IMonsterCatalog catalog)
    : IQueryHandler<GetMonstersQuery, IReadOnlyList<MonsterDefinition>>
{
    public Task<Result<IReadOnlyList<MonsterDefinition>>> HandleAsync(GetMonstersQuery query, CancellationToken cancellationToken)
    {
        if (query.Com2usIds.Count is 0 or > GetMonstersQuery.MaxIds)
        {
            return Task.FromResult<Result<IReadOnlyList<MonsterDefinition>>>(GameDataErrors.InvalidIdCount);
        }

        // Ids desconhecidos (monstros lançados depois da última sincronização) são ignorados
        IReadOnlyList<MonsterDefinition> monsters = query.Com2usIds
            .Distinct()
            .Select(catalog.Find)
            .OfType<MonsterDefinition>()
            .ToList();

        return Task.FromResult(Result.Success(monsters));
    }
}
