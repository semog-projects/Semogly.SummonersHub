using Semogly.SummonersHub.Application.GameData.GetMonsters;
using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Application.GameData;

public static class GameDataErrors
{
    public static readonly Error InvalidIdCount = Error.Validation(
        "GameData.InvalidIdCount",
        $"Informe entre 1 e {GetMonstersQuery.MaxIds} ids de monstros.");
}
