using Microsoft.AspNetCore.Http.HttpResults;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Application.GameData.GetMonsters;
using Semogly.SummonersHub.Domain.Accounts;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class GameDataEndpoints
{
    // Ícones servidos pelo SWARFARM (mesmo nome de arquivo do bestiário)
    private const string MonsterImageBaseUrl = "https://swarfarm.com/static/herders/images/monsters/";
    private const string SkillIconBaseUrl = "https://swarfarm.com/static/herders/images/skills/";

    public static IEndpointRouteBuilder MapGameDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/game-data").WithTags("GameData");

        group.MapGet("/monsters", GetMonsters)
            .WithName("GetMonsters")
            .WithSummary("Monstros do bestiário pelos ids do jogo (unit_master_id)")
            .WithDescription($"Exemplo: /api/game-data/monsters?ids=19811&ids=21911. Até {GetMonstersQuery.MaxIds} ids; ids desconhecidos são ignorados.");

        group.MapGet("/monsters/search", SearchMonsters)
            .WithName("SearchMonsters")
            .WithSummary("Monstros que dá para criar à mão, pelo nome (até 20)")
            .WithDescription("Exemplo: /api/game-data/monsters/search?name=lapis. Só monstros com atributos no bestiário.");

        group.MapGet("/bestiary", ListBestiary)
            .WithName("ListBestiary")
            .WithSummary("Bestiário: todos os monstros obteníveis (sem NPCs e bosses)");

        group.MapGet("/bestiary/{com2usId:int}", GetBestiaryMonster)
            .WithName("GetBestiaryMonster")
            .WithSummary("Monstro do bestiário: atributos 6★ nv. 40, skills, líder, despertar, família e onde obter");

        group.MapGet("/dungeons", (IDungeonCatalog dungeons) => TypedResults.Ok(dungeons.All()))
            .WithName("GetDungeons")
            .WithSummary("Andar mais alto de cada dungeon dos times PvE: RES e VEL do boss e dos demais inimigos (SWARFARM)");

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

    private const int SearchLimit = 20;

    private static Ok<IReadOnlyList<MonsterResponse>> SearchMonsters(string? name, IMonsterCatalog catalog)
    {
        IReadOnlyList<MonsterResponse> response = [.. catalog.Search(name ?? string.Empty, SearchLimit).Select(ToResponse)];
        return TypedResults.Ok(response);
    }

    private static Ok<IReadOnlyList<MonsterResponse>> ListBestiary(IMonsterCatalog catalog)
    {
        IReadOnlyList<MonsterResponse> response = [.. catalog.All()
            .Where(m => m.Obtainable)
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.AwakenLevel)
            .ThenBy(m => m.Element)
            .Select(ToResponse)];
        return TypedResults.Ok(response);
    }

    private static Results<Ok<BestiaryMonsterResponse>, NotFound> GetBestiaryMonster(int com2usId, IMonsterCatalog catalog, ISkillCatalog skills)
    {
        if (catalog.Find(com2usId) is not { Obtainable: true } monster)
        {
            return TypedResults.NotFound();
        }

        // Família: os outros elementos e as versões desperta/não desperta/2A
        var family = catalog.All()
            .Where(m => m.FamilyId == monster.FamilyId && m.Com2usId != monster.Com2usId && m.Obtainable)
            .OrderBy(m => m.AwakenLevel)
            .ThenBy(m => m.Element)
            .Select(ToResponse)
            .ToList();

        var monsterSkills = (monster.SkillIds ?? [])
            .Select(skills.Find)
            .OfType<SkillDefinition>()
            .Select(skill => new BestiarySkillResponse(
                skill.Id,
                skill.Name,
                skill.Slot,
                skill.Description,
                skill.Cooldown,
                skill.Hits,
                skill.Passive,
                skill.Aoe,
                skill.MaxLevel,
                skill.FormulaText,
                skill.LevelProgress ?? [],
                string.IsNullOrEmpty(skill.IconFilename) ? null : SkillIconBaseUrl + skill.IconFilename))
            .ToList();

        return TypedResults.Ok(new BestiaryMonsterResponse(
            ToResponse(monster),
            monster.MaxLevelStats,
            monster.LeaderSkill,
            monster.AwakenBonus,
            monster.Sources ?? [],
            monsterSkills,
            family));
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

/// <param name="MaxLevelStats">Atributos base em 6★ nível 40, sem runas; nulo quando o bestiário não informa.</param>
/// <param name="Family">Outros elementos e versões (desperta, não desperta, 2A) da mesma família.</param>
internal sealed record BestiaryMonsterResponse(
    MonsterResponse Monster,
    BaseStats? MaxLevelStats,
    LeaderSkill? LeaderSkill,
    string? AwakenBonus,
    IReadOnlyList<string> Sources,
    IReadOnlyList<BestiarySkillResponse> Skills,
    IReadOnlyList<MonsterResponse> Family);

/// <param name="Cooldown">Recarga em turnos; nula sem recarga.</param>
/// <param name="LevelProgress">Texto de cada skill-up, na ordem.</param>
internal sealed record BestiarySkillResponse(
    int Id,
    string Name,
    int Slot,
    string? Description,
    int? Cooldown,
    int Hits,
    bool Passive,
    bool Aoe,
    int MaxLevel,
    string? FormulaText,
    IReadOnlyList<string> LevelProgress,
    string? IconUrl);
