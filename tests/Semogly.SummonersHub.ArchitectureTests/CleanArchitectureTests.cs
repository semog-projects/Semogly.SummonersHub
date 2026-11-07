using System.Reflection;
using Semogly.SummonersHub.Application.Abstractions.Messaging;

namespace Semogly.SummonersHub.ArchitectureTests;

/// <summary>
/// Garante a regra de dependência da Clean Architecture:
/// as dependências apontam sempre para dentro (Api → Infrastructure → Application → Domain).
/// </summary>
public class CleanArchitectureTests
{
    private const string Domain = "Semogly.SummonersHub.Domain";
    private const string Application = "Semogly.SummonersHub.Application";
    private const string Infrastructure = "Semogly.SummonersHub.Infrastructure";
    private const string Api = "Semogly.SummonersHub.Api";

    // Bibliotecas de infraestrutura que não podem vazar para as camadas internas
    private static readonly string[] InfrastructureLibraries =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
    ];

    [Fact]
    public void Domain_ShouldNotDependOnOtherLayers() =>
        AssertNoDependency(Assembly.Load(Domain), Application, Infrastructure, Api);

    [Fact]
    public void Domain_ShouldNotDependOnInfrastructureLibraries() =>
        AssertNoDependency(Assembly.Load(Domain), InfrastructureLibraries);

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrApi() =>
        AssertNoDependency(Assembly.Load(Application), Infrastructure, Api);

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureLibraries() =>
        AssertNoDependency(Assembly.Load(Application), InfrastructureLibraries);

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi() =>
        AssertNoDependency(Assembly.Load(Infrastructure), Api);

    [Fact]
    public void Handlers_ShouldBeInternalAndSealed()
    {
        Type[] handlerInterfaces = [typeof(ICommandHandler<>), typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

        var invalidHandlers = Assembly.Load(Application).GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            .Where(t => t.IsPublic || !t.IsSealed)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(invalidHandlers);
    }

    private static void AssertNoDependency(Assembly assembly, params string[] forbiddenPrefixes)
    {
        var violations = assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"{assembly.GetName().Name} não deveria depender de: {string.Join(", ", violations)}");
    }
}
