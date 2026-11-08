using Microsoft.Extensions.DependencyInjection;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.GameData.GetMonsters;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application;

public static class DependencyInjection
{
    /// <summary>Registra os handlers de casos de uso.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IQueryHandler<GetMonstersQuery, IReadOnlyList<MonsterDefinition>>, GetMonstersQueryHandler>();

        return services;
    }
}
