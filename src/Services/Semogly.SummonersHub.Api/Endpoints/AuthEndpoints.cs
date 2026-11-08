using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Semogly.SummonersHub.Infrastructure.Identity;

namespace Semogly.SummonersHub.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        // register, login (?useCookies=true para o front), refresh, manage/info, forgotPassword...
        auth.MapIdentityApi<ApplicationUser>();

        auth.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .WithSummary("Encerra a sessão do cookie de login");

        return app;
    }

    private static async Task<NoContent> Logout(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }
}
