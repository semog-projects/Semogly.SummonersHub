using Microsoft.AspNetCore.Authentication;
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

        auth.MapGet("/providers", ListProviders)
            .WithName("ListExternalProviders")
            .WithSummary("Provedores de login externo configurados (ex.: Google)");

        auth.MapGet("/external/{provider}", ChallengeExternal)
            .WithName("ExternalLogin")
            .WithSummary("Redireciona para o login do provedor; volta para returnUrl (caminho local)");

        auth.MapGet("/external/callback", ExternalCallback)
            .WithName("ExternalLoginCallback")
            .WithSummary("Conclui o login externo: entra, vincula pelo e-mail confirmado ou cria o usuário");

        return app;
    }

    private static async Task<Ok<string[]>> ListProviders(SignInManager<ApplicationUser> signInManager) =>
        TypedResults.Ok((await signInManager.GetExternalAuthenticationSchemesAsync()).Select(s => s.Name).Order().ToArray());

    private static async Task<Results<ChallengeHttpResult, NotFound>> ChallengeExternal(
        string provider,
        string? returnUrl,
        SignInManager<ApplicationUser> signInManager)
    {
        var scheme = (await signInManager.GetExternalAuthenticationSchemesAsync())
            .FirstOrDefault(s => string.Equals(s.Name, provider, StringComparison.OrdinalIgnoreCase));
        if (scheme is null)
        {
            return TypedResults.NotFound();
        }

        var callback = $"/api/auth/external/callback?returnUrl={Uri.EscapeDataString(ExternalLogins.SafeReturnUrl(returnUrl))}";
        var properties = signInManager.ConfigureExternalAuthenticationProperties(scheme.Name, callback);
        return TypedResults.Challenge(properties, [scheme.Name]);
    }

    private static async Task<RedirectHttpResult> ExternalCallback(
        string? returnUrl,
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        var outcome = info is null
            ? ExternalLoginOutcome.Failed
            : await ExternalLogins.SignInAsync(signInManager, userManager, info);

        // O cookie externo só serve para este passo
        await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        return outcome == ExternalLoginOutcome.SignedIn
            ? TypedResults.LocalRedirect(ExternalLogins.SafeReturnUrl(returnUrl))
            : TypedResults.LocalRedirect($"/login?erro={outcome.ToString().ToLowerInvariant()}");
    }

    private static async Task<NoContent> Logout(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }
}
