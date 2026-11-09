using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Semogly.SummonersHub.Api.Authentication;
using Semogly.SummonersHub.Api.Endpoints;
using Semogly.SummonersHub.Api.ErrorHandling;
using Semogly.SummonersHub.Application;
using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Infrastructure;
using Semogly.SummonersHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructure();
builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

// Origem real do navegador vinda do proxy do Vite (só proxies em loopback são aceitos por padrão):
// o redirect URI do Google fica na origem do front
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost);

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.ApplyMigrationsAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapSystemEndpoints();
app.MapGameDataEndpoints();
app.MapAccountsEndpoints();
app.MapOptimizerPresetsEndpoints();
app.MapSettingsEndpoints();
app.MapManualItemsEndpoints();
app.MapSharedBuildsEndpoints();

await app.RunAsync();

// Ponto de entrada para o WebApplicationFactory nos testes de integração
public partial class Program;
