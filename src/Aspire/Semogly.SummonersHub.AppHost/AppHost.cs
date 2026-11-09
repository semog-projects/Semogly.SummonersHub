using Semogly.SummonersHub.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Container persistente com volume: os imports sobrevivem entre execuções locais
var postgres = builder.AddPostgres(ResourceNames.Postgres)
    .WithDataVolume()
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase(ResourceNames.Database);

var api = builder.AddProject<Projects.Semogly_SummonersHub_Api>(ResourceNames.Api)
    .WithReference(database).WaitFor(database)
    .WithHttpHealthCheck("/health");

// Front-end (React + Vite): o dev server faz proxy de /api para a API, então o navegador
// enxerga uma única origem e não é preciso configurar CORS
builder.AddViteApp(ResourceNames.Web, "../../Apps/web")
    .WithNpm(install: true)
    .WithReference(api).WaitFor(api)
    // Porta fixa: o redirect URI do login com Google é http://localhost:5173/api/auth/signin-google
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithExternalHttpEndpoints();

builder.Build().Run();
