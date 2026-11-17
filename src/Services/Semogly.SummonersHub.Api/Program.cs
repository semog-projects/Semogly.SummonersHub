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

// Hospedagem em container (Vercel): a plataforma informa a porta em PORT e o tráfego chega de fora do loopback
if (builder.Configuration["PORT"] is { Length: > 0 } port)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

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

// Origem real do navegador vinda do proxy (Vite em dev, Vercel em produção): o redirect URI do Google fica
// na origem do front. Por padrão só proxies em loopback são aceitos; atrás da Vercel
// (ReverseProxy:TrustForwardedHeaders) o container só recebe tráfego dela, então qualquer origem é aceita
var trustForwardedHeaders = builder.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    if (trustForwardedHeaders)
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
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
