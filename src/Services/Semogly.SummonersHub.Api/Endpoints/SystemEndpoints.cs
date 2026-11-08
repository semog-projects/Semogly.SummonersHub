using System.Reflection;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class SystemEndpoints
{
    /// <summary>Endpoints de diagnóstico: o front usa /api/info para confirmar que a API está no ar.</summary>
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/info", GetInfo)
            .WithName("GetInfo")
            .WithTags("System");

        return app;
    }

    private static InfoResponse GetInfo(IHostEnvironment environment) => new(
        "Semogly.SummonersHub",
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev",
        environment.EnvironmentName);
}

internal sealed record InfoResponse(string Name, string Version, string Environment);
