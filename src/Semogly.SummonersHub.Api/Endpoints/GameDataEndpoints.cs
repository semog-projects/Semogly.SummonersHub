using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData.GetMonsters;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class GameDataEndpoints
{
    // Ícones servidos pelo SWARFARM (mesmo nome de arquivo do bestiário)
    private const string MonsterImageBaseUrl = "https://swarfarm.com/static/herders/images/monsters/";

    public static IEndpointRouteBuilder MapGameDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game-data").WithTags("GameData");

        group.MapGet("/monsters", GetMonsters)
            .WithName("GetMonsters")
            .WithSummary("Monstros do bestiário pelos ids do jogo (unit_master_id)")
            .WithDescription($"Exemplo: /api/game-data/monsters?ids=19811&ids=21911. Até {GetMonstersQuery.MaxIds} ids; ids desconhecidos são ignorados.");

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<MonsterResponse>>, ProblemHttpResult>> GetMonsters(
        int[] ids,
        IQueryHandler<GetMonstersQuery, IReadOnlyList<MonsterDefinition>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMonstersQuery(ids), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        IReadOnlyList<MonsterResponse> response = result.Value.Select(ToResponse).ToList();
        return TypedResults.Ok(response);
    }

    private static MonsterResponse ToResponse(MonsterDefinition monster) => new(
        monster.Com2usId,
        monster.FamilyId,
        monster.Name,
        monster.Element,
        monster.Archetype,
        monster.NaturalStars,
        monster.AwakenLevel,
        monster.IsHomunculus,
        string.IsNullOrEmpty(monster.ImageFilename) ? null : MonsterImageBaseUrl + monster.ImageFilename);
}

internal sealed record MonsterResponse(
    int Com2usId,
    int FamilyId,
    string Name,
    Element Element,
    Archetype Archetype,
    int NaturalStars,
    int AwakenLevel,
    bool IsHomunculus,
    string? ImageUrl);
