using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Semogly.SummonersHub.Infrastructure.Identity;

/// <summary>Login com provedores externos (Google): configuração e vínculo com o usuário do site.</summary>
public static class ExternalLogins
{
    public const string Google = "Google";
    public const string GoogleSection = "Authentication:Google";
    public const string GoogleCallbackPath = "/api/auth/signin-google";
    public const string EmailVerifiedClaim = "email_verified";

    /// <summary>
    /// Só aceita caminhos locais (<c>/contas</c>), nunca outra origem (<c>//site</c>, <c>https://...</c>):
    /// evita usar o login como redirecionador aberto.
    /// </summary>
    public static string SafeReturnUrl(string? returnUrl) =>
        returnUrl is { Length: > 0 } url && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
            ? url
            : "/";

    /// <summary>
    /// Entra com o login externo. Sem vínculo ainda, vincula ao usuário com o mesmo e-mail (confirmado pelo
    /// provedor) ou cria um usuário novo. Devolve falso quando não é possível entrar.
    /// </summary>
    public static async Task<ExternalLoginOutcome> SignInAsync(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ExternalLoginInfo info)
    {
        var existing = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        if (existing.Succeeded)
        {
            return ExternalLoginOutcome.SignedIn;
        }

        if (existing.IsLockedOut || existing.IsNotAllowed)
        {
            return ExternalLoginOutcome.Blocked;
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var verified = string.Equals(info.Principal.FindFirstValue(EmailVerifiedClaim), "true", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(email) || !verified)
        {
            return ExternalLoginOutcome.EmailNotVerified;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            if (!(await userManager.CreateAsync(user)).Succeeded)
            {
                return ExternalLoginOutcome.Failed;
            }
        }
        else if (await userManager.IsLockedOutAsync(user))
        {
            return ExternalLoginOutcome.Blocked;
        }

        if (!(await userManager.AddLoginAsync(user, info)).Succeeded)
        {
            return ExternalLoginOutcome.Failed;
        }

        await signInManager.SignInAsync(user, isPersistent: true, info.LoginProvider);
        return ExternalLoginOutcome.SignedIn;
    }
}

public enum ExternalLoginOutcome
{
    SignedIn,
    EmailNotVerified,
    Blocked,
    Failed,
}
