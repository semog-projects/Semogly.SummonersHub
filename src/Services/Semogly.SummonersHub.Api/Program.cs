using System.Text.Json.Serialization;
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

var app = builder.Build();

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
