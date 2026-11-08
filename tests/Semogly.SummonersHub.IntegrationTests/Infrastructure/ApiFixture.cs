using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Semogly.SummonersHub.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Semogly.SummonersHub.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe um PostgreSQL em container e hospeda a API em memória apontando para ele, com o mesmo nome de
/// connection string que o AppHost injeta. As migrations rodam na inicialização (ambiente Development).
/// Compartilhado por todos os testes: cada teste usa um invocador próprio para não interferir nos demais.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    private WebApplicationFactory<Program> _factory = null!;

    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>Escopo de DI para verificar diretamente o que foi gravado no banco.</summary>
    public AsyncServiceScope CreateScope() => _factory.Services.CreateAsyncScope();

    public static SummonersHubDbContext DbContext(AsyncServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<SummonersHubDbContext>();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:summonershub-db", _postgres.GetConnectionString());
        });

        // Força a inicialização do host (aplica as migrations)
        _ = _factory.Server;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
