using Microsoft.Extensions.DependencyInjection;

namespace Semogly.SummonersHub.Application;

public static class DependencyInjection
{
    /// <summary>Registra os handlers de casos de uso (adicionados conforme as features entram).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
