using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Semogly.SummonersHub.Infrastructure.Persistence;

namespace Semogly.SummonersHub.Infrastructure.Identity;

internal static class IdentityExtensions
{
    /// <summary>
    /// ASP.NET Core Identity com os endpoints prontos (cadastro, login, refresh, gestão da conta).
    /// O front usa o cookie HttpOnly (mesma origem via proxy); o bearer token fica disponível para outros clientes.
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services
            .AddIdentityApiEndpoints<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<SummonersHubDbContext>()
            .AddErrorDescriber<PortugueseIdentityErrorDescriber>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "summonershub.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;

            // API pura: sem redirecionar para uma página de login, só o status HTTP
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // Chaves que assinam cookies e tokens ficam no banco: sobrevivem a restarts e são compartilhadas entre instâncias
        services.AddDataProtection()
            .SetApplicationName("Semogly.SummonersHub")
            .PersistKeysToDbContext<SummonersHubDbContext>();

        services.AddAuthorization();

        return services;
    }
}
